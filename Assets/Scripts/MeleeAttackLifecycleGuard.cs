using UnityEngine;

/// <summary>
/// 防呆用：把近戰攻擊的「收尾」從 Animator Event 解耦出來。
/// 目的：就算 Combo2 沒成功轉場、或 Animation Event 漏打，attacking 也不會卡死。
/// 
/// 用法：把這個掛在 Player（或有 Animator/PlayerAnimation 的同一個 prefab）上即可。
/// 不需要改你現有 PlayerMovement / PlayerController 的呼叫流程。
/// </summary>
public class MeleeAttackLifecycleGuard : MonoBehaviour
{
    [Header("Auto Find")]
    [SerializeField] private PlayerAnimation playerAnimation;
    [SerializeField] private Animator anim;

    [Header("Animator Params")]
    [SerializeField] private string attackingBoolName = "attacking";
    [SerializeField] private string leftComboIntName = "LeftHandCombo";
    [SerializeField] private string rightComboIntName = "RightHandCombo";

    [Header("Layer / State (optional)")]
    [Tooltip("只在這個 layer 監控 normalizedTime（留空 = 用 layer 0）")]
    [SerializeField] private string meleeLayerNameContains = "Melee";
    [Tooltip("判定動畫播完的 normalizedTime 閾值。0.98 比較保守。")]
    [Range(0.5f, 1f)]
    [SerializeField] private float endNormalized = 0.98f;

    [Header("Failsafe")]
    [Tooltip("攻擊狀態最長允許多久（秒）。超過就強制 Stop，避免卡死。")]
    [SerializeField] private float maxAttackSeconds = 2.0f;
    [Tooltip("當 combo2 被排隊但一直沒進入（轉場失敗）時，允許多久再強制收尾。")]
    [SerializeField] private float queuedComboFailSafeSeconds = 1.0f;

    private int _meleeLayerIndex = 0;
    private int _hashAttacking;
    private int _hashLeftCombo;
    private int _hashRightCombo;

    private bool _wasAttacking = false;
    private float _attackStartTime = 0f;
    private float _comboQueuedTime = 0f;   // 當 combo2 被排隊的時間（用來偵測「排隊但沒進去」）

    private void Awake()
    {
        if (playerAnimation == null) playerAnimation = GetComponentInChildren<PlayerAnimation>(true);
        if (anim == null) anim = (playerAnimation != null) ? playerAnimation.GetComponent<Animator>() : null;
        if (anim == null) anim = GetComponentInChildren<Animator>(true);

        _hashAttacking = Animator.StringToHash(attackingBoolName);
        _hashLeftCombo = Animator.StringToHash(leftComboIntName);
        _hashRightCombo = Animator.StringToHash(rightComboIntName);

        _meleeLayerIndex = 0;
        if (anim != null && !string.IsNullOrEmpty(meleeLayerNameContains))
        {
            for (int i = 0; i < anim.layerCount; i++)
            {
                string ln = anim.GetLayerName(i);
                if (!string.IsNullOrEmpty(ln) && ln.Contains(meleeLayerNameContains))
                {
                    _meleeLayerIndex = i;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        if (anim == null || playerAnimation == null) return;

        bool attacking = anim.GetBool(_hashAttacking);

        // rising edge：開始攻擊
        if (attacking && !_wasAttacking)
        {
            _attackStartTime = Time.time;
            _comboQueuedTime = 0f;
        }

        // falling edge：結束攻擊（外部已收尾）
        if (!attacking && _wasAttacking)
        {
            _comboQueuedTime = 0f;
        }

        _wasAttacking = attacking;

        if (!attacking) return;

        // 讀 combo 狀態（>=2 表示想接 combo2 / 或已在 combo2）
        int leftCombo = anim.GetInteger(_hashLeftCombo);
        int rightCombo = anim.GetInteger(_hashRightCombo);
        bool wantsCombo2 = (leftCombo >= 2) || (rightCombo >= 2);

        // 記錄「第一次被排隊」的時間，用於偵測轉場失敗
        if (wantsCombo2 && _comboQueuedTime <= 0f)
            _comboQueuedTime = Time.time;

        // === Failsafe 1：整段攻擊超時 ===
        if (Time.time - _attackStartTime > maxAttackSeconds)
        {
            ForceStop("maxAttackSeconds");
            return;
        }

        // === Failsafe 2：已排隊 combo2 但太久都未進入（常見於 spin input / transition miss）===
        // 如果 combo2 已排隊，而且超過一定時間，但目前狀態似乎仍未進入第二段，就強制收尾避免卡死。
        if (wantsCombo2 && _comboQueuedTime > 0f && (Time.time - _comboQueuedTime) > queuedComboFailSafeSeconds)
        {
            // 盡量判斷「仍在第一段」：normalizedTime 已接近結尾，但還在同一個 state（或沒有成功進入下一段）
            AnimatorStateInfo st = anim.GetCurrentAnimatorStateInfo(_meleeLayerIndex);
            bool nearEnd = st.normalizedTime >= endNormalized;
            bool transitioning = anim.IsInTransition(_meleeLayerIndex);

            if (nearEnd && !transitioning)
            {
                // 清 combo，然後收尾，避免下一次被卡住
                anim.SetInteger(_hashLeftCombo, 0);
                anim.SetInteger(_hashRightCombo, 0);
                ForceStop("queuedComboFailSafe");
                return;
            }
        }

        // === 正常收尾：動畫播完且沒有再排隊下一段 ===
        AnimatorStateInfo s = anim.GetCurrentAnimatorStateInfo(_meleeLayerIndex);
        if (!anim.IsInTransition(_meleeLayerIndex) && s.normalizedTime >= endNormalized)
        {
            // 若沒有想接 combo2，就結束
            if (!wantsCombo2)
            {
                ForceStop("normalEnd");
                return;
            }

            // wantsCombo2 但也已到尾端、且沒有進入轉場 → 很可能轉場 miss；交給 failsafe 2 或直接收尾
            // 這裡保守：若已到尾端且 wantsCombo2，等 failsafe 2 的時間到再收尾（避免誤殺剛好要進轉場的情況）
        }
    }

    private void ForceStop(string reason)
    {
        // 這裡不要依賴 Animation Event；直接走 PlayerAnimation 的 StopAttacking()（會順便 SetOffAttackLayer & OnStopAttacking）
        Debug.LogWarning($"MeleeAttackLifecycleGuard: ForceStop ({reason})");
        playerAnimation.StopAttacking();
    }
}
