using System.Collections;
using MoreMountains.Feedbacks;
using System;
using Unity.Cinemachine;
using UnityEngine;
public class PlayerAnimation : MonoBehaviour
{
    [SerializeField] private AttackManager attackManager;

    [Tooltip("近戰連段控制器。留空則從 attackManager 的 GameObject 上自動抓。")]
    [SerializeField] private MeleeAttackController meleeController;

    private Coroutine _initRoutine;
    public Animator anim;
    [SerializeField] private CapsuleCollider capsuleCollider;
    [SerializeField] private Transform groundPoint;
    int bipedLayer;
    int hoverLayer;
    int BarehandedLayer; // 未持有武器
    int Wielding_Gun_LeftLayer; // 持有單手槍（左手）
    int Wielding_Gun_RightLayer; // 持有單手槍（右手）
    int Dual_Wielding_Weapon_LeftLayer; // 雙手持有武器（左手）
    int Dual_Wielding_Weapon_RightLayer; // 雙手持有武器（右手）
    int One_Hand_Melee_AttackLayer; // 持有單手近戰武器
    int Shoulder_Weapon_LeftLayer; // 肩掛武器（左）
    int Shoulder_Weapon_RightLayer; // 肩掛武器（右）
    //Character height adjustment
    private float baseHeight;
    private Vector3 baseCenter;
    private float baseGroundPointY;
    //Thruster flame adjustment
    public Transform thrusterFlamePointL;
    public Transform thrusterFlamePointR;
    public GameObject normalThrusterFlameL;
    public GameObject normalThrusterFlameR;
    public GameObject boostedThrusterFlameL;
    public GameObject boostedThrusterFlameR;
    public GameObject meleeThrusterFlameL;
    public GameObject meleeThrusterFlameR;

    // ===== Attack Layer Blend (Smooth) =====
    [SerializeField] private float weaponHoldBlendTime = 0.15f; // 主人可調：持槍/雙持切換的混合時間
    [SerializeField] private float attackLayerBlendInTime = 0.12f;   // 可調：進入 Attack Layer 的時間
    [SerializeField] private float attackLayerBlendOutTime = 0.5f;  // 可調：退出 Attack Layer 的時間
    private Coroutine _attackLayerBlendRoutine;
    private Coroutine _weaponHoldBlendRoutine;
    public event Action OnStartAttacking;
    public event Action OnStopAttacking;
    private bool _attackEventFired;

    public CinemachineCamera Camera;

    public MMF_Player leftAttackFeedback;//Range
    public MMF_Player rightAttackFeedback;//Range
    public MMF_Player meleeAttackFeedback;
    public MMF_Player swordSwingFeedback;
    public MMF_Player reloadFeedback;
    public MMF_Player walkFeedback;

    public MMF_Player dustFeedback;
    public MMF_Player dustFeedback_OnShip;
    [Header("Gun")]
    [SerializeField] private ParticleSystem[] leftMuzzle;
    [SerializeField] private ParticleSystem[] rightMuzzle;
    [Header("Dust Particle")]
    [SerializeField] private ParticleSystem dustParticle;
    [SerializeField] private Transform landshipTransform;

    public bool IsDashAnimationActive { get; private set; }

    void Awake()
    {
        anim = anim != null ? anim : GetComponent<Animator>();
        capsuleCollider = capsuleCollider != null ? capsuleCollider : GetComponent<CapsuleCollider>();

        // 近戰 Animation Event 的轉發目標
        if (meleeController == null && attackManager != null)
            meleeController = attackManager.GetComponent<MeleeAttackController>();

        if (anim != null)
        {
            bipedLayer = anim.GetLayerIndex("Walking_Bipedal");
            hoverLayer = anim.GetLayerIndex("Walking_Hover");
            BarehandedLayer = anim.GetLayerIndex("Barehanded");
            Wielding_Gun_LeftLayer = anim.GetLayerIndex("Wielding_Gun_Left");
            Wielding_Gun_RightLayer = anim.GetLayerIndex("Wielding_Gun_Right");
            Dual_Wielding_Weapon_LeftLayer = anim.GetLayerIndex("Dual_Wielding_Weapon_Left");
            Dual_Wielding_Weapon_RightLayer = anim.GetLayerIndex("Dual_Wielding_Weapon_Right");
            One_Hand_Melee_AttackLayer = anim.GetLayerIndex("One_Hand_Melee_Attack");
            Shoulder_Weapon_LeftLayer = anim.GetLayerIndex("Shoulder_Weapon_Left");
            Shoulder_Weapon_RightLayer = anim.GetLayerIndex("Shoulder_Weapon_Right");
        }

        if (capsuleCollider != null)
        {
            baseHeight = capsuleCollider.height;
            baseCenter = capsuleCollider.center;
            baseGroundPointY = groundPoint != null ? groundPoint.localPosition.y : 0f;
        }
    }

    private void OnEnable()
    {
        _initRoutine = StartCoroutine(InitWhenReady());
    }
    private System.Collections.IEnumerator InitWhenReady()
    {
        SetWeaponHoldAllLayersOff();
        if (BarehandedLayer >= 0) anim.SetLayerWeight(BarehandedLayer, 1f);

        while (PlayerStats.Instance == null)
            yield return null;

        PlayerStats.Instance.OnLegVisualChanged += ApplyLegVisualChange;
        PlayerStats.Instance.OnHandWeaponDataChanged += RefreshWeaponHoldLayers;

        PlayerStats.Instance.OnThrusterVisualChanged += ApplyThrusterVfxChange;
        PlayerStats.Instance.OnThrusterFlameOffsetChanged += ApplyThrusterFlameTramformChange;

        // �i���P�B�]����GVFX + Offset ���n�^
        ApplyLegVisualChange(PlayerStats.Instance.CurrentLegVisual);
        RefreshWeaponHoldLayers();
        ApplyThrusterVfxChange(PlayerStats.Instance.CurrentThruster);
        ApplyThrusterFlameTramformChange(PlayerStats.Instance.CurrentThrusterFlameOffset);
    }

    private void OnDisable()
    {
        if (_initRoutine != null) StopCoroutine(_initRoutine);

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnLegVisualChanged -= ApplyLegVisualChange;
            PlayerStats.Instance.OnHandWeaponDataChanged -= RefreshWeaponHoldLayers;
            PlayerStats.Instance.OnThrusterFlameOffsetChanged -= ApplyThrusterFlameTramformChange;
            PlayerStats.Instance.OnThrusterVisualChanged -= ApplyThrusterVfxChange;
        }
    }

    private void ApplyLegVisualChange(VisualChange vc)
    {
        if (vc == null) return;

        ApplyColliderHeightOffset(vc.heightOffset);
        ApplyLocomotion(vc.animationType);
    }
    private void ApplyThrusterFlameTramformChange(Vector3 offset)
    {
        if (thrusterFlamePointL != null) thrusterFlamePointL.localPosition = offset;
        if (thrusterFlamePointR != null)
        {
            offset.x = -offset.x; // �k�䪺 X �b����
            thrusterFlamePointR.localPosition = offset;
        }
    }
    private void ApplyColliderHeightOffset(float heightOffset)
    {
        if (capsuleCollider == null) return;

        float newHeight = Mathf.Max(0.1f, baseHeight + heightOffset);
        capsuleCollider.height = newHeight;

        // �T�w�Y���G���׼W�[�Acenter ���U���@�b
        var c = baseCenter;
        c.y -= (newHeight - baseHeight) * 0.5f;
        capsuleCollider.center = c;

        // �վ� groundPoint ��m
        if (groundPoint != null)
        {
            Vector3 gp = groundPoint.localPosition;
            gp.y = baseGroundPointY - (newHeight - baseHeight);
            groundPoint.localPosition = gp;
        }
    }
    private void ApplyThrusterVfxChange(Thruster thr)
    {
        // 1) 先清掉舊的（避免重複堆疊）
        ClearThrusterVfxInstances();
        // 2) 沒裝 thruster（或卸下）→ 清空後直接結束
        if (thr == null) return;

        // 3) Instantiate normal flames
        if (thr.normalThrusterFlame != null)
        {
            if (thrusterFlamePointL != null)
                normalThrusterFlameL = Instantiate(thr.normalThrusterFlame, thrusterFlamePointL.transform, false);

            if (thrusterFlamePointR != null)
                normalThrusterFlameR = Instantiate(thr.normalThrusterFlame, thrusterFlamePointR.transform, false);
        }

        // 4) Instantiate boosted flames（通常預設先關閉，等 Boost 時再開）
        if (thr.boostedThrusterFlame != null)
        {
            if (thrusterFlamePointL != null)
                boostedThrusterFlameL = Instantiate(thr.boostedThrusterFlame, thrusterFlamePointL.transform, false);

            if (thrusterFlamePointR != null)
                boostedThrusterFlameR = Instantiate(thr.boostedThrusterFlame, thrusterFlamePointR.transform, false);

        }
        // 5) Instantiate melee flames
        if (thr.meleeThrusterFlame != null)
        {
            if (thrusterFlamePointL != null)
                meleeThrusterFlameL = Instantiate(thr.meleeThrusterFlame, thrusterFlamePointL.transform, false);
            if (thrusterFlamePointR != null)
                meleeThrusterFlameR = Instantiate(thr.meleeThrusterFlame, thrusterFlamePointR.transform, false);
        }
    }

    private void ClearThrusterVfxInstances()
    {
        if (normalThrusterFlameL != null) Destroy(normalThrusterFlameL);
        if (normalThrusterFlameR != null) Destroy(normalThrusterFlameR);
        if (boostedThrusterFlameL != null) Destroy(boostedThrusterFlameL);
        if (boostedThrusterFlameR != null) Destroy(boostedThrusterFlameR);
        if (meleeThrusterFlameL != null) Destroy(meleeThrusterFlameL);
        if (meleeThrusterFlameR != null) Destroy(meleeThrusterFlameR);

        normalThrusterFlameL = null;
        normalThrusterFlameR = null;
        boostedThrusterFlameL = null;
        boostedThrusterFlameR = null;
        meleeThrusterFlameL = null;
        meleeThrusterFlameR = null;
    }
    private void ApplyLocomotion(AnimationType type)
    {
        // �̧A�� AnimationType ��� enum �Ƚվ� case
        switch (type)
        {
            case AnimationType.Hover:
                SetHoverMode();
                break;
            case AnimationType.Bipedal:
                SetBipedMode();
                break;
            default:
                SetBipedMode();
                break;
        }
    }


    public void SetBipedMode() { SetAllLayersOff(); anim.SetLayerWeight(bipedLayer, 1f); }
    public void SetHoverMode() { SetAllLayersOff(); anim.SetLayerWeight(hoverLayer, 1f); }
    public void SetAllLayersOff()
    {
        anim.SetLayerWeight(bipedLayer, 0f);
        anim.SetLayerWeight(hoverLayer, 0f);
    }

    public void SetMovementParameters(float horizontal, float vertical)
    {
        anim.SetFloat("x", horizontal);
        anim.SetFloat("y", vertical);
    }

    public void setDashTrigger()
    {
        IsDashAnimationActive = true;
        anim.SetTrigger("dash");
    }
    public void SetDashAnimationLock(bool locked)
    {
        IsDashAnimationActive = locked;
    }
    public void SetIsOnGround(bool onGround)
    {
        anim.SetBool("onGround", onGround);
    }

    public void SetToAttackLayer()
    {
        SmoothSetLayerWeight(One_Hand_Melee_AttackLayer, 1f, attackLayerBlendInTime);
    }
    public void SetOffAttackLayer()
    {
        SmoothSetLayerWeight(One_Hand_Melee_AttackLayer, 0f, attackLayerBlendOutTime);
    }
    public void BeginMeleeDash(bool isLeftHand, MeleeWeaponPartAttribute weaponAttribute)
    {
        anim.SetTrigger("startAttack");
        anim.SetBool("dashing", true);
        int stance = (weaponAttribute == MeleeWeaponPartAttribute.LanceHead) ? 0 : 1;
        anim.SetInteger("stance", stance);
        anim.SetBool("leftHandAttack", isLeftHand);

    }

    private void EnsureAttackStartEventFired()
    {
        if (_attackEventFired) return;
        _attackEventFired = true;
        OnStartAttacking?.Invoke();// Trail/粒子等效果靠這個
    }

    public void StartAttack()
    {
        EnsureAttackStartEventFired();
        anim.SetBool("attacking", true);
    }
    public void StopAttacking()
    {
        Debug.Log("PlayerAnimation: StopAttacking invoked");
        anim.SetBool("attacking", false);
        SetOffAttackLayer();

        _attackEventFired = false;     // 允許下一次攻擊再觸發 OnStartAttacking
        OnStopAttacking?.Invoke();
    }
    public void InvokeStartAttack(float delay)
    {
        if (!IsInvoking(nameof(StartAttack)))
        {
            Invoke(nameof(StartAttack), delay);
        }
    }
    public void StopDashing()
    {
        anim.SetBool("dashing", false);
    }

    public void InvokeStopAttacking()
    {
        if (!IsInvoking(nameof(StopAttacking)))
        {
            Invoke("StopAttacking", 0.2f);
        }
    }
    private void SmoothSetLayerWeight(int layerIndex, float target, float duration)
    {
        if (anim == null) return;
        if (layerIndex < 0) return;

        if (_attackLayerBlendRoutine != null)
            StopCoroutine(_attackLayerBlendRoutine);

        _attackLayerBlendRoutine = StartCoroutine(SmoothSetLayerWeightRoutine(layerIndex, target, duration));
    }

    private System.Collections.IEnumerator SmoothSetLayerWeightRoutine(int layerIndex, float target, float duration)
    {
        float start = anim.GetLayerWeight(layerIndex);

        if (duration <= 0f)
        {
            anim.SetLayerWeight(layerIndex, target);
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            anim.SetLayerWeight(layerIndex, Mathf.Lerp(start, target, a));
            yield return null;
        }

        anim.SetLayerWeight(layerIndex, target);
    }
    private void SetWeaponHoldAllLayersOff()
    {
        if (anim == null) return;

        if (BarehandedLayer >= 0) anim.SetLayerWeight(BarehandedLayer, 0f);
        if (Wielding_Gun_LeftLayer >= 0) anim.SetLayerWeight(Wielding_Gun_LeftLayer, 0f);
        if (Wielding_Gun_RightLayer >= 0) anim.SetLayerWeight(Wielding_Gun_RightLayer, 0f);
        if (Dual_Wielding_Weapon_LeftLayer >= 0) anim.SetLayerWeight(Dual_Wielding_Weapon_LeftLayer, 0f);
        if (Dual_Wielding_Weapon_RightLayer >= 0) anim.SetLayerWeight(Dual_Wielding_Weapon_RightLayer, 0f);
    }

    private void RefreshWeaponHoldLayers()
    {
        if (anim == null || PlayerStats.Instance == null) return;

        bool hasLeft = PlayerStats.Instance.leftHand.HasWeapon;
        bool hasRight = PlayerStats.Instance.rightHand.HasWeapon;

        // 先算目標權重（不要先把現有權重清掉，否則會變成從 0 開始硬切）
        float targetBare = 0f;
        float targetWL = 0f;
        float targetWR = 0f;
        float targetDL = 0f;
        float targetDR = 0f;

        // 0 把武器：徒手
        if (!hasLeft && !hasRight)
        {
            targetBare = 1f;
        }
        // 2 把武器：雙持
        else if (hasLeft && hasRight)
        {
            targetDL = 1f;
            targetDR = 1f;

            int leftHnadWeapon = 0; // 0: none, 1: melee, 2: range
            if (PlayerStats.Instance.leftHand.meleeWeapon != null) leftHnadWeapon = 1;
            else if (PlayerStats.Instance.leftHand.rangeweapon != null) leftHnadWeapon = 2;
            anim.SetInteger("leftHandWeaponType", leftHnadWeapon);

            int rightHnadWeapon = 0; // 0: none, 1: melee, 2: range
            if (PlayerStats.Instance.rightHand.meleeWeapon != null) rightHnadWeapon = 1;
            else if (PlayerStats.Instance.rightHand.rangeweapon != null) rightHnadWeapon = 2;
            anim.SetInteger("rightHandWeaponType", rightHnadWeapon);
        }
        // 1 把武器：單手（左或右）
        else if (hasLeft)
        {
            targetWL = 1f;
        }
        else // hasRight
        {
            targetWR = 1f;
        }

        // 平滑混合到目標
        SmoothSetWeaponHoldWeights(
            targetBare,
            targetWL,
            targetWR,
            targetDL,
            targetDR,
            weaponHoldBlendTime);
    }
    private void SmoothSetWeaponHoldWeights(
    float barehanded,
    float wieldLeft,
    float wieldRight,
    float dualLeft,
    float dualRight,
    float duration)
    {
        if (anim == null) return;

        // 若層不存在（GetLayerIndex = -1），就忽略
        float startBare = (BarehandedLayer >= 0) ? anim.GetLayerWeight(BarehandedLayer) : 0f;
        float startWL = (Wielding_Gun_LeftLayer >= 0) ? anim.GetLayerWeight(Wielding_Gun_LeftLayer) : 0f;
        float startWR = (Wielding_Gun_RightLayer >= 0) ? anim.GetLayerWeight(Wielding_Gun_RightLayer) : 0f;
        float startDL = (Dual_Wielding_Weapon_LeftLayer >= 0) ? anim.GetLayerWeight(Dual_Wielding_Weapon_LeftLayer) : 0f;
        float startDR = (Dual_Wielding_Weapon_RightLayer >= 0) ? anim.GetLayerWeight(Dual_Wielding_Weapon_RightLayer) : 0f;

        if (_weaponHoldBlendRoutine != null)
            StopCoroutine(_weaponHoldBlendRoutine);

        // duration <= 0 就直接設
        if (duration <= 0f)
        {
            if (BarehandedLayer >= 0) anim.SetLayerWeight(BarehandedLayer, barehanded);
            if (Wielding_Gun_LeftLayer >= 0) anim.SetLayerWeight(Wielding_Gun_LeftLayer, wieldLeft);
            if (Wielding_Gun_RightLayer >= 0) anim.SetLayerWeight(Wielding_Gun_RightLayer, wieldRight);
            if (Dual_Wielding_Weapon_LeftLayer >= 0) anim.SetLayerWeight(Dual_Wielding_Weapon_LeftLayer, dualLeft);
            if (Dual_Wielding_Weapon_RightLayer >= 0) anim.SetLayerWeight(Dual_Wielding_Weapon_RightLayer, dualRight);
            return;
        }

        _weaponHoldBlendRoutine = StartCoroutine(WeaponHoldBlendRoutine(
            startBare, startWL, startWR, startDL, startDR,
            barehanded, wieldLeft, wieldRight, dualLeft, dualRight,
            duration));
    }

    private System.Collections.IEnumerator WeaponHoldBlendRoutine(
        float startBare, float startWL, float startWR, float startDL, float startDR,
        float targetBare, float targetWL, float targetWR, float targetDL, float targetDR,
        float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);

            if (BarehandedLayer >= 0) anim.SetLayerWeight(BarehandedLayer, Mathf.Lerp(startBare, targetBare, a));
            if (Wielding_Gun_LeftLayer >= 0) anim.SetLayerWeight(Wielding_Gun_LeftLayer, Mathf.Lerp(startWL, targetWL, a));
            if (Wielding_Gun_RightLayer >= 0) anim.SetLayerWeight(Wielding_Gun_RightLayer, Mathf.Lerp(startWR, targetWR, a));
            if (Dual_Wielding_Weapon_LeftLayer >= 0) anim.SetLayerWeight(Dual_Wielding_Weapon_LeftLayer, Mathf.Lerp(startDL, targetDL, a));
            if (Dual_Wielding_Weapon_RightLayer >= 0) anim.SetLayerWeight(Dual_Wielding_Weapon_RightLayer, Mathf.Lerp(startDR, targetDR, a));

            yield return null;
        }

        // 最後鎖定到精準值
        if (BarehandedLayer >= 0) anim.SetLayerWeight(BarehandedLayer, targetBare);
        if (Wielding_Gun_LeftLayer >= 0) anim.SetLayerWeight(Wielding_Gun_LeftLayer, targetWL);
        if (Wielding_Gun_RightLayer >= 0) anim.SetLayerWeight(Wielding_Gun_RightLayer, targetWR);
        if (Dual_Wielding_Weapon_LeftLayer >= 0) anim.SetLayerWeight(Dual_Wielding_Weapon_LeftLayer, targetDL);
        if (Dual_Wielding_Weapon_RightLayer >= 0) anim.SetLayerWeight(Dual_Wielding_Weapon_RightLayer, targetDR);

        _weaponHoldBlendRoutine = null;
    }

    // ────────────────────────────────────────────────
    //  肩武器
    //
    //  舉起（left/rightShoulderAttacking）：只看「準星的鎖定圈裡有沒有目標」——
    //    有目標 → 自動舉起、轉向前方待命（不用按鍵）
    //    目標消失 shoulderLowerDelay 秒後 → 收回
    //
    //  開火：每一輪射擊觸發 fireShoulder_L / _R，等 Attack_Fire 開始播放才生成子彈
    //    （見 FireShoulderAndWait，由 RangeAttackController 呼叫）。
    //    沒有目標時武器是收著的 —— 請在 Animator 加一條 Idle → Attack_Fire（條件 fireShoulder_*），
    //    武器會直接迅速轉向前方開火。
    // ────────────────────────────────────────────────

    [Header("Shoulder Weapon")]
    [Tooltip("鎖定圈裡的目標消失後，肩武器維持舉起幾秒才收回。")]
    [SerializeField, Min(0f)] private float shoulderLowerDelay = 1f;

    [Tooltip("觸發開火後最多等幾秒讓 Attack_Fire 開始播放。逾時就直接生成子彈 ——\n" +
             "避免 Animator 沒接好（缺轉場、圖層權重 0）時肩武器整個打不出去。")]
    [SerializeField, Min(0.05f)] private float shoulderFireStateTimeout = 0.5f;

    private static readonly int LeftShoulderAttackingHash = Animator.StringToHash("leftShoulderAttacking");
    private static readonly int RightShoulderAttackingHash = Animator.StringToHash("rightShoulderAttacking");

    private float _lastShoulderTargetTime = -1f;
    private bool _leftShoulderRaised, _rightShoulderRaised;

    void Update()
    {
        UpdateShoulderAim();
    }

    private void UpdateShoulderAim()
    {
        if (anim == null) return;

        PlayerAiming aiming = PlayerAiming.Instance;
        if (aiming != null && aiming.lockOn)
            _lastShoulderTargetTime = Time.time;

        bool raise = _lastShoulderTargetTime >= 0f && (Time.time - _lastShoulderTargetTime) <= shoulderLowerDelay;

        bool left = raise && HasShoulderWeapon(attackManager != null ? attackManager.leftShoulderWeapon : null);
        bool right = raise && HasShoulderWeapon(attackManager != null ? attackManager.rightShoulderWeapon : null);

        if (left != _leftShoulderRaised)
        {
            _leftShoulderRaised = left;
            anim.SetBool(LeftShoulderAttackingHash, left);
        }
        if (right != _rightShoulderRaised)
        {
            _rightShoulderRaised = right;
            anim.SetBool(RightShoulderAttackingHash, right);
        }
    }

    /// <summary>這個肩槽有裝遠程武器（AttackManager.ClearWeaponOutput 會把 bullet 清成 null）。</summary>
    private static bool HasShoulderWeapon(Weapon w) => w != null && w.bullet != null;

    // 開火後座的狀態。Attack_Fire 0 是給連射用的第二份複本（跟手持武器的 Range_Fire / Range_Fire 0 一樣），
    // 沒有建也沒關係，只是多比對一個 hash。
    private static readonly int AttackFireStateHash = Animator.StringToHash("Attack_Fire");
    private static readonly int AttackFire0StateHash = Animator.StringToHash("Attack_Fire 0");

    private static bool IsShoulderFireState(int shortNameHash)
    {
        return shortNameHash == AttackFireStateHash || shortNameHash == AttackFire0StateHash;
    }

    /// <summary>
    /// 觸發肩武器的開火動畫，等到 Attack_Fire「開始播放」那一刻才結束。
    /// RangeAttackController 每一輪射擊都 yield 它一次，之後才生成子彈。
    ///
    /// 「開始播放」= 往 Attack_Fire（或 Attack_Fire 0）的轉場開始的那一刻，包含：
    ///   Idle → Attack_Fire（沒有目標時直接開火）、Attack → Attack_Fire、
    ///   Attack_Fire ↔ Attack_Fire 0（連射）、Attack_Fire → Attack_Fire（自我轉場）。
    ///
    /// Animator 沒有 fireShoulder_* 參數、或圖層不存在時立刻結束（子彈照常生成，不會卡住）；
    /// 等超過 shoulderFireStateTimeout 也會結束，並清掉沒被消耗的 Trigger。
    /// </summary>
    public IEnumerator FireShoulderAndWait(bool isLeft)
    {
        if (anim == null) yield break;

        int layer = isLeft ? Shoulder_Weapon_LeftLayer : Shoulder_Weapon_RightLayer;
        if (layer < 0 || !HasShoulderFireTrigger(isLeft)) yield break;

        int trigger = isLeft ? FireShoulderLeftTriggerHash : FireShoulderRightTriggerHash;

        // 觸發前的狀態當基準：之後只要「進入了另一個開火狀態」或「同一個開火狀態從頭播」就算開始
        bool hadFire = TryGetFireStateKey(layer, out int baseHash, out float baseTime);

        anim.SetTrigger(trigger);

        float waited = 0f;
        while (waited < shoulderFireStateTimeout)
        {
            yield return null;
            waited += Time.deltaTime;

            bool inFire = TryGetFireStateKey(layer, out int hash, out float time);
            if (inFire && (!hadFire || hash != baseHash || time < baseTime - 0.0001f))
                yield break;   // Attack_Fire 開始播放 → 生成子彈

            // 還在上一發的後座裡：持續更新基準，才偵測得到之後「從頭播」
            hadFire = inFire;
            baseHash = hash;
            baseTime = time;
        }

        // 逾時：Animator 沒反應（缺轉場、圖層權重 0…）→ 不等了，清掉 Trigger 免得之後無緣無故補播一次
        anim.ResetTrigger(trigger);
    }

    /// <summary>
    /// 這個圖層「正在進入或正在播」的開火狀態。轉場中優先看目標狀態 ——
    /// 0.01 秒的轉場可能整個落在兩幀之間，所以也接受直接看到目前狀態已經是開火狀態。
    /// </summary>
    private bool TryGetFireStateKey(int layer, out int hash, out float normalizedTime)
    {
        if (anim.IsInTransition(layer))
        {
            AnimatorStateInfo next = anim.GetNextAnimatorStateInfo(layer);
            if (IsShoulderFireState(next.shortNameHash))
            {
                hash = next.shortNameHash;
                normalizedTime = next.normalizedTime;
                return true;
            }
        }

        AnimatorStateInfo cur = anim.GetCurrentAnimatorStateInfo(layer);
        hash = cur.shortNameHash;
        normalizedTime = cur.normalizedTime;
        return IsShoulderFireState(cur.shortNameHash);
    }

    public void LeftWeaponMuzzleFlash()
    {
        leftAttackFeedback?.PlayFeedbacks(this.transform.position);
        TriggerFireLeft();
    }
    public void RightWeaponMuzzleFlash()
    {
        rightAttackFeedback?.PlayFeedbacks(this.transform.position);
        TriggerFireRight();
    }

    // 遠程武器每次生成子彈時觸發開槍動畫：
    //   左手 → fire_L（Dual_Wielding_Weapon_Left 層）
    //   右手 → fire_R（Dual_Wielding_Weapon_Right 層）
    // RangeAttackController 在每顆子彈 Spawn 前呼叫 Left/RightWeaponMuzzleFlash()，
    // 散彈槍一發多顆時同一幀會重複 SetTrigger，效果等同一次，不會多播。
    private static readonly int FireLeftTriggerHash = Animator.StringToHash("fire_L");
    private static readonly int FireRightTriggerHash = Animator.StringToHash("fire_R");

    public void TriggerFireLeft()
    {
        if (anim == null) return;
        anim.SetTrigger(FireLeftTriggerHash);
    }

    public void TriggerFireRight()
    {
        if (anim == null) return;
        anim.SetTrigger(FireRightTriggerHash);
    }

    // 肩武器的開火後座 Trigger：
    //   左肩 → fireShoulder_L（Shoulder_Weapon_Left 層）
    //   右肩 → fireShoulder_R（Shoulder_Weapon_Right 層）
    // Animator 裡還沒建這兩個參數時 FireShoulderAndWait 直接跳過，不會每發子彈都噴一次警告。
    private static readonly int FireShoulderLeftTriggerHash = Animator.StringToHash("fireShoulder_L");
    private static readonly int FireShoulderRightTriggerHash = Animator.StringToHash("fireShoulder_R");
    private bool _shoulderFireParamsChecked;
    private bool _hasFireShoulderL, _hasFireShoulderR;

    private bool HasShoulderFireTrigger(bool isLeft)
    {
        if (!_shoulderFireParamsChecked)
        {
            _shoulderFireParamsChecked = true;
            foreach (AnimatorControllerParameter p in anim.parameters)
            {
                if (p.nameHash == FireShoulderLeftTriggerHash) _hasFireShoulderL = true;
                else if (p.nameHash == FireShoulderRightTriggerHash) _hasFireShoulderR = true;
            }
        }
        return isLeft ? _hasFireShoulderL : _hasFireShoulderR;
    }
    public void DustEffect()
    {
        dustFeedback?.PlayFeedbacks(this.transform.position);
    }
    public void DustEffect_OnShip()
    {
        dustFeedback_OnShip?.PlayFeedbacks(this.transform.position);
    }
    public void StopDustEffect()
    {
        dustFeedback?.StopFeedbacks();
        dustFeedback_OnShip?.StopFeedbacks();
    }
    public void AnimEvent_MeleeImpact()
    {
        PlayMeleeHitFeedback(transform.position);
    }

    public void AnimEvent_SwordSwing()
    {
        swordSwingFeedback?.PlayFeedbacks(this.transform.position);
    }

    /// <summary>
    /// 近戰命中的打擊回饋。由 MeleeAttackController 在 hitbox 實際造成傷害時呼叫，
    /// 所以只有真的打到才會播 —— 揮空不會。
    /// </summary>
    public void PlayMeleeHitFeedback(Vector3 position)
    {
        meleeAttackFeedback?.PlayFeedbacks(position);
    }

    // ────────────────────────────────────────────────
    //  近戰連段的 Animation Event
    //
    //  這五個方法由攻擊 clip 上的 Animation Event 呼叫，單純轉發給
    //  MeleeAttackController。全部無參數 —— 「哪隻手在揮」由控制器的內部狀態
    //  決定，動畫不需要知道。
    //
    //  放置順序：DashStart → HitboxOn → HitboxOff → Brake → ComboWindow → StepEnd
    //
    //  DashStart 只有 dashMode != None 的段需要，放在舉刀之後、HitboxOn 之前
    //  （先舉刀、再突進、再劈下）。
    //
    //  Brake 煞停突進動量，時機自由 —— 劈砍類放在刀刃落到底的瞬間（衝勢轉成
    //  打擊感），突刺類可以晚一點讓動量延續。沒放的段就不煞車。
    //
    //  ComboWindow 一般抓在動畫 60~75% 的位置；太早開連段會失控，太晚開玩家
    //  會覺得按鍵沒反應。
    //
    //  ★ AnimEvent_MeleeStepEnd 千萬不能漏，漏了會卡在攻擊狀態，
    //    只能靠 MeleeAttackStep.maxStepDuration 兜底（會有明顯卡頓）。
    // ────────────────────────────────────────────────

    public void AnimEvent_MeleeDashStart() => meleeController?.OnDashStart();

    public void AnimEvent_MeleeHitboxOn() => meleeController?.OnHitboxOn();

    public void AnimEvent_MeleeHitboxOff() => meleeController?.OnHitboxOff();

    public void AnimEvent_MeleeBrake() => meleeController?.OnBrake();

    public void AnimEvent_MeleeComboWindow() => meleeController?.OnComboWindow();

    public void AnimEvent_MeleeStepEnd() => meleeController?.OnStepEnd();
    public void AnimEvent_Reload()
    {
        reloadFeedback?.PlayFeedbacks(this.transform.position);
    }


    public void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Hit:" + collision.gameObject);
        if (collision.gameObject.tag == "Enemy" && attackManager.playerRb.linearVelocity.magnitude > 8f)
        {
            meleeAttackFeedback.PlayFeedbacks(this.transform.position);
        }
    }

    public void SetSimulationSpaceLandship()
    {
        if (dustParticle == null) return;
        var main = dustParticle.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Custom;
        main.customSimulationSpace = landshipTransform;

        for (int i = 0; i < leftMuzzle.Length; i++)
        {
            var leftMuzzleMain = leftMuzzle[i].main;
            leftMuzzleMain.simulationSpace = ParticleSystemSimulationSpace.Custom;
            leftMuzzleMain.customSimulationSpace = landshipTransform;
        }
        for (int i = 0; i < rightMuzzle.Length; i++)
        {
            var rightMuzzleMain = rightMuzzle[i].main;
            rightMuzzleMain.simulationSpace = ParticleSystemSimulationSpace.Custom;
            rightMuzzleMain.customSimulationSpace = landshipTransform;
        }
    }

    public void SetSimulationSpaceWorld()
    {
        if (dustParticle == null) return;
        var main = dustParticle.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        for (int i = 0; i < leftMuzzle.Length; i++)
        {
            var leftMuzzleMain = leftMuzzle[i].main;
            leftMuzzleMain.simulationSpace = ParticleSystemSimulationSpace.World;
        }
        for (int i = 0; i < rightMuzzle.Length; i++)
        {
            var rightMuzzleMain = rightMuzzle[i].main;
            rightMuzzleMain.simulationSpace = ParticleSystemSimulationSpace.World;
        }
    }

    public void PlayWalkFeedback()
    {
        walkFeedback?.PlayFeedbacks(this.transform.position);
    }
    public void StopWalkFeedback()
    {
        walkFeedback?.StopFeedbacks();
    }
}