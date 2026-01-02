using UnityEngine;
using System;

public class PlayerAnimation : MonoBehaviour
{
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

    private float baseHeight;
    private Vector3 baseCenter;
    private float baseGroundPointY;

    // ===== Attack Layer Blend (Smooth) =====
    [SerializeField] private float attackLayerBlendInTime = 0.12f;   // 可調：進入 Attack Layer 的時間
    [SerializeField] private float attackLayerBlendOutTime = 0.18f;  // 可調：退出 Attack Layer 的時間
    private Coroutine _attackLayerBlendRoutine;
    public event Action OnStopAttacking;
    void Awake()
    {
        anim = anim != null ? anim : GetComponent<Animator>();
        capsuleCollider = capsuleCollider!=null ? capsuleCollider: GetComponent<CapsuleCollider>();

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
        // 先給一個安全預設：徒手
        SetWeaponHoldAllLayersOff();
        if (BarehandedLayer >= 0) anim.SetLayerWeight(BarehandedLayer, 1f);

        // 等 PlayerStats Instance 準備好
        while (PlayerStats.Instance == null)
            yield return null;

        PlayerStats.Instance.OnLegVisualChanged += ApplyLegVisualChange;
        PlayerStats.Instance.OnHandWeaponDataChanged += RefreshWeaponHoldLayers;

        ApplyLegVisualChange(PlayerStats.Instance.CurrentLegVisual);
        RefreshWeaponHoldLayers();
    }
    private void OnDisable()
    {
        if (_initRoutine != null) StopCoroutine(_initRoutine);

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.OnLegVisualChanged -= ApplyLegVisualChange;
            PlayerStats.Instance.OnHandWeaponDataChanged -= RefreshWeaponHoldLayers;
        }
    }

    private void ApplyLegVisualChange(VisualChange vc)
    {
        if (vc == null) return;

        ApplyColliderHeightOffset(vc.heightOffset);
        ApplyLocomotion(vc.animationType);
    }

    private void ApplyColliderHeightOffset(float heightOffset)
    {
        if (capsuleCollider == null) return;

        float newHeight = Mathf.Max(0.1f, baseHeight + heightOffset);
        capsuleCollider.height = newHeight;

        // 固定頭頂：高度增加，center 往下移一半
        var c = baseCenter;
        c.y -= (newHeight - baseHeight) * 0.5f;
        capsuleCollider.center = c;

        // 調整 groundPoint 位置
        if (groundPoint != null)
        {
            Vector3 gp = groundPoint.localPosition;
            gp.y = baseGroundPointY - (newHeight - baseHeight);
            groundPoint.localPosition = gp;
        }
    }

    private void ApplyLocomotion(AnimationType type)
    {
        // 依你的 AnimationType 實際 enum 值調整 case
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

    // ===== 你原本的函式可以保留 =====
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


    public void StartAttack()
    {
        anim.SetBool("attacking", true);
    }
    public void StopDashing()
    {
        anim.SetBool("dashing", false);
    }
    public void InvokeStopAttacking()
    {
        CancelInvoke(nameof(StopAttacking));
        Invoke("StopAttacking", 0.5f);
    }
    public void StopAttacking()
    {
        Debug.Log("PlayerAnimation: StopAttacking invoked");
        anim.SetBool("attacking", false);
        SetOffAttackLayer();
        // 通知外界：攻擊已停止（誰想聽就訂閱）
        OnStopAttacking?.Invoke();
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
        bool hasRight = PlayerStats.Instance.rightHand.HasWeapon; // leftHand/rightHand.weapon 是 RangeWeaponInstance :contentReference[oaicite:3]{index=3}

        SetWeaponHoldAllLayersOff();

        // 0 把武器（Barehanded）
        if (!hasLeft && !hasRight)
        {
            if (BarehandedLayer >= 0) anim.SetLayerWeight(BarehandedLayer, 1f);
            return;
        }

        // 2 把武器（Dual Wield）
        if (hasLeft && hasRight)
        {
            if (Dual_Wielding_Weapon_LeftLayer >= 0) anim.SetLayerWeight(Dual_Wielding_Weapon_LeftLayer, 1f);
            if (Dual_Wielding_Weapon_RightLayer >= 0) anim.SetLayerWeight(Dual_Wielding_Weapon_RightLayer, 1f);
            int leftHnadWeapon = 0; // 0: none, 1: melee, 2: range
            if (PlayerStats.Instance.leftHand.meleeWeapon != null)
            {
                leftHnadWeapon = 1;
            }else if (PlayerStats.Instance.leftHand.rangeweapon != null)
            {
                leftHnadWeapon = 2;
            }
            anim.SetInteger("leftHandWeaponType", leftHnadWeapon);
            int rightHnadWeapon = 0; // 0: none, 1: melee, 2: range
            if (PlayerStats.Instance.rightHand.meleeWeapon != null)
            {
                rightHnadWeapon = 1;
            }
            else if (PlayerStats.Instance.rightHand.rangeweapon != null)
            {
                rightHnadWeapon = 2;
            }
            anim.SetInteger("rightHandWeaponType", rightHnadWeapon);
            return;
        }

        // 1 把武器（單手）
        if (hasLeft)
        {
            if (Wielding_Gun_LeftLayer >= 0) anim.SetLayerWeight(Wielding_Gun_LeftLayer, 1f);
        }
        else // hasRight
        {
            if (Wielding_Gun_RightLayer >= 0) anim.SetLayerWeight(Wielding_Gun_RightLayer, 1f);
        }
    }
}
