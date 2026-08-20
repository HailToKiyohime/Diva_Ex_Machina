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

    public void ShoulderWeaponAttackLeft()
    {
        CancelInvoke("StopShoulderWeaponAttackLeft");
        anim.SetBool("leftShoulderAttacking", true);
        Invoke("StopShoulderWeaponAttackLeft", 3f);
    }
    public void ShoulderWeaponAttackRight()
    {
        CancelInvoke("StopShoulderWeaponAttackRight");
        anim.SetBool("rightShoulderAttacking", true);
        Invoke("StopShoulderWeaponAttackRight", 3f);
    }

    public void StopShoulderWeaponAttackLeft()
    {
        ResetShoulderWeaponFireGate(true);
        anim.SetBool("leftShoulderAttacking", false);
    }
    public void StopShoulderWeaponAttackRight()
    {
        ResetShoulderWeaponFireGate(false);
        anim.SetBool("rightShoulderAttacking", false);
    }

    private static readonly int AttackStateHash = Animator.StringToHash("Attack");

    // 記錄「進入 Attack state」的時間點（秒）
    private float _leftShoulderAttackEnteredTime = -1f;
    private float _rightShoulderAttackEnteredTime = -1f;

    // 用來偵測“剛進入 Attack state”的邊沿
    private bool _leftShoulderWasInAttack = false;
    private bool _rightShoulderWasInAttack = false;

    /// <summary>
    /// Shoulder layer 已完成轉場且在 Attack state 後，延遲 fireDelaySeconds 秒才回傳 true
    /// </summary>
    public bool IsShoulderWeaponReadyToFire(bool isLeft, float fireDelaySeconds)
    {
        if (anim == null) return false;

        int layer = isLeft ? Shoulder_Weapon_LeftLayer : Shoulder_Weapon_RightLayer;
        if (layer < 0) return false;

        // 還在任何 transition 中：不允許
        if (anim.IsInTransition(layer))
        {
            // transition 期間也視為未進入 attack
            if (isLeft) _leftShoulderWasInAttack = false;
            else _rightShoulderWasInAttack = false;
            return false;
        }

        var st = anim.GetCurrentAnimatorStateInfo(layer);
        bool inAttack = (st.shortNameHash == AttackStateHash);

        if (isLeft)
        {
            if (inAttack)
            {
                // 第一次進入 Attack 的那一幀
                if (!_leftShoulderWasInAttack)
                {
                    _leftShoulderWasInAttack = true;
                    _leftShoulderAttackEnteredTime = Time.time;
                }

                return (Time.time - _leftShoulderAttackEnteredTime) >= fireDelaySeconds;
            }
            else
            {
                // 離開 Attack：重置
                _leftShoulderWasInAttack = false;
                _leftShoulderAttackEnteredTime = -1f;
                return false;
            }
        }
        else
        {
            if (inAttack)
            {
                if (!_rightShoulderWasInAttack)
                {
                    _rightShoulderWasInAttack = true;
                    _rightShoulderAttackEnteredTime = Time.time;
                }

                return (Time.time - _rightShoulderAttackEnteredTime) >= fireDelaySeconds;
            }
            else
            {
                _rightShoulderWasInAttack = false;
                _rightShoulderAttackEnteredTime = -1f;
                return false;
            }
        }
    }
    public void ResetShoulderWeaponFireGate(bool isLeft)
    {
        if (isLeft)
        {
            _leftShoulderWasInAttack = false;
            _leftShoulderAttackEnteredTime = -1f;
        }
        else
        {
            _rightShoulderWasInAttack = false;
            _rightShoulderAttackEnteredTime = -1f;
        }
    }

    public void LeftWeaponMuzzleFlash()
    {
        leftAttackFeedback?.PlayFeedbacks(this.transform.position);
    }
    public void RightWeaponMuzzleFlash()
    {
        rightAttackFeedback?.PlayFeedbacks(this.transform.position);
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