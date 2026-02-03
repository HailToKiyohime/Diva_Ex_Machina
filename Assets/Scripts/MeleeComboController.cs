using System;
using UnityEngine;
using UnityEngine.Animations.Rigging;

/// <summary>
/// Combo rules (方案 B):
/// - 每一段只吃一次輸入（不能按住一直 refresh）
/// - 錯過窗口且沒有 pending：在 Hit_1 / Hit_2 尾段自動 EndAttack() -> Attacking=false，Animator 才能 Exit
/// - Dash 結束由 PlayerMovement 呼叫 FireHit1FromDash(isLeftHand) 觸發 Hit_1
/// </summary>
public class MeleeComboController : MonoBehaviour
{
    [Header("Animator")]
    public Animator anim;

    [Header("Layer Weight (One_Hand_Melee_Attack)")]
    [SerializeField] private PlayerAnimation playerAnimation;

    [Header("Attack Manager (for melee reload)")]
    [SerializeField] private AttackManager attackManager;


    [Tooltip("Animator layer name that contains Dash/Hit_1/Hit_2/Hit_3 states")]
    [SerializeField] private string meleeLayerName = "One_Hand_Melee_Attack";
    private int _meleeLayer = -1;

    [Header("Animator Params (match your controller)")]
    [SerializeField] private string paramAttacking = "Attacking";
    [SerializeField] private string paramLeftHandAttack = "leftHandAttack";
    [SerializeField] private string paramRightHandAttack = "rightHandAttack";
    [SerializeField] private string trigDash = "Dash";
    [SerializeField] private string trigHit1 = "Hit 1";
    [SerializeField] private string trigHit2 = "Hit 2";
    [SerializeField] private string trigHit3 = "Hit 3";

    [Header("State Names (must match Animator state names)")]
    [SerializeField] private string stateDash = "Dash";
    [SerializeField] private string stateHit1 = "Hit_1";
    [SerializeField] private string stateHit2 = "Hit_2";
    [SerializeField] private string stateHit3 = "Hit_3";

    [Header("Combo Windows (normalizedTime % 1)")]
    [Tooltip("Buffer validity seconds (for early press). Example: 0.2")]
    [SerializeField] private float inputBufferTime = 0.2f;

    [Tooltip("When normalized time >= this, buffered input may be consumed.")]
    [Range(0f, 1f)]
    [SerializeField] private float comboWindowOpen = 0.70f;

    [Tooltip("After this normalized time, we stop consuming buffer to avoid late hard-cuts.")]
    [Range(0f, 1f)]
    [SerializeField] private float comboConsumeThreshold = 0.90f;

    [Tooltip("If no pending input and we're past this time, end attack (set Attacking=false) so Hit_1/Hit_2 can Exit.")]
    [Range(0.8f, 1f)]
    [SerializeField] private float autoEndAt = 0.98f;


    [SerializeField] private BoxCollider meleeAttackWall; // A box collider that enables/disables, it is larger than the player collider to prevent enemy from glitching through player during melee attack due to the capsule collider being smaller than the player model and it round shape


    public int handInUse = 0; // 0 none, 1 left, 2 right 

    // runtime
    public int currentCombo = 0;            // 0 none, 1/2/3 current stage
    private bool _isLeftHand = true;

    // one-shot buffer per stage
    private bool _queuedNext = false;
    private int _queuedFromCombo = 0;       // which combo stage created this queue (prevents stacking)
    private float _queueExpireTime = 0f;
    private int _lastQueuedPressFrame = -999999;
    public bool OwnerIsLeftHand => _isLeftHand;

    // --- failsafe: prevent permanent Attacking lock if Dash->Hit1 handoff fails ---
    [SerializeField] private float dashFailSafeSeconds = 1.5f;
    private float _attackStartTime = -999f;

    [SerializeField] private PlayerMovement playerMovement;
    private void Awake()
    {
        if (attackManager == null)
            attackManager = GetComponentInParent<AttackManager>();

        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (playerAnimation == null) playerAnimation = GetComponentInParent<PlayerAnimation>();
        _meleeLayer = (anim != null) ? anim.GetLayerIndex(meleeLayerName) : -1;

        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();
    }

    private void Update()
    {
        if (anim == null || _meleeLayer < 0) return;

        // If not attacking, nothing to manage
        if (!anim.GetBool(paramAttacking))
            return;

        var s = anim.GetCurrentAnimatorStateInfo(_meleeLayer);
        float t = s.normalizedTime % 1f;

        int stage = GetStageFromState(s);

        // stage < 0 才代表「不在 Dash/Hit」，stage==0 是 Dash，不能 early-return
        if (stage < 0)
            return;

        // expire buffer (Dash 期間也要讓它過期，避免卡 queue)
        if (_queuedNext && Time.time > _queueExpireTime)
            ClearQueue();

        // ✅ Dash failsafe: if we're stuck in Dash too long, release Attacking gate
        if (stage == 0)
        {
            if (Time.time - _attackStartTime >= dashFailSafeSeconds)
            {
                EndAttack();
            }
            return; // Dash 期間不做 Hit_1/2/3 消耗窗口邏輯
        }

        // expire buffer
        if (_queuedNext && Time.time > _queueExpireTime)
            ClearQueue();

        // Auto-end rule:
        // If we're in Hit_1 or Hit_2, and we're near the end and there is NO queued input,
        // end the attack so Animator can take Hit_x -> Exit (Attacking=false).
        if ((stage == 1 || stage == 2) && t >= autoEndAt)
        {
            if (!_queuedNext)
            {
                EndAttack();
                return;
            }
        }

        // Consume buffer inside window (only for Hit_1/Hit_2)
        if (_queuedNext && (stage == 1 || stage == 2))
        {
            if (t >= comboWindowOpen && t <= comboConsumeThreshold)
            {
                // Only allow one advance per stage; queue cannot skip stages.
                if (stage == 1)
                {
                    FireTrigger(trigHit2);
                    currentCombo = 2;
                    ClearQueue();
                    return;
                }
                if (stage == 2)
                {
                    FireTrigger(trigHit3);
                    currentCombo = 3;
                    ClearQueue();
                    return;
                }
            }
        }

        // Hit_3 always ends at tail (even if someone spammed)
        if (stage == 3 && t >= autoEndAt)
        {
            EndAttack();
            return;
        }
    }

    /// <summary>
    /// Called by input (PlayerMovement). For first press: open gate + enter Dash.
    /// For subsequent presses during attack: queue next (once per stage, with expiry).
    /// </summary>
    public void MeleeAttack(bool isLeftHand)
    {
        if (anim == null) return;

        bool isAttacking = anim.GetBool(paramAttacking);

        // ✅ combo 期間：另一隻手輸入一律忽略（防止偷換 owner）
        if (isAttacking && isLeftHand != _isLeftHand)
            return;

        // 起手：先鎖定 owner，再 BeginAttack
        if (!isAttacking)
        {
            _isLeftHand = isLeftHand;   // ✅ ONLY set here
            BeginAttack(isLeftHand);
            return;
        }

        // 已在攻擊：只 queue（同手）
        QueueNextOncePerStage();
    }

    /// <summary>
    /// Called by PlayerMovement when melee dash finishes. This MUST fire Hit_1 immediately.
    /// </summary>
    public void FireHit1FromDash(bool isLeftHand)
    {
        if (anim == null) return;

        // 如果已經在攻擊中：另一手不能 hijack
        if (anim.GetBool(paramAttacking) && isLeftHand != _isLeftHand)
            return;

        // ✅ only set owner if we're starting a new chain
        if (!anim.GetBool(paramAttacking))
            _isLeftHand = isLeftHand;

        if (!anim.GetBool(paramAttacking))
            BeginAttack(isLeftHand);

        FireTrigger(trigHit1);
        currentCombo = 1;

        if (_queuedNext && _queuedFromCombo == 0)
            _queuedFromCombo = 1;
    }

    private void BeginAttack(bool isLeftHand)
    {
        // Open gate
        anim.SetBool(paramAttacking, true);
        _attackStartTime = Time.time;
        // hand routing
        anim.SetBool(paramLeftHandAttack, isLeftHand);
        anim.SetBool(paramRightHandAttack, !isLeftHand);

        playerAnimation?.SetToAttackLayer();

        currentCombo = 0;
        ClearQueue();
        // Enter dash state
        ResetTrigger(trigDash);
        FireTrigger(trigDash);

        playerMovement?.LockGravity();
    }

    private void EndAttack()
    {
        bool isLeftHand = _isLeftHand;  // ✅ ending hand = chain owner

        anim.SetBool(paramAttacking, false);

        // reset hand flags (prevents weird re-entry)
        if (_isLeftHand) anim.SetBool(paramLeftHandAttack, false);
        else anim.SetBool(paramRightHandAttack, false);

        playerAnimation?.SetOffAttackLayer();

        currentCombo = 0;
        ClearQueue();

        // ✅ combo chain finished -> start melee reload/cooldown (per hand)
        attackManager?.NotifyMeleeComboFinished(isLeftHand);

        anim.ResetTrigger(trigHit1);
        anim.ResetTrigger(trigHit2);
        anim.ResetTrigger(trigHit3);
        anim.ResetTrigger(trigDash);

        playerMovement?.UnlockGravity();
        meleeAttackWall.enabled = false;
    }

    private void QueueNextOncePerStage()
    {
        // Prevent "hold refresh": only allow one queue action per frame
        if (Time.frameCount == _lastQueuedPressFrame)
            return;

        _lastQueuedPressFrame = Time.frameCount;

        int stage = currentCombo; // 0 during dash, 1/2/3 during hits

        // Can't queue beyond Hit_3
        if (stage >= 3) return;

        // Already queued for this stage => ignore
        if (_queuedNext && _queuedFromCombo == stage)
            return;

        // Set/refresh queue (expiry only when newly queued for this stage)
        _queuedNext = true;
        _queuedFromCombo = stage;          // stage 0 = during dash before hit1; stage 1 = during hit1; stage 2 = during hit2
        _queueExpireTime = Time.time + inputBufferTime;
    }

    private void ClearQueue()
    {
        _queuedNext = false;
        _queuedFromCombo = 0;
        _queueExpireTime = 0f;
    }

    private void FireTrigger(string trig)
    {
        // Clean safety: reset then set to avoid sticky triggers
        anim.ResetTrigger(trig); 
        anim.SetTrigger(trig);
    }
    private void ResetTrigger(string trig)
    {
        anim.ResetTrigger(trig);
    }
    private int GetStageFromState(AnimatorStateInfo s)
    {
        // Dash / Hit_1 / Hit_2 / Hit_3 mapping -> 0..3
        if (s.IsName(stateDash)) return 0; // dash phase
        if (s.IsName(stateHit1)) return 1;
        if (s.IsName(stateHit2)) return 2;
        if (s.IsName(stateHit3)) return 3;
        return -1;
    }
}
