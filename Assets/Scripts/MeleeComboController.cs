using UnityEngine;

public class MeleeComboController : MonoBehaviour
{
    [Header("References")]
    public Animator anim;

    [Header("Config")]
    [Tooltip("0 = Heavy Slashing, 1 = Heavy Piercing")]
    public int weaponStance = 0;

    [Range(0f, 1f)]
    public float comboConsumeThreshold = 0.9f;   // 去到尾段先消耗 buffer 並出下一段

    [Tooltip("After setting a trigger, give Animator time to enter the attack state before we reset.")]
    public float enterAttackGraceTime = 0.15f;

    [Header("Input Buffer")]
    [Tooltip("Seconds to remember a queued input.")]
    public float inputBufferTime = 0.2f;

    [Header("Runtime")]
    public int currentCombo = 0;

    private int One_Hand_Melee_AttackLayer = -1;

    // 記住 combo 屬於邊隻手（比對 full path 用）
    private bool _activeIsLeftHand = true;

    // 防止剛 SetTrigger 就被 reset
    private float _enterGraceTimer = 0f;

    // ★ input buffer：>0 代表有一次「待消耗」輸入
    private float _bufferTimer = 0f;
    [Header("Combo Forgiveness")]
    [Range(0f, 1f)] public float comboWindowOpen = 0.75f; // 由 0.9 放寬到 0.75
    public float lateForgiveness = 0.12f;                 // 播完後仍可接的時間
    private float _lateTimer = 0f;
    void Awake()
    {
        if (anim == null) anim = GetComponent<Animator>();
        One_Hand_Melee_AttackLayer = anim.GetLayerIndex("One_Hand_Melee_Attack");
    }

    void Update()
    {
        if (anim == null || One_Hand_Melee_AttackLayer < 0) return;

        if (_enterGraceTimer > 0f) _enterGraceTimer -= Time.deltaTime;
        if (_bufferTimer > 0f) _bufferTimer -= Time.deltaTime;

        if (currentCombo == 0) return;

        // transition 中唔做 reset / 唔消耗 buffer
        if (anim.IsInTransition(One_Hand_Melee_AttackLayer)) return;

        var s = anim.GetCurrentAnimatorStateInfo(One_Hand_Melee_AttackLayer);

        if (s.normalizedTime >= 1f)
        {
            _lateTimer = Mathf.Max(_lateTimer, lateForgiveness);
        }

        if (_lateTimer > 0f)
            _lateTimer -= Time.deltaTime;

        // grace time 期間：唔好 reset（等 Animator 入 Hit state）
        if (_enterGraceTimer <= 0f)
        {
            // 離開 Hit state -> reset
            if (!IsInAnyHitState(s, _activeIsLeftHand, weaponStance))
            {
                ResetCombo();
                return;
            }

            // Hit_3 完結 -> reset
            if (IsHitState(s, _activeIsLeftHand, weaponStance, 3) && s.normalizedTime >= 1f)
            {
                ResetCombo();
                return;
            }
        }

        // ★ 如果有 buffer，等到窗口打開先消耗並出下一段
        if (_bufferTimer > 0f)
        {
            float t = s.normalizedTime % 1f;

            // 窗口打開（較早） OR 播完後寬限時間內
            if (t > comboWindowOpen || _lateTimer > 0f)
            {
                _lateTimer = 0f;
                ConsumeBufferedInput();
            }
        }

        // ★ 如果 buffer 放到過期，代表主人按得太早但冇等到窗口（或被打斷）
        // 你可以選擇過期就 reset，或者乜都唔做。
        // 我建議「唔自動 reset」，只係失去一次輸入：
        // if (_bufferTimer <= 0f) { /* do nothing */ }
    }

    public void MeleeAttack(bool isLeftHand)
    {
        if (anim == null || One_Hand_Melee_AttackLayer < 0) return;

        _activeIsLeftHand = isLeftHand;
        anim.SetBool("leftHandAttack", isLeftHand);

        // 起手：即刻出 Hit 1（唔用 buffer）
        if (currentCombo == 0)
        {
            FireHitTrigger(1);
            currentCombo = 1;

            // 清 buffer（避免起手同時排到下一段）
            _bufferTimer = 0f;
            return;
        }

        // 已經喺 combo：只係「記住呢次按鍵」
        _bufferTimer = inputBufferTime;
    }

    private void ConsumeBufferedInput()
    {
        // 消耗一次 buffer
        _bufferTimer = 0f;

        if (currentCombo == 1)
        {
            FireHitTrigger(2);
            currentCombo = 2;
        }
        else if (currentCombo == 2)
        {
            FireHitTrigger(3);
            currentCombo = 3;
        }
        // currentCombo == 3：唔再接（等 Hit_3 完結 reset）
    }

    private void FireHitTrigger(int hitIndex)
    {
        anim.SetTrigger($"Hit {hitIndex}");
        _enterGraceTimer = enterAttackGraceTime;
    }

    private void ResetCombo()
    {
        currentCombo = 0;
        _bufferTimer = 0f;

        anim.ResetTrigger("Hit 1");
        anim.ResetTrigger("Hit 2");
        anim.ResetTrigger("Hit 3");
    }

    // -------------------------
    // FullPathHash matching
    // -------------------------

    private bool IsInAnyHitState(AnimatorStateInfo s, bool isLeftHand, int stance)
    {
        return IsHitState(s, isLeftHand, stance, 1)
            || IsHitState(s, isLeftHand, stance, 2)
            || IsHitState(s, isLeftHand, stance, 3);
    }

    private bool IsHitState(AnimatorStateInfo s, bool isLeftHand, int stance, int combo)
    {
        string p = BuildFullPath(isLeftHand, stance, combo);
        if (string.IsNullOrEmpty(p)) return false;
        return s.fullPathHash == Animator.StringToHash(p);
    }

    public string BuildFullPath(bool isLeftHand, int stance, int combo)
    {
        string layer = "One_Hand_Melee_Attack.";

        string sm = null;
        if (isLeftHand)
            sm = stance == 0 ? "Heavy Slashing_L" :
                 stance == 1 ? "Heavy Piercing_L" : null;
        else
            sm = stance == 0 ? "Heavy Slashing_R" :
                 stance == 1 ? "Heavy Piercing_R" : null;

        if (sm == null) return null;
        if (combo < 1 || combo > 3) return null;

        // 依你 Animator：Hit_1 / Hit_2 / Hit_3
        return $"{layer}{sm}.Hit_{combo}";
    }
}
