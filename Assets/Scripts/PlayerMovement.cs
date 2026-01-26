using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
public class PlayerMovement : MonoBehaviour
{
    [Header("Character orientation")]
    //[SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform characterOrientation;
    [SerializeField] private Transform characterModel;
    [Header("Rigidbody")]
    [SerializeField] private Rigidbody playerRigidbody;
    [Header("Movement Settings")]
    [SerializeField] private bool grounded;
    [SerializeField] private bool readyToJump;
    [SerializeField] private float jumpCooldown = 0.2f;
    [SerializeField] private bool canRegenerateEnergy;
    [SerializeField] private Vector3 moveDirection = new Vector3(0, 0, 0);
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private Transform groundPoint;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float flyInputBuffer = 0.12f; // 飛行請求緩衝（避免 Update/FixedUpdate 不同步漏掉）
    private float flyRequestUntil = 0f;
    [Header("Dash")]
    [SerializeField] private float dashDuration = 0.1f;     // 主人指定：只持續 0.1s
    [SerializeField] private float dashInputBuffer = 0.12f; // 跟 fly 一樣避免漏吃
    [SerializeField] private float dashCooldown = 0.25f;    // 可自行調
    private float dashRequestUntil = 0f;
    private float dashActiveUntil = 0f;
    private float nextDashTime = 0f;
    private Vector3 dashDir = Vector3.forward;
    private float dashAccel = 0f;        // 固定加速度（m/s^2）
    private float dashTargetSpeed = 0f;  // 目標 dash 速度

    [Header("Melee Dash")]
    [SerializeField] private float meleeDashMaxDuration = 1.5f; // 超過這時間仍未達距離就停（例如撞牆）
    private bool meleeDashActive = false;
    private Vector3 meleeDashDir = Vector3.forward;
    private float meleeDashSpeed = 0f;
    private float meleeDashDistance = 0f;
    private float meleeDashStartTime = 0f;
    private Vector3 meleeDashStartPos = Vector3.zero;
    private Vector3 meleeDashTargetPoint = Vector3.zero;
    private Transform meleeDashTarget = null; // dash 期間追蹤的敵人（可為 null）
    private bool meleeDashChasingTarget = false; // 只有 lockOn 時才追人 / 才用「距離目標<=1」停止

    // 用於 dash 結束後啟動 melee reload
    private AttackManager meleeDashOwnerManager = null;
    private Weapon meleeDashOwnerWeapon = null;
    [SerializeField] private float meleeDashStopWithin = 3f; // 目標距離 <= meleeDashStopWithin 時停止

    private bool meleeDashSavedUseGravity;
    private bool meleeDashHasSavedGravity = false;

    private bool meleeDashReachedStopWithin = false;
    [Header("Attack Facing")]
    [SerializeField] private float attackRotateSpeed = 25;         // 轉身速度
    [SerializeField] private float attackAngleThreshold = 6f;       // 允許開火的角度誤差
    [SerializeField] private float attackAimRayDistance = 100f;     // 非 lockOn 時，用準星 ray 取點距離
    [SerializeField] private float singleShotBufferTime = 0.25f;    // 單發按下後，最多等待多久才消耗掉這次射擊
    [SerializeField] private float maxAlignTime = 0.25f;            // 最多對準多久就強制放行（避免卡死）
    private Weapon attackFacingOwner; // 目前誰在主導 attackFacingActive
    private bool attackFacingActive;
    private Vector3 attackDesiredForward; // XZ 平面朝向

    // key = Weapon instance（AttackManager.leftWeapon/rightWeapon）
    // value = 這次單發請求的失效時間 / 開始對準時間
    private readonly Dictionary<Weapon, float> pendingSingleUntil = new();
    private readonly Dictionary<Weapon, float> alignStartTime = new();
    [Header("Animation")]
    [SerializeField] private PlayerAnimation playerAnimation;
    [SerializeField] private float movementBlendSpeed = 3f; // 控制 0↔1 的快慢
    private float animX = 0f; // 水平（左右）動畫輸入
    private float animY = 0f; // 垂直（前後）動畫輸入
    [Header("Aim Facing Hold (Anti Jitter)")]
    [SerializeField] private float aimHoldAfterLockLost = 2f;
    [SerializeField] private float aimHoldAfterShootNoLock = 2f;
    private bool _prevLockOn;
    private float _aimHoldUntil;
    private Vector3 _lastAimHoldForward = Vector3.forward;

    [Header("Melee Dash End Brake")]
    [Range(0f, 1f)]
    [SerializeField] private float meleeDashEndHorizontalSpeedFactor = 0.2f; // 剩 20%
    [SerializeField] private float meleeDashEndMinHorizontalSpeed = 0.0f;    // 可選：避免太慢卡住

    // --- Melee Dash lead (predict) ---
    private Rigidbody meleeDashTargetRb = null;
    private Vector3 meleeDashLastTargetPos = Vector3.zero;
    private bool meleeDashHasLastTargetPos = false;

    [Header("Melee Combo")]
    [Tooltip("Animator layer name keyword used to locate the melee attack layer. If not found, layer 0 will be used.")]
    [SerializeField] private string meleeAttackLayerNameContains = "Melee";
    [Tooltip("Combo input is only accepted when current attack normalizedTime is within this range.")]
    [Range(0f, 1f)][SerializeField] private float comboInputMinNormalized = 0.25f;
    [Range(0f, 1f)][SerializeField] private float comboInputMaxNormalized = 0.90f;

    // runtime combo state (0 = none, 1 = combo1, 2 = combo2)
    private int _leftCombo = 0;
    private int _rightCombo = 0;
    private bool _leftComboQueued = false;
    private bool _rightComboQueued = false;

    private Animator _anim;
    private int _meleeLayerIndex = 0;
    private static readonly int Anim_Attacking = Animator.StringToHash("attacking");
    private static readonly int Anim_LeftHandCombo = Animator.StringToHash("LeftHandCombo");
    private static readonly int Anim_RightHandCombo = Animator.StringToHash("RightHandCombo");

    [Tooltip("If player presses before the combo window, keep this request for N seconds.")]
    [SerializeField] private float comboInputBufferSeconds = 0.15f;

    private float _leftComboBufferedUntil = 0f;
    private float _rightComboBufferedUntil = 0f;

    // If player presses combo during melee dash (before attack animation starts), queue combo2 after dash ends
    private bool _leftQueueComboAfterDash = false;
    private bool _rightQueueComboAfterDash = false;

    private void Awake()
    {
        // Cache Animator for combo + state checks.
        if (playerAnimation != null)
            _anim = playerAnimation.GetComponent<Animator>();
        if (_anim == null)
            _anim = GetComponentInChildren<Animator>(true);

        // Best-effort to locate the melee attack layer by name.
        _meleeLayerIndex = 0;
        if (_anim != null)
        {
            for (int i = 0; i < _anim.layerCount; i++)
            {
                string ln = _anim.GetLayerName(i);
                if (!string.IsNullOrEmpty(ln) && ln.Contains(meleeAttackLayerNameContains))
                {
                    _meleeLayerIndex = i;
                    break;
                }
            }
        }

        ResetMeleeComboState();
    }

    private void OnEnable()
    {
        if (playerAnimation != null)
            playerAnimation.OnStopAttacking += HandleStopAttacking;
    }

    private void OnDisable()
    {
        if (playerAnimation != null)
            playerAnimation.OnStopAttacking -= HandleStopAttacking;
    }

    private void HandleStopAttacking()
    {
        StopMeleeDash();
        ResetMeleeComboState();
    }
    public void Update()
    {
        EnergyRegenerationCheck();
        RotateCharacter();
        UIManager.Instance.speedText.text = playerRigidbody.linearVelocity.magnitude < 0.0001f ? 0.ToString("F2") : playerRigidbody.linearVelocity.magnitude.ToString("F2");
    }
    public void FixedUpdate()
    {
        GroundCheck();
        ApplyHorizontalMovementFixed(Time.fixedDeltaTime);
        ApplyFlyFixed(Time.fixedDeltaTime); // 新增
        ApplyMeleeDashFixed(Time.fixedDeltaTime); // 新增（近戰衝刺）
        ApplyDashFixed(Time.fixedDeltaTime); // 新增
    }
    private void ApplyHorizontalMovementFixed(float dt)
    {
        if (meleeDashActive) return; // 近戰衝刺期間忽略所有移動輸入
        if (Time.time <= dashActiveUntil) return;
        Vector3 v = playerRigidbody.linearVelocity;
        Vector3 horizontalVel = new Vector3(v.x, 0f, v.z);

        // 沒輸入：用 deceleration 拉回 0（只處理 x/z，保留 y）
        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            horizontalVel = Vector3.MoveTowards(horizontalVel, Vector3.zero, PlayerStats.Instance.decelerationSpeed * dt);
            playerRigidbody.linearVelocity = new Vector3(horizontalVel.x, v.y, horizontalVel.z);
            return;
        }

        // 有輸入：用 acceleration 推向目標水平速度（同樣只處理 x/z）
        Vector3 targetHorizontalVel = moveDirection * PlayerStats.Instance.sprintSpeed;
        horizontalVel = Vector3.MoveTowards(horizontalVel, targetHorizontalVel, PlayerStats.Instance.accelerationSpeed * dt);

        playerRigidbody.linearVelocity = new Vector3(horizontalVel.x, v.y, horizontalVel.z);
    }

    private void ApplyFlyFixed(float dt)
    {
        // 落地就清掉飛行請求
        if (grounded)
        {
            flyRequestUntil = 0f;
            return;
        }

        // 沒有飛行請求就不干預 Y 速度（讓重力自然下落）
        if (Time.time > flyRequestUntil) return;

        var stats = PlayerStats.Instance;
        if (stats == null) return;

        if (stats.currentEnergy <= 0f)
        {
            flyRequestUntil = 0f;
            return;
        }

        Vector3 v = playerRigidbody.linearVelocity;

        // 這裡把 flyForce 當成「目標上升速度（m/s）」來用
        float targetVy = stats.flySpeed;

        // 用 accelerationSpeed 當作「飛行加速度（m/s^2）」讓 vy 逼近 targetVy
        if (stats.flySpeed != 0)
        {
            float newVy = Mathf.MoveTowards(v.y, targetVy, stats.flyAcceleration * dt);

            playerRigidbody.linearVelocity = new Vector3(v.x, newVy, v.z);
        }
        // 能量消耗用 fixed dt（不飄幀率）
        stats.currentEnergy = Mathf.Max(0f, stats.currentEnergy - dt * stats.flyEnergyCost);

        if (stats.currentEnergy <= 0f)
            flyRequestUntil = 0f;
    }
    public void ProcessAttackFacingAndAttack(AttackManager attackManager, Weapon w, InputAction attackInput)
    {
        if (attackManager == null || w == null || attackInput == null) return;
        if (PlayerAiming.Instance == null || characterModel == null) return;

        // 判斷這次攻擊是遠程還是近戰（不改 PlayerController 的呼叫方式）
        var stats = PlayerStats.Instance;
        bool isLeft = (attackManager != null && w == attackManager.leftHandWeapon);
        bool isRight = (attackManager != null && w == attackManager.rightHandWeapon);
        bool isLeftShoulderAttack = (attackManager != null && w == attackManager.leftShoulderWeapon);
        bool isRightShoulderAttack = (attackManager != null && w == attackManager.rightShoulderWeapon);
        bool isMelee = false;

        // 近戰衝刺距離目前統一使用 PlayerStats.meleeDashDistance
        float meleeDashDistance = (stats != null) ? stats.meleeDashDistance : 0f;

        if (stats != null && (isLeft || isRight))
        {
            var hand = isLeft ? stats.leftHand : stats.rightHand;
            isMelee = (hand.weaponKind == HandWeaponKind.Melee);
        }

        // 近戰目前先視為「單發」：按一下觸發一次（之後要連段再擴充）
        bool isSingle = isMelee ? true : (w.range.firingMode == 0);

        // ===== Melee dash combo buffer (pressing too fast during dash) =====
        // If player presses attack while melee dash is still active (attack animation not started yet),
        // do NOT restart the dash. Instead, remember to queue combo2 after dash ends.
        if (isMelee && meleeDashActive && attackInput.WasPressedThisFrame())
        {
            if (isLeft && _leftCombo == 1) _leftQueueComboAfterDash = true;
            if (isRight && _rightCombo == 1) _rightQueueComboAfterDash = true;
            return;
        }

        // ===== Melee combo input (ONLY while already attacking) =====
        if (isMelee && attackInput.WasPressedThisFrame() && IsAttackingAnim())
        {
            // 先嘗試：如果已在窗口內，立刻 queue combo2
            if (TryQueueMeleeCombo(attackManager, w))
                return;

            // 否則：記一次 buffer，等窗口到自動接
            BufferMeleeComboInput(attackManager, w);
            return; // 只阻止「攻擊中」的第二下開新 Dash
        }
        // 1) 收集「想射擊」意圖（單發需要緩存，否則轉完身就不是 this frame 了）
        if (isSingle)
        {
            if (attackInput.WasPressedThisFrame())
            {
                pendingSingleUntil[w] = Time.time + singleShotBufferTime;
                alignStartTime[w] = Time.time;
                if (isLeftShoulderAttack)
                {
                    playerAnimation.ShoulderWeaponAttackLeft();
                }
                else if (isRightShoulderAttack)
                {
                    playerAnimation.ShoulderWeaponAttackRight();
                }
            }

            if (!pendingSingleUntil.TryGetValue(w, out float until) || Time.time > until)
            {
                pendingSingleUntil.Remove(w);
                alignStartTime.Remove(w);
                ClearAttackFacingOwnerIfSelf(w);
                return;
            }
        }
        else
        {
            // 連發/蓄力：只要按住就持續嘗試
            if (!attackInput.IsPressed())
            {
                alignStartTime.Remove(w);
                ClearAttackFacingOwnerIfSelf(w);
                if (isLeftShoulderAttack)
                {
                    playerAnimation.ShoulderWeaponAttackLeft();
                }
                else if (isRightShoulderAttack)
                {
                    playerAnimation.ShoulderWeaponAttackRight();
                }
                return;
            }

            if (!alignStartTime.ContainsKey(w))
                alignStartTime[w] = Time.time;
        }

        // 2) 算出這一幀「應該面向哪裡」（lockOn -> aimingPoint；否則 -> 準星 ray）
        Vector3 targetPoint;
        if (PlayerAiming.Instance.lockOn && PlayerAiming.Instance.aimingPoint != null)
            targetPoint = PlayerAiming.Instance.aimingPoint.position;
        else
            targetPoint = PlayerAiming.Instance.GetRay().GetPoint(attackAimRayDistance);

        Vector3 lookDir = targetPoint - characterModel.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude < 0.0001f) return;

        SetAttackFacingOwner(w, lookDir.normalized);

        // 3) 判斷是否已對準（或超時就放行）
        Vector3 flatForward = characterModel.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.0001f) flatForward = attackDesiredForward;
        flatForward.Normalize();

        float angle = Vector3.Angle(flatForward, attackDesiredForward);
        bool aligned = angle <= attackAngleThreshold;

        bool timedOut = alignStartTime.TryGetValue(w, out float startT) && (Time.time - startT) >= maxAlignTime;

        if (aligned || timedOut)
        {
            if (isMelee)
            {
                // Step 2（先做最簡單）：對準後衝刺到目標方向
                bool didMelee = attackManager.TryStartMeleeAttack(w);
                if (didMelee)
                {
                    StartMeleeDash(attackManager, w, targetPoint, stats != null ? stats.dashSpeed : 0f, meleeDashDistance);

                    if (isSingle)
                    {

                        pendingSingleUntil.Remove(w);
                        alignStartTime.Remove(w);
                    }
                }
            }
            else
            {
                // 新增：未 lockOn 時，射擊後保留準星朝向一段時間（允許邊走邊朝準星）
                if ((PlayerAiming.Instance != null) && !PlayerAiming.Instance.lockOn)
                {
                    // 記一份 fallback，避免極端情況 ray/dir 失效時抖動
                    if (TryGetAimForwardFlat(out var aimFwd))
                        _lastAimHoldForward = aimFwd;

                    StartAimHold(aimHoldAfterShootNoLock);
                }

                if (isLeftShoulderAttack)
                {
                    if (playerAnimation != null && !playerAnimation.IsShoulderWeaponReadyToFire(true, 0.1f))
                        return; // 還未完成 Idle->Attack 轉場，先不要射（保留 pendingSingleUntil，下一幀再試）
                }
                else if (isRightShoulderAttack)
                {
                    if (playerAnimation != null && !playerAnimation.IsShoulderWeaponReadyToFire(false, 0.1f))
                        return;
                }

                // 4) 真正觸發射擊（由 AttackManager 管理 cooldown/bullets）
                bool didShoot = attackManager.TryStartShoot(w);
                // 單發：一旦成功開火就消耗掉這次請求
                if (didShoot && isSingle)
                {
                    pendingSingleUntil.Remove(w);
                    alignStartTime.Remove(w);
                }
            }
        }

    }
    private void SetAttackFacingOwner(Weapon w, Vector3 desiredForward)
    {
        attackFacingOwner = w;
        attackFacingActive = true;
        attackDesiredForward = desiredForward;
    }
    private void ClearAttackFacingOwnerIfSelf(Weapon w)
    {
        if (attackFacingOwner == w)
        {
            attackFacingOwner = null;
            attackFacingActive = false;
        }
    }
    public void HorizontalMovement(float moveX, float moveZ)
    {
        // 衝刺期間忽略所有移動輸入（包含朝向/動畫用的 moveDirection）
        if (meleeDashActive || Time.time <= dashActiveUntil) return;

        // 只更新 moveDirection（給 RotateCharacter / 動畫用），不要在 Update 內碰剛體
        Vector3 forward = characterOrientation.forward;
        forward.y = 0f;

        Vector3 right = characterOrientation.right;
        right.y = 0f;

        // 避免極端情況（方向向量太小）造成 NaN
        if (forward.sqrMagnitude > 0.0001f) forward.Normalize();
        if (right.sqrMagnitude > 0.0001f) right.Normalize();

        moveDirection = forward * moveZ + right * moveX;

        if (moveDirection.sqrMagnitude < 0.0001f)
            moveDirection = Vector3.zero;
        else
            moveDirection.Normalize();
    }
    public bool JumpAction()
    {
        if (grounded && readyToJump)
        {
            readyToJump = false;
            playerRigidbody.linearVelocity = new Vector3(playerRigidbody.linearVelocity.x, 0, playerRigidbody.linearVelocity.z);
            playerRigidbody.AddForce(Vector3.up * convertJumpHeightToForce(PlayerStats.Instance.jumpHeight), ForceMode.Impulse);
            Invoke("ResetJump", jumpCooldown);
            return true;
        }
        else if (!grounded && readyToJump && PlayerStats.Instance.currentEnergy > 0)
        {
            FlyAction();
        }
        return false;
    }
    private void ApplyMeleeDashFixed(float dt)
    {
        if (!meleeDashActive) return;

        // 1) 超時保護：撞牆或卡住，最多持續 meleeDashMaxDuration

        // 2) 只有 lockOn 追人時才：
        //    - 距離目標 <= meleeDashStopWithin 就停
        //    - 每個 FixedUpdate 重新校正方向去追目標
        if (meleeDashChasingTarget && meleeDashStopWithin > 0f)
        {
            Debug.Log("meleeDashStopWithin");
            Vector3 targetPos = (meleeDashTarget != null) ? meleeDashTarget.position : meleeDashTargetPoint;
            float distToTarget = Vector3.Distance(transform.position, targetPos);
            Debug.Log("distToTarget" + distToTarget);
            Debug.Log("meleeDashStopWithin" + meleeDashStopWithin);

            if (distToTarget <= meleeDashStopWithin)
            {
                if (!meleeDashReachedStopWithin)
                {

                    meleeDashReachedStopWithin = true;
                    Debug.Log(" 距離目標 <= meleeDashStopWithin");
                    StopMeleeDash();
                    playerAnimation.InvokeStartAttack(0.1f);
                    playerAnimation.SmoothSetFOV();
                    ApplyQueuedComboAfterDash();
                }
                return;
            }

            // 追人：每個 FixedUpdate 重新校正方向
            if (meleeDashTarget != null)
            {
                targetPos = meleeDashTarget.position;

                // 估目標速度：優先 Rigidbody；冇就用位置差分估速（可對付 NavMeshAgent 類）
                Vector3 targetVel = Vector3.zero;
                if (meleeDashTargetRb != null)
                {
                    targetVel = meleeDashTargetRb.linearVelocity;
                }
                else
                {
                    if (meleeDashHasLastTargetPos && dt > 0f)
                        targetVel = (targetPos - meleeDashLastTargetPos) / dt;

                    meleeDashLastTargetPos = targetPos;
                    meleeDashHasLastTargetPos = true;
                }

                // 算攔截點
                if (Math.InterceptionPoint(targetPos, transform.position, targetVel, meleeDashSpeed, out var leadPoint))
                {
                    //leadPoint.y = transform.position.y; // 平面追擊
                    Vector3 toLead = leadPoint - transform.position;
                    if (toLead.sqrMagnitude > 0.0001f) meleeDashDir = toLead.normalized;
                }
                else
                {
                    Vector3 toTarget = targetPos - transform.position;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.0001f) meleeDashDir = toTarget.normalized;
                }
            }
        }

        // 2) 距離判定（真 3D 距離：包含垂直）
        Vector3 delta = transform.position - meleeDashStartPos;
        if (meleeDashDistance > 0f && delta.magnitude >= meleeDashDistance)
        {
            Debug.Log(" delta.magnitude >= meleeDashDistance");
            StopMeleeDash();
            playerAnimation.StartAttack();
            playerAnimation.SmoothSetFOV();
            ApplyQueuedComboAfterDash();
            return;
        }
        if (Time.time - meleeDashStartTime > meleeDashMaxDuration)
        {
            Debug.Log(" 超時保護");
            StopMeleeDash();
            playerAnimation.StartAttack();
            playerAnimation.SmoothSetFOV();
            ApplyQueuedComboAfterDash();
            return;
        }
        // 3) 施加衝刺速度（真 3D：X/Y/Z 全吃方向）
        Vector3 dashVel = meleeDashDir * meleeDashSpeed;
        playerRigidbody.linearVelocity = dashVel;
    }

    private void StartMeleeDash(AttackManager attackManager, Weapon ownerWeapon, Vector3 targetPoint, float dashSpeed, float dashDistance)
    {
        // Prevent re-entering melee dash (spamming attack would otherwise extend dash indefinitely)
        if (meleeDashActive)
            return;

        // Start of a new melee sequence for this hand.
        _leftComboBufferedUntil = 0f;
        _rightComboBufferedUntil = 0f;
        _leftQueueComboAfterDash = false;
        _rightQueueComboAfterDash = false;

        if (_anim != null)
        {
            bool isLeft = (attackManager != null && ownerWeapon == attackManager.leftHandWeapon);
            bool isRight = (attackManager != null && ownerWeapon == attackManager.rightHandWeapon);

            if (isLeft)
            {
                _leftCombo = 1;
                _leftComboQueued = false;
                _anim.SetInteger(Anim_LeftHandCombo, 1);
            }
            else if (isRight)
            {
                _rightCombo = 1;
                _rightComboQueued = false;
                _anim.SetInteger(Anim_RightHandCombo, 1);
            }
        }
        if (playerRigidbody == null) return;
        if (dashSpeed <= 0f || dashDistance <= 0f) return;

        // 已在衝刺中就不重新開（避免連點造成狀態抖動）
        if (meleeDashActive) return;

        // 只有 lockOn 時才追敵人；未 lockOn 就直接往前衝（不要朝 ray/targetPoint）
        bool lockOn = (PlayerAiming.Instance != null && PlayerAiming.Instance.lockOn);

        Transform targetTf = null;
        bool chasing = false;
        meleeDashTargetRb = null;
        meleeDashHasLastTargetPos = false;

        if (lockOn)
        {
            var targetRb = PlayerAiming.Instance.GetTargetRigidbody();
            if (targetRb != null)
            {
                meleeDashTargetRb = targetRb;
                targetTf = targetRb.transform;
                chasing = true;
            }
        }

        Vector3 dir;
        if (chasing && targetTf != null)
        {

            // 取目標速度：有 Rigidbody 就用佢；冇就先當 0（之後 FixedUpdate 會用位移估速）
            Vector3 targetVel = (meleeDashTargetRb != null) ? meleeDashTargetRb.linearVelocity : Vector3.zero;

            // 用攔截點做 lead（Math.cs 已有）
            if (Math.InterceptionPoint(targetTf.position, transform.position, targetVel, dashSpeed, out var leadPoint))
            {
                // 近戰 dash 通常唔想飛天：鎖死 y，保持平面追擊
                //leadPoint.y = transform.position.y;
                dir = (leadPoint - transform.position);
            }
            else
            {
                dir = (targetTf.position - transform.position);
                dir.y = 0f;
            }
        }
        else
        {
            // 你原本未 lockOn 邏輯照舊
            dir = (targetPoint - transform.position);
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f)
            {
                dir = (characterModel != null ? characterModel.forward : transform.forward);
                dir.y = 0f;
            }
        }

        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        // 立刻讓模型對齊 dash 方向（避免第一幀視覺錯位）
        if (characterModel != null)
            characterModel.forward = dir;

        // 同步給 RotateCharacter 的攻擊面向（確保 dash 期間不被 moveDirection 蓋掉）
        attackFacingActive = true;
        attackDesiredForward = dir;
        attackFacingOwner = ownerWeapon;


        // 記住本次 dash 的來源（用於結束後啟動 melee reload）
        meleeDashOwnerManager = attackManager;
        meleeDashOwnerWeapon = ownerWeapon;

        // 到這裡才真正開始 dash：先關重力
        if (!meleeDashHasSavedGravity)
        {
            meleeDashSavedUseGravity = playerRigidbody.useGravity;
            meleeDashHasSavedGravity = true;
        }
        playerRigidbody.useGravity = false;

        meleeDashTargetPoint = targetPoint;
        meleeDashTarget = targetTf;
        meleeDashChasingTarget = chasing;
        meleeDashActive = true;
        meleeDashDir = dir;
        meleeDashSpeed = Mathf.Max(0f, dashSpeed);
        meleeDashDistance = Mathf.Max(0f, dashDistance);
        meleeDashStartTime = Time.time;
        meleeDashStartPos = transform.position;
        // 近戰 dash 期間：把鏡頭拉去鎖定目標，讓目標在畫面中央
        if (meleeDashChasingTarget && meleeDashTarget != null && PlayerAiming.Instance != null)
        {
            //PlayerAiming.Instance.BeginMeleeDashCameraFocus(meleeDashTarget);
        }

        // meleeDashTargetPoint/meleeDashTarget/meleeDashChasingTarget 已在上面設定

        playerAnimation.SetToAttackLayer();
        // 近戰衝刺期間不回能（與一般 dash 同步）
        CancelInvoke("ResetEnergyRegenerate");
        canRegenerateEnergy = false;
        bool isLeftHand = (attackManager != null && ownerWeapon != null && attackManager.leftHandWeapon == ownerWeapon);

        // For now: stance is decided only by the melee weapon's attribute (ignore attachments/handles)
        MeleeWeaponPartAttribute attr = default;
        var stats = PlayerStats.Instance;
        if (stats != null)
        {
            var hand = isLeftHand ? stats.leftHand : stats.rightHand;
            if (hand != null && hand.meleeWeapon != null && hand.meleeWeapon.item is MeleeWeapon mw)
                attr = mw.attribute;
        }
        playerAnimation.BeginMeleeDash(isLeftHand, attr);

        Vector3 targetPos = (meleeDashTarget != null) ? meleeDashTarget.position : meleeDashTargetPoint;
        float distToTarget = Vector3.Distance(transform.position, targetPos);
        if (distToTarget >= meleeDashStopWithin * 2)
        {
            playerAnimation.ChangeFOVtoAttack();
        }
        _leftComboBufferedUntil = 0f;
        _rightComboBufferedUntil = 0f;
    }

    private void StopMeleeDash()
    {
        if (!meleeDashActive) return;

        // ✅ 立刻剎停水平慣性（保留 Y）
        if (playerRigidbody != null)
        {
            Vector3 v = playerRigidbody.linearVelocity;

            Vector3 horizontal = new Vector3(v.x, 0f, v.z);
            float y = v.y;

            horizontal *= meleeDashEndHorizontalSpeedFactor;

            // 可選：做個下限，避免變到幾乎 0 時感覺「黏地」
            if (meleeDashEndMinHorizontalSpeed > 0f)
            {
                float mag = horizontal.magnitude;
                if (mag > 0f && mag < meleeDashEndMinHorizontalSpeed)
                    horizontal = horizontal.normalized * meleeDashEndMinHorizontalSpeed;
            }

            playerRigidbody.linearVelocity = new Vector3(horizontal.x, y, horizontal.z);
        }

        // 結束近戰 dash：把相機控制權還給滑鼠
        if (PlayerAiming.Instance != null)
        {
            //PlayerAiming.Instance.EndMeleeDashCameraFocus();
        }

        if (meleeDashHasSavedGravity)
        {
            playerRigidbody.useGravity = meleeDashSavedUseGravity;
            meleeDashHasSavedGravity = false;
        }

        meleeDashActive = false;

        if (meleeDashOwnerManager != null && meleeDashOwnerWeapon != null)
        {
            meleeDashOwnerManager.StartMeleeReload(meleeDashOwnerWeapon);
        }

        playerAnimation.StopDashing();
        meleeDashOwnerManager = null;
        meleeDashOwnerWeapon = null;

        canRegenerateEnergy = true;
        meleeDashReachedStopWithin = false;
        attackFacingOwner = null;
        attackFacingActive = false;

        meleeDashTargetRb = null;
        meleeDashHasLastTargetPos = false;
    }


    private void ApplyDashFixed(float dt)
    {
        if (Time.time > dashRequestUntil) return;

        if (Time.time > dashActiveUntil)
        {
            dashRequestUntil = 0f;
            return;
        }

        Vector3 v = playerRigidbody.linearVelocity;
        Vector3 horizontalVel = new Vector3(v.x, 0f, v.z);

        float vParallel = Vector3.Dot(horizontalVel, dashDir);

        // 達標就不要再推，避免疊加
        if (vParallel >= dashTargetSpeed) return;

        // 固定加速度（不受質量影響）
        playerRigidbody.AddForce(dashDir * dashAccel, ForceMode.Acceleration);
    }
    public bool DashAction()
    {
        var stats = PlayerStats.Instance;
        if (stats == null) return false;

        if (Time.time < nextDashTime) return false;

        // 修正：能量不足就不啟動
        if (stats.currentEnergy <= 0) return false;

        Vector3 dir;
        if (moveDirection.sqrMagnitude > 0.001f) dir = moveDirection;
        else dir = characterModel != null ? characterModel.forward : transform.forward;

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return false;
        dir.Normalize();

        // 先算 dash 的固定加速度（只看水平）
        Vector3 v = playerRigidbody.linearVelocity;
        Vector3 horizontalVel = new Vector3(v.x, 0f, v.z);

        dashDir = dir;
        dashTargetSpeed = stats.dashSpeed;

        float vParallel = Vector3.Dot(horizontalVel, dashDir);
        float deltaVStart = Mathf.Max(0f, dashTargetSpeed - vParallel);

        // 固定加速度：整段 dashDuration 都用這個值
        dashAccel = deltaVStart / dashDuration;

        // 扣能量
        stats.currentEnergy -= stats.dashEnergyCost;

        dashRequestUntil = Time.time + dashInputBuffer;
        dashActiveUntil = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;

        CancelInvoke("ResetEnergyRegenerate");
        canRegenerateEnergy = false;

        return true;
    }

    public void FlyAction()
    {
        // 空中且有能量才允許提出飛行請求
        if (grounded) return;
        if (PlayerStats.Instance.currentEnergy <= 0f) return;
        if (PlayerStats.Instance.flyEnergyCost > 0)
        {
            CancelInvoke("ResetEnergyRegenerate");
            canRegenerateEnergy = false;
        }
        // 把「飛行輸入」延長一小段時間，確保 FixedUpdate 一定能吃到
        flyRequestUntil = Time.time + flyInputBuffer;
    }

    private void ResetEnergyRegenerate()
    {
        canRegenerateEnergy = true;
    }

    public float convertJumpHeightToForce(float jumpHeight)
    {
        float g = Mathf.Abs(Physics.gravity.y);
        float v0 = Mathf.Sqrt(2f * g * jumpHeight);
        float mass = playerRigidbody.mass;
        float impulseMagnitude = mass * v0;

        return impulseMagnitude;
    }

    public void EnergyRegenerationCheck()
    {
        if (PlayerStats.Instance.currentEnergy > PlayerStats.Instance.maxEnergy)
        {
            PlayerStats.Instance.currentEnergy = PlayerStats.Instance.maxEnergy;
            canRegenerateEnergy = false;
        }
        else if (PlayerStats.Instance.currentEnergy < PlayerStats.Instance.maxEnergy && canRegenerateEnergy)
        {
            PlayerStats.Instance.currentEnergy = PlayerStats.Instance.currentEnergy + (Time.deltaTime * PlayerStats.Instance.energyRegen);
        }
        else if (PlayerStats.Instance.currentEnergy < PlayerStats.Instance.maxEnergy && !canRegenerateEnergy && PlayerStats.Instance.currentEnergy <= 0)
        {
            Invoke("ResetEnergyRegenerate", 5);
        }
        else if (PlayerStats.Instance.currentEnergy < PlayerStats.Instance.maxEnergy && !canRegenerateEnergy)
        {
            Invoke("ResetEnergyRegenerate", 1);
        }
        else if (PlayerStats.Instance.currentEnergy < 0)
        {
            PlayerStats.Instance.currentEnergy = 0;
        }
    }
    public void GroundCheck()
    {
        // ————————————————————
        // 1) dynamic ground check
        // ————————————————————
        float castDistance = groundCheckDistance + playerRigidbody.linearVelocity.y * Time.fixedDeltaTime;
        RaycastHit hit;
        bool didHit = Physics.Raycast(groundPoint.position,
            Vector3.down,
            out hit,
            castDistance,
            whatIsGround
        );
        grounded = didHit;
        playerAnimation.SetIsOnGround(grounded);
    }
    private void ResetJump()
    {
        readyToJump = true;
    }
    private void RotateCharacter()
    {
        // 監控 lockOn 狀態變化：剛失去 lockOn 時啟動保留
        bool lockOnNow = (PlayerAiming.Instance != null && PlayerAiming.Instance.lockOn);
        if (_prevLockOn && !lockOnNow)
        {
            StartAimHold(aimHoldAfterLockLost);
        }
        _prevLockOn = lockOnNow;

        // 先處理角色朝向（攻擊優先，其次 lockOn，其次移動）
        Vector3? desiredForward = null;

        if (meleeDashActive)
        {
            Vector3 fwd = meleeDashDir;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f)
                desiredForward = fwd.normalized;
        }
        else if (attackFacingActive)
        {
            desiredForward = attackDesiredForward;
        }
        else if (PlayerAiming.Instance.lockOn)
        {
            Vector3 lookDirection = PlayerAiming.Instance.aimingPoint.position - characterModel.position;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.001f)
                desiredForward = lookDirection.normalized;
        }
        // 新增：剛失去鎖定 or 剛單發射擊後，短時間內仍面向「準星」方向（允許蟹行）
        else if (Time.time < _aimHoldUntil)
        {
            if (TryGetAimForwardFlat(out var aimFwd))
            {
                desiredForward = aimFwd;
                _lastAimHoldForward = aimFwd; // 記住一份 fallback
            }
            else if (_lastAimHoldForward.sqrMagnitude > 0.001f)
            {
                desiredForward = _lastAimHoldForward;
            }
        }
        else
        {
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Vector3 faceDirection = moveDirection.normalized;
                desiredForward = new Vector3(faceDirection.x, 0, faceDirection.z).normalized;
            }
        }

        if (desiredForward.HasValue)
        {
            characterModel.forward = Vector3.Slerp(
                characterModel.forward,
                desiredForward.Value,
                Time.deltaTime * attackRotateSpeed
            );
        }
        // 把世界空間的 moveDirection 轉成「角色本地」方向
        Vector3 localMove = Vector3.zero;
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            // InverseTransformDirection：世界 → 角色本地
            localMove = characterModel.InverseTransformDirection(moveDirection);
            localMove.y = 0f;
            localMove.Normalize();
        }

        float targetX = localMove.x; // 左右
        float targetY = localMove.z; // 前後

        // 根據輸入大小決定目標長度（0~1），這樣可以有快走/慢走的過渡（如果主人之後要加）
        float targetMagnitude = Mathf.Clamp01(new Vector2(targetX, targetY).magnitude);
        if (targetMagnitude < 0.001f)
        {
            targetX = 0f;
            targetY = 0f;
        }

        // 平滑左右 / 前後輸入
        animX = Mathf.MoveTowards(animX, targetX, movementBlendSpeed * Time.deltaTime);
        animY = Mathf.MoveTowards(animY, targetY, movementBlendSpeed * Time.deltaTime);

        // 統一送進動畫：animX = 左右，animY = 前後
        playerAnimation.SetMovementParameters(animX, animY);
    }
    private void StartAimHold(float seconds)
    {
        if (seconds <= 0f) return;
        _aimHoldUntil = Mathf.Max(_aimHoldUntil, Time.time + seconds);
    }

    // 取得「準星/鎖定」在水平面的目標朝向（跟你射擊對準算法一致）
    private bool TryGetAimForwardFlat(out Vector3 flatForward)
    {
        flatForward = Vector3.zero;

        if (PlayerAiming.Instance == null || characterModel == null) return false;

        Vector3 targetPoint;

        if (PlayerAiming.Instance.lockOn && PlayerAiming.Instance.aimingPoint != null)
            targetPoint = PlayerAiming.Instance.aimingPoint.position;
        else
            targetPoint = PlayerAiming.Instance.GetRay().GetPoint(attackAimRayDistance);

        Vector3 dir = targetPoint - characterModel.position;
        dir.y = 0f;

        if (dir.sqrMagnitude <= 0.001f) return false;

        flatForward = dir.normalized;
        return true;
    }
    // ---- Thruster VFX state (read-only) ----
    public bool IsGrounded => grounded;

    // 你原本的飛行判定就是靠 flyRequestUntil buffer，這裡沿用同一條準則
    public bool IsFlyingActive => !grounded && Time.time <= flyRequestUntil;

    public float VerticalVelocity
    {
        get
        {
            if (playerRigidbody == null) return 0f;
            return playerRigidbody.linearVelocity.y;
        }
    }

    public bool IsDashActive => Time.time <= dashActiveUntil;

    public bool IsMeleeDashActive => meleeDashActive;


    private void ResolveMeleeAttackLayerIndex()
    {
        _meleeLayerIndex = 0;
        if (_anim == null) return;

        int count = _anim.layerCount;
        if (count <= 0) return;

        for (int i = 0; i < count; i++)
        {
            string ln = _anim.GetLayerName(i);
            if (!string.IsNullOrEmpty(ln) && ln.IndexOf(meleeAttackLayerNameContains, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                _meleeLayerIndex = i;
                return;
            }
        }
    }

    private bool IsAttackingAnim()
    {
        return _anim != null && _anim.GetBool(Anim_Attacking);
    }

    private bool TryGetMeleeAttackNormalizedTime(out float normalized)
    {
        normalized = 0f;
        if (_anim == null) return false;

        if (_meleeLayerIndex < 0 || _meleeLayerIndex >= _anim.layerCount)
            ResolveMeleeAttackLayerIndex();

        var info = _anim.GetCurrentAnimatorStateInfo(_meleeLayerIndex);
        normalized = info.normalizedTime - Mathf.Floor(info.normalizedTime);

        // Heuristic: only allow combo input when current clip looks like melee attack
        var clips = _anim.GetCurrentAnimatorClipInfo(_meleeLayerIndex);
        if (clips == null || clips.Length == 0) return false;
        var clip = clips[0].clip;
        if (clip == null) return false;

        string n = clip.name;
        if (string.IsNullOrEmpty(n)) return false;

        bool looksLikeAttack = n.IndexOf("Attack", System.StringComparison.OrdinalIgnoreCase) >= 0
                            || n.IndexOf("Piercing", System.StringComparison.OrdinalIgnoreCase) >= 0
                            || n.IndexOf("Slashing", System.StringComparison.OrdinalIgnoreCase) >= 0;
        return looksLikeAttack;
    }

    private bool TryQueueMeleeCombo(AttackManager attackManager, Weapon w)
    {
        // Only queue while already attacking (animation-wise).
        if (!IsAttackingAnim()) return false;
        if (_anim == null) return false;

        if (!TryGetMeleeAttackNormalizedTime(out float nt)) return false;
        bool inWindow = (nt >= comboInputMinNormalized && nt <= comboInputMaxNormalized);

        bool buffered = false;
        bool isLeft = (attackManager != null && w == attackManager.leftHandWeapon);
        bool isRight = (attackManager != null && w == attackManager.rightHandWeapon);

        if (isLeft) buffered = (Time.time <= _leftComboBufferedUntil);
        else if (isRight) buffered = (Time.time <= _rightComboBufferedUntil);

        if (!isLeft && !isRight) return false;

        // 既不在窗口內、又沒有有效 buffer → 不接段
        if (!inWindow && !buffered) return false;

        if (isLeft)
        {
            if (_leftCombo <= 0) return false;
            if (_leftCombo >= 2) return false;
            if (_leftComboQueued) return true;

            _leftComboQueued = true;
            _leftCombo = 2;
            _anim.SetInteger(Anim_LeftHandCombo, 2);
            _leftComboBufferedUntil = 0f;
            return true;
        }

        if (_rightCombo <= 0) return false;
        if (_rightCombo >= 2) return false;
        if (_rightComboQueued) return true;

        _rightComboQueued = true;
        _rightCombo = 2;
        _anim.SetInteger(Anim_RightHandCombo, 2);
        _rightComboBufferedUntil = 0f;
        return true;
    }

    private void ResetMeleeComboState()
    {
        _leftCombo = 0;
        _rightCombo = 0;
        _leftComboQueued = false;
        _rightComboQueued = false;

        if (_anim != null)
        {
            _anim.SetInteger(Anim_LeftHandCombo, 0);
            _anim.SetInteger(Anim_RightHandCombo, 0);
        }
        _leftComboBufferedUntil = 0f;
        _rightComboBufferedUntil = 0f;
    }
    private void BufferMeleeComboInput(AttackManager attackManager, Weapon w)
    {
        bool isLeft = (attackManager != null && w == attackManager.leftHandWeapon);
        bool isRight = (attackManager != null && w == attackManager.rightHandWeapon);
        if (!isLeft && !isRight) return;

        // 只在正在攻擊中、且目前 combo=1 時才允許 buffer（避免亂記）
        if (!IsAttackingAnim()) return;

        if (isLeft)
        {
            if (_leftCombo != 1) return;
            if (_leftComboQueued) return;
            _leftComboBufferedUntil = Time.time + comboInputBufferSeconds;
        }
        else
        {
            if (_rightCombo != 1) return;
            if (_rightComboQueued) return;
            _rightComboBufferedUntil = Time.time + comboInputBufferSeconds;
        }
    }


    // If player pressed the next combo during melee dash (before attack animation starts),
    // apply it now so Combo1 -> Combo2 transition can happen normally.
    private void ApplyQueuedComboAfterDash()
    {
        if (_anim == null) return;

        if (_leftQueueComboAfterDash)
        {
            _leftQueueComboAfterDash = false;
            _leftComboQueued = true;
            _leftCombo = 2;
            _anim.SetInteger(Anim_LeftHandCombo, 2);
            _leftComboBufferedUntil = 0f;
        }

        if (_rightQueueComboAfterDash)
        {
            _rightQueueComboAfterDash = false;
            _rightComboQueued = true;
            _rightCombo = 2;
            _anim.SetInteger(Anim_RightHandCombo, 2);
            _rightComboBufferedUntil = 0f;
        }
    }
}
