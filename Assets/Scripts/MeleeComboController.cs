using System;
using UnityEngine;

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

    // runtime
    public int currentCombo = 0;            // 0 none, 1/2/3 current stage
    private bool _isLeftHand = true;

    // one-shot buffer per stage
    private bool _queuedNext = false;
    private int _queuedFromCombo = 0;       // which combo stage created this queue (prevents stacking)
    private float _queueExpireTime = 0f;
    private int _lastQueuedPressFrame = -999999;

    private void Awake()
    {
        if (attackManager == null)
            attackManager = GetComponentInParent<AttackManager>();

        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (playerAnimation == null) playerAnimation = GetComponentInParent<PlayerAnimation>();
        _meleeLayer = (anim != null) ? anim.GetLayerIndex(meleeLayerName) : -1;
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
        if (stage <= 0)
            return; // not in Dash/Hit states (maybe transitioning)

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

        _isLeftHand = isLeftHand;

        // Start new chain
        if (!anim.GetBool(paramAttacking))
        {
            BeginAttack(isLeftHand);
            return;
        }

        // Already attacking -> queue next (方案 B)
        QueueNextOncePerStage();
    }

    /// <summary>
    /// Called by PlayerMovement when melee dash finishes. This MUST fire Hit_1 immediately.
    /// </summary>
    public void FireHit1FromDash(bool isLeftHand)
    {
        if (anim == null) return;

        _isLeftHand = isLeftHand;

        // Ensure gate open (safety)
        if (!anim.GetBool(paramAttacking))
            BeginAttack(isLeftHand);

        // Fire Hit_1 immediately
        FireTrigger(trigHit1);
        currentCombo = 1;

        // After Hit_1 starts, queued-from-dash should count as "from combo 1"
        // (so spamming during dash doesn't allow stacking to hit3)
        if (_queuedNext && _queuedFromCombo == 0)
            _queuedFromCombo = 1;
    }

    private void BeginAttack(bool isLeftHand)
    {
        // Open gate
        anim.SetBool(paramAttacking, true);

        // hand routing
        anim.SetBool(paramLeftHandAttack, isLeftHand);
        anim.SetBool(paramRightHandAttack, !isLeftHand);

        playerAnimation?.SetToAttackLayer();

        currentCombo = 0;
        ClearQueue();

        // Enter dash state
        FireTrigger(trigDash);
    }

    private void EndAttack()
    {
        // Close gate so Hit_1/Hit_2/Hit_3 can transition to Exit based on Attacking=false
        anim.SetBool(paramAttacking, false);

        // reset hand flags (prevents weird re-entry)
        anim.SetBool(paramLeftHandAttack, false);
        anim.SetBool(paramRightHandAttack, false);

        //make sure no triggers are left hanging
        anim.ResetTrigger(trigHit1);
        anim.ResetTrigger(trigHit2);
        anim.ResetTrigger(trigHit3);

        playerAnimation?.SetOffAttackLayer();

        currentCombo = 0;
        ClearQueue();

        // ✅ combo chain finished -> start melee reload/cooldown (per hand)
        attackManager?.NotifyMeleeComboFinished(_isLeftHand);
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
