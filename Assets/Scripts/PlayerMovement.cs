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
    [Header("Melee Attack")]
    public MeleeComboController meleeComboController;
    [Header("Melee Dash")]
    [Tooltip("When lock-on: stop dash early if within this distance to aimingPoint.")]
    [SerializeField] private float meleeDashStopDistance = 1.5f;
    [Tooltip("Extra time added on top of (distance / dashSpeed) to tolerate tiny stalls.")]
    [SerializeField] private float meleeDashTimeExtra = 0.08f;
    [Tooltip("Hard clamp for melee dash timeout.")]
    [SerializeField] private float meleeDashMaxTimeClamp = 0.6f;
    [Tooltip("If horizontal speed stays below this for a short time, treat as stuck and end dash.")]
    [SerializeField] private float meleeDashStuckSpeed = 0.2f;
    [SerializeField] private float meleeDashStuckTime = 0.10f;
    [Header("Melee Dash Magnetism (Homing)")]
    [SerializeField] private float meleeDashHomingStrength = 14f;  // 越大越黏
    [SerializeField] private float meleeDashHomingMaxTurnDegPerSec = 720f; // 限制轉向，避免瞬拐
    [SerializeField] private float meleeDashLeadFallbackTime = 0.05f; // solver fail 時用的簡易 lead
    // runtime
    private bool _meleeDashActive = false;
    private bool _meleeDashIsLeft = true;
    private float _meleeDashElapsed = 0f;
    private float _meleeDashTimeout = 0f;
    private float _meleeDashMaxDistance = 0f;
    private float _meleeDashStuckElapsed = 0f;
    private Vector3 _meleeDashStartPos;
    private Vector3 _meleeDashDir;

    // --- Gravity Lock (for melee attack etc.) ---
    private int _gravityLockCount = 0;
    private bool _cachedUseGravity = true;

    [SerializeField] private BoxCollider meleeAttackWall; // A box collider that enables/disables, it is larger than the player collider to prevent enemy from glitching through player during melee attack due to the capsule collider being smaller than the player model and it round shape




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
        ApplyMeleeDashFixed(Time.fixedDeltaTime); // NEW: melee dash
        ApplyDashFixed(Time.fixedDeltaTime); // 新增

    }
    private void ApplyHorizontalMovementFixed(float dt)
    {
        if (_meleeDashActive || Time.time <= dashActiveUntil) return;

        Vector3 platformVel = GetMobilePlatformVelocity();

        // 用「相對平台」速度來做加速/減速
        Vector3 vWorld = playerRigidbody.linearVelocity;
        Vector3 vRel = vWorld - platformVel;

        Vector3 horizontalRel = new Vector3(vRel.x, 0f, vRel.z);

        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            horizontalRel = Vector3.MoveTowards(horizontalRel, Vector3.zero, PlayerStats.Instance.decelerationSpeed * dt);
            Vector3 outRel = new Vector3(horizontalRel.x, vRel.y, horizontalRel.z);
            playerRigidbody.linearVelocity = outRel + platformVel;   // ✅ 設定一次，不累加
            return;
        }

        Vector3 targetHorizontalRel = moveDirection * PlayerStats.Instance.sprintSpeed;
        horizontalRel = Vector3.MoveTowards(horizontalRel, targetHorizontalRel, PlayerStats.Instance.accelerationSpeed * dt);

        Vector3 outRel2 = new Vector3(horizontalRel.x, vRel.y, horizontalRel.z);
        playerRigidbody.linearVelocity = outRel2 + platformVel;      // ✅ 設定一次，不累加
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


        if (stats != null && (isLeft || isRight))
        {
            var hand = isLeft ? stats.leftHand : stats.rightHand;
        }

        bool attacking = meleeComboController.anim.GetBool("Attacking");
        if (IsMeleeHandAttack(attackManager, w))
        {
            if (attackInput.WasPressedThisFrame())
            {
                // if combo is running, only allow the owner hand to input
                if (meleeComboController != null && meleeComboController.anim != null)
                {
                    if (attacking)
                    {
                        // owner hand is meleeComboController's internal _isLeftHand (make a public getter)
                        if (isLeft != meleeComboController.OwnerIsLeftHand)
                            return;
                    }
                }
                // ✅ Block melee attacks while this hand is reloading/cooling down (reload starts after combo ends)
                if (w != null && w.meleeRuntime != null && w.meleeRuntime.reloading)
                    return;

                // 先判斷：按下之前係咪已經在攻擊中（Hit_1/2/3 期間都算）
                bool wasAttacking = false;
                if (meleeComboController != null && meleeComboController.anim != null)
                    wasAttacking = meleeComboController.anim.GetBool("Attacking");

                // 永遠先交俾 combo controller（起手會開 gate + 進 Dash，連段會 queue）
                if (meleeComboController != null)
                    meleeComboController.MeleeAttack(isLeft);

                // ✅ 只有「起手」（按下前不是 Attacking）先可以開 melee dash
                if (!wasAttacking)
                {
                    playerRigidbody.linearVelocity = Vector3.zero; 
                    meleeAttackWall.enabled = true; // enable the melee attack wall collider to prevent enemy from glitching through player during melee attack
                    StartMeleeDashAttack(w, isLeft);
                }
            }

            return;
        }
        else if(!attacking)
        {

            bool isSingle = w.range.firingMode == 0;


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



    // =========================
    // Melee Dash (DROP-IN FIX)
    // =========================
    private void StartMeleeDashAttack(Weapon w, bool isLeftHand)
    {
        var stats = PlayerStats.Instance;
        if (stats == null || playerRigidbody == null) return;
        if (PlayerAiming.Instance == null) return;

        float maxDist = stats.GetMeleeDashDistanceForHand(isLeftHand);
        if (maxDist <= 0f) return;

        bool allowVertical = (PlayerAiming.Instance.lockOn && PlayerAiming.Instance.aimingPoint != null);

        Vector3 dir = Vector3.zero;

        // 1) Direction source
        if (allowVertical)
        {
            // --- lock-on: 3D lead/intercept direction ---
            Vector3 tgtPos = PlayerAiming.Instance.aimingPoint.position;
            Vector3 tgtVel = Vector3.zero;

            var targetRb = PlayerAiming.Instance.GetTargetRigidbody();
            if (targetRb != null)
                tgtVel = targetRb.linearVelocity; // Unity 6

            float chaserSpeed = stats.dashSpeed;

            if (!Math.InterceptionDirection(tgtPos, transform.position, tgtVel, chaserSpeed, out dir))
                dir = (tgtPos - transform.position);

            if (dir.sqrMagnitude < 0.0001f)
                dir = (characterModel != null ? characterModel.forward : transform.forward);

            // Clamp pitch so it doesn't become "vertical rocket"
            float maxUpAngle = 65f; // tweakable
            Vector3 flat = new Vector3(dir.x, 0f, dir.z);

            Vector3 baseForward = (flat.sqrMagnitude > 0.0001f)
                ? flat.normalized
                : (characterModel != null ? characterModel.forward : transform.forward);

            dir = Vector3.RotateTowards(baseForward, dir.normalized, maxUpAngle * Mathf.Deg2Rad, 0f);
            dir.Normalize();
        }
        else
        {
            // --- free aim: keep your original horizontal feel ---
            Ray ray = PlayerAiming.Instance.GetRay();

            if (Physics.Raycast(ray, out RaycastHit hit, maxDist))
                maxDist = Mathf.Min(hit.distance, maxDist);

            dir = ray.direction;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f)
                dir = (characterModel != null ? characterModel.forward : transform.forward);
            else
                dir.Normalize();
        }

        // Only flatten when not lock-on
        if (!allowVertical)
            dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        // Visual facing: still rotate only on XZ (prevents weird model tilt)
        if (characterModel != null)
        {
            Vector3 f = dir; f.y = 0f;
            if (f.sqrMagnitude > 0.0001f)
                characterModel.forward = f.normalized;
        }

        // 2) Setup runtime
        _meleeDashActive = true;
        _meleeDashIsLeft = isLeftHand;
        _meleeDashElapsed = 0f;
        _meleeDashStuckElapsed = 0f;
        _meleeDashStartPos = transform.position;
        _meleeDashDir = dir;
        _meleeDashMaxDistance = maxDist;

        // 3) Reuse dash feel (accel)
        Vector3 v = playerRigidbody.linearVelocity;

        dashDir = dir;
        dashTargetSpeed = stats.dashSpeed;

        // Use full velocity projection (works for both 2D and 3D dash)
        float vParallel = Vector3.Dot(v, dashDir);
        float deltaVStart = Mathf.Max(0f, dashTargetSpeed - vParallel);

        float dur = Mathf.Max(0.02f, dashDuration);
        dashAccel = deltaVStart / dur;

        // 4) Timeout
        float est = (dashTargetSpeed > 0.01f) ? (maxDist / dashTargetSpeed) : 0.2f;
        _meleeDashTimeout = Mathf.Clamp(est + meleeDashTimeExtra, 0.15f, meleeDashMaxTimeClamp);
    }

    private void ApplyMeleeDashFixed(float dt)
    {
        if (!_meleeDashActive) return;
        if (playerRigidbody == null) return;

        _meleeDashElapsed += dt;

        // Stop conditions
        float traveled = Vector3.Distance(_meleeDashStartPos, transform.position);
        bool stop = traveled >= _meleeDashMaxDistance;

        // lock-on early stop
        if (!stop && PlayerAiming.Instance != null && PlayerAiming.Instance.lockOn && PlayerAiming.Instance.aimingPoint != null)
        {
            float d = Vector3.Distance(transform.position, PlayerAiming.Instance.aimingPoint.position);
            if (d <= meleeDashStopDistance) stop = true;
        }

        // timeout
        if (!stop && _meleeDashElapsed >= _meleeDashTimeout) stop = true;

        Vector3 v = playerRigidbody.linearVelocity;

        // ✅ stuck detect: use speed ALONG dash direction (fixes air-target false-stuck)
        float alongSpeed = Mathf.Abs(Vector3.Dot(v, dashDir));
        if (alongSpeed <= meleeDashStuckSpeed)
            _meleeDashStuckElapsed += dt;
        else
            _meleeDashStuckElapsed = 0f;

        if (!stop && _meleeDashStuckElapsed >= meleeDashStuckTime) stop = true;

        if (stop)
        {
            EndMeleeDashAndFireHit1();
            return;
        }

        // Push (cap once reached)
        float vParallel = Vector3.Dot(v, dashDir);
        if (vParallel >= dashTargetSpeed) return;

        // ===== Magnetism / Homing (lock-on only) =====
        if (PlayerAiming.Instance != null &&
            PlayerAiming.Instance.lockOn &&
            PlayerAiming.Instance.aimingPoint != null)
        {
            Vector3 tgtPos = PlayerAiming.Instance.aimingPoint.position;

            Vector3 tgtVel = Vector3.zero;
            var targetRb = PlayerAiming.Instance.GetTargetRigidbody();
            if (targetRb != null) tgtVel = targetRb.linearVelocity; // Unity 6

            // 用你提供嘅 solver：算「攔截點」再朝住佢衝
            Vector3 interceptPoint;
            bool ok = Math.InterceptionPoint(
                tgtPos,                     // a = target position
                transform.position,         // b = chaser position
                tgtVel,                     // vA = target velocity
                dashTargetSpeed > 0.01f ? dashTargetSpeed : PlayerStats.Instance.dashSpeed, // sB = our speed
                out interceptPoint
            );

            Vector3 desiredDir;
            if (ok)
            {
                desiredDir = (interceptPoint - transform.position);
            }
            else
            {
                // solver fail：用簡易 lead 做 fallback
                Vector3 pred = tgtPos + tgtVel * meleeDashLeadFallbackTime;
                desiredDir = (pred - transform.position);
            }

            if (desiredDir.sqrMagnitude > 0.0001f)
            {
                desiredDir.Normalize();

                // 轉向限制：避免瞬間「硬拐」造成抖/怪
                float maxRad = meleeDashHomingMaxTurnDegPerSec * Mathf.Deg2Rad * dt;
                Vector3 limited = Vector3.RotateTowards(_meleeDashDir, desiredDir, maxRad, 0f);

                // 磁吸平滑（越大越黏）
                _meleeDashDir = Vector3.Slerp(_meleeDashDir, limited, meleeDashHomingStrength * dt);
                _meleeDashDir.Normalize();

                dashDir = _meleeDashDir;
            }
        }

        playerRigidbody.AddForce(dashDir * dashAccel, ForceMode.Acceleration);
    }

    private void EndMeleeDashAndFireHit1()
    {
        if (!_meleeDashActive) return;
        _meleeDashActive = false;
        attackFacingActive = false;
        // Reduce residual horizontal speed a bit (so "dash ends" feels immediate, without hard zero)
        Vector3 v = playerRigidbody.linearVelocity;
        Vector3 hv = new Vector3(v.x, 0f, v.z) * 0.5f;
        playerRigidbody.linearVelocity = new Vector3(hv.x, v.y, hv.z);

        if (meleeComboController != null)
            meleeComboController.FireHit1FromDash(_meleeDashIsLeft);
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


        if (attackFacingActive)
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

    public bool IsDashActive => Time.time <= dashActiveUntil;
    public bool IsMeleeDashActive => _meleeDashActive;
    public float VerticalVelocity
    {
        get
        {
            if (playerRigidbody == null) return 0f;
            return playerRigidbody.linearVelocity.y;
        }
    }
    private bool IsMeleeHandAttack(AttackManager attackManager, Weapon w)
    {
        var stats = PlayerStats.Instance;
        if (stats == null || attackManager == null || w == null) return false;

        // 只針對左右手；肩膀目前你的資料結構係 Range only
        if (w == attackManager.leftHandWeapon) return stats.leftHand.weaponKind == HandWeaponKind.Melee;
        if (w == attackManager.rightHandWeapon) return stats.rightHand.weaponKind == HandWeaponKind.Melee;

        return false;
    }

    public void LockGravity()
    {
        if (playerRigidbody == null) return;

        if (_gravityLockCount == 0)
        {
            _cachedUseGravity = playerRigidbody.useGravity;
            playerRigidbody.useGravity = false;   // ✅ gravity = 0
        }

        _gravityLockCount++;
    }

    public void UnlockGravity()
    {
        if (playerRigidbody == null) return;

        _gravityLockCount = Mathf.Max(0, _gravityLockCount - 1);

        if (_gravityLockCount == 0)
        {
            playerRigidbody.useGravity = _cachedUseGravity; // ✅ restore
        }
    }
    public void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Hit: " + collision.gameObject + ",tag:" + collision.gameObject.tag + ", linearVelocity: "+ playerRigidbody.linearVelocity.magnitude);
        if (collision.gameObject.tag == "Enemy" && playerRigidbody.linearVelocity.magnitude > 5f)
        {
            Debug.Log("Weeeeeeeee");
            playerAnimation.MeleeAttackFeedback.PlayFeedbacks(this.transform.position);
        }

    }
    // Mobile Platform carry (velocity-based)
    private bool _onMobilePlatform = false;
    private Rigidbody _mobilePlatformRb = null;
    public void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Mobile Platform"))
        {
            _mobilePlatformRb = other.GetComponentInParent<Rigidbody>();
            _onMobilePlatform = (_mobilePlatformRb != null);
        }
    }
    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Mobile Platform"))
        {
            _onMobilePlatform = false;
            _mobilePlatformRb = null;
        }
    }

    private Vector3 GetMobilePlatformVelocity()
    {
        if (!_onMobilePlatform || _mobilePlatformRb == null) return Vector3.zero;
        return _mobilePlatformRb.linearVelocity; // Unity 6
    }

}