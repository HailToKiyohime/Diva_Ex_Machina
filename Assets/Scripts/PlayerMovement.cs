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
    [Tooltip("Dash 期間禁止攻擊/射擊的鎖定秒數。填你 dash 動畫的實際長度；想跟位移視窗一致就填和 dashDuration 一樣。")]
    [SerializeField] private float dashAttackLockDuration = 0.35f;
    private float dashAttackLockUntil = 0f;

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

    [Header("Dust Effect")]
    [SerializeField] private float dustIntervalSlow = 0.4f;   // 慢速：間隔長 = 稀
    [SerializeField] private float dustIntervalFast = 0.08f;  // 全速：間隔短 = 密
    [SerializeField] private float dustMinSpeed = 1f;         // 低於此速度不噴
    [SerializeField] private float dustMaxSpeed = 16f;        // 達此速度 = 最密(60km/h≈16.7)
    private float _nextDustTime = 0f;

    public AttackManager attackManager;
    public void Update()
    {
        EnergyRegenerationCheck();
        RotateCharacter();
        HandleDustEffect();
        if (!grounded)
        {
            playerAnimation.StopWalkFeedback();
        }
        UIManager.Instance.speedText.text = playerRigidbody.linearVelocity.magnitude < 0.0001f ? 0.ToString("F2") : playerRigidbody.linearVelocity.magnitude.ToString("F2");
    }
    public void FixedUpdate()
    {
        CarryWithPlatformRotation(Time.fixedDeltaTime);
        GroundCheck();
        ApplyHorizontalMovementFixed(Time.fixedDeltaTime);
        ApplyFlyFixed(Time.fixedDeltaTime); // 新增
        ApplyDashFixed(Time.fixedDeltaTime); // 新增
        ApplyMeleeDashFixed(Time.fixedDeltaTime);
        ApplyMeleeAnchorFixed(Time.fixedDeltaTime);
        ApplyMeleeHoverFixed(Time.fixedDeltaTime);
        ApplyMeleeBrakeFixed(Time.fixedDeltaTime);

    }
    private void ApplyHorizontalMovementFixed(float dt)
    {

        Vector3 platformVel = GetMobilePlatformVelocity();

        // 用「相對平台」速度來做加速/減速
        Vector3 vWorld = playerRigidbody.linearVelocity;
        Vector3 vRel = vWorld - platformVel;

        // 滯空中連垂直速度一起煞掉 —— 沒有重力可以自然衰減它，
        // 殘留的 Y 會讓角色在空中緩慢飄走。
        Vector3 horizontalRel = _meleeHovering
            ? vRel
            : new Vector3(vRel.x, 0f, vRel.z);

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
        // Dash 動畫播放期間：禁止任何攻擊/射擊（涵蓋左右手、肩武器、近戰）
        if (IsDashAttackBlocked) return;

        var stats = PlayerStats.Instance;
        bool isLeft = (attackManager != null && w == attackManager.leftHandWeapon);
        bool isRight = (attackManager != null && w == attackManager.rightHandWeapon);
        bool isLeftShoulderAttack = (attackManager != null && w == attackManager.leftShoulderWeapon);
        bool isRightShoulderAttack = (attackManager != null && w == attackManager.rightShoulderWeapon);


        if (stats != null && (isLeft || isRight))
        {
            var hand = isLeft ? stats.leftHand : stats.rightHand;
        }



        bool isSingle = w.range.firingMode == 0;

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

        float singleAlignDeadline = Mathf.Min(maxAlignTime, singleShotBufferTime * 0.5f);
        bool timedOut = alignStartTime.TryGetValue(w, out float startT)
                        && (Time.time - startT) >= (isSingle ? singleAlignDeadline : maxAlignTime);

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

    private void ApplyDashFixed(float dt)
    {
        if (Time.time > dashRequestUntil) return;

        if (Time.time > dashActiveUntil)
        {
            dashRequestUntil = 0f;
            return;
        }

        Vector3 platformVel = GetMobilePlatformVelocity();

        // 用「相對平台」速度來判斷是否達標
        Vector3 vWorld = playerRigidbody.linearVelocity;
        Vector3 vRel = vWorld - platformVel;

        // 滯空中連垂直速度一起煞掉 —— 沒有重力可以自然衰減它，
        // 殘留的 Y 會讓角色在空中緩慢飄走。
        Vector3 horizontalRel = _meleeHovering
            ? vRel
            : new Vector3(vRel.x, 0f, vRel.z);
        float vParallelRel = Vector3.Dot(horizontalRel, dashDir);

        if (vParallelRel >= dashTargetSpeed) return;

        // 推力仍然是世界空間，會同等增加 world 與 relative（因為平台速度是常數項）
        playerRigidbody.AddForce(dashDir * dashAccel, ForceMode.Acceleration);
    }

    public bool DashAction()
    {
        var stats = PlayerStats.Instance;
        if (stats == null) return false;

        // 近戰揮擊中，且這一段標記為不可取消 → 擋掉 Dash
        if (meleeController != null && meleeController.IsBlockingDash) return false;

        if (Time.time < nextDashTime) return false;
        if (stats.currentEnergy <= 0) return false;

        Vector3 dir;
        if (moveDirection.sqrMagnitude > 0.001f) dir = moveDirection;
        else dir = characterModel != null ? characterModel.forward : transform.forward;

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return false;
        dir.Normalize();

        Vector3 platformVel = GetMobilePlatformVelocity();

        // 用「相對平台」速度來計算起始差值
        Vector3 vWorld = playerRigidbody.linearVelocity;
        Vector3 vRel = vWorld - platformVel;

        // 滯空中連垂直速度一起煞掉 —— 沒有重力可以自然衰減它，
        // 殘留的 Y 會讓角色在空中緩慢飄走。
        Vector3 horizontalRel = _meleeHovering
            ? vRel
            : new Vector3(vRel.x, 0f, vRel.z);

        dashDir = dir;
        dashTargetSpeed = stats.dashSpeed;

        float vParallelRel = Vector3.Dot(horizontalRel, dashDir);
        float deltaVStart = Mathf.Max(0f, dashTargetSpeed - vParallelRel);

        float dur = Mathf.Max(0.02f, dashDuration);
        dashAccel = deltaVStart / dur;

        stats.currentEnergy -= stats.dashEnergyCost;
        // Dash 期間鎖住攻擊/射擊
        float dashAtkLock = (dashAttackLockDuration > 0f) ? dashAttackLockDuration : dashDuration;
        dashAttackLockUntil = Time.time + dashAtkLock;
        dashRequestUntil = Time.time + dashInputBuffer;
        dashActiveUntil = Time.time + dashDuration;
        nextDashTime = Time.time + dashCooldown;

        // ★ 順序很重要：先收掉近戰，再播 Dash 動畫。
        //   近戰層是 Override 權重 1，CancelByDash → SetOffAttackLayer 需要
        //   attackLayerBlendOutTime 才淡出。若先 setDashTrigger，這段期間
        //   Dash 動畫會被近戰動畫蓋住，看起來像「沒播放」。
        CancelMeleeBrake();
        StopMeleeDash("PlayerDashCancel");
        EndMeleeHover();
        EndMeleeAnchor();

        if (meleeController != null)
            meleeController.CancelByDash();

        playerAnimation.setDashTrigger();

        CancelInvoke("ResetEnergyRegenerate");
        canRegenerateEnergy = false;


        return true;
    }

    // ────────────────────────────────────────────────
    //  近戰突進
    //
    //  跟一般 Dash 的差別：
    //    - 不受 dashCooldown 限制（它是攻擊的一部分，不是逃生手段）
    //    - 不設 dashAttackLockUntil（不然連段會被自己鎖住）
    //    - 能量由 MeleeAttackController 結算（一串連段只付一次入場費）
    //
    //  跟一般 Dash 相同的地方：全部走「相對平台」的速度判定。
    //  在移動中的 Landship 上，世界空間的位移會被船速污染，所以進度必須
    //  用相對平台的累積位移來算。
    // ────────────────────────────────────────────────

    [Header("Melee Dash")]
    [SerializeField] private MeleeAttackController meleeController;

    [Tooltip("突進的加速度。越大越快貼到目標速度，太小會軟綿綿。")]
    [SerializeField] private float meleeDashAccel = 400f;

    [Tooltip("dashCurve 末端趨近 0 時的速度下限倍率，避免永遠走不完全程。")]
    [SerializeField] private float meleeDashMinSpeedFactor = 0.08f;

    [Tooltip("突進開始時保留多少比例的既有速度（0~1）。\n\n" +
             "★ 這個歸零是必要的，不是手感選項。攔截解算假設你會從當前位置以\n" +
             "  dashSpeed 朝攔截點前進；若帶著既有速度進入，實際速度會是\n" +
             "  「原速度 + 突進加速」，方向與大小雙雙偏離解算前提。\n" +
             "  更糟的是 ApplyMeleeDashFixed 只在「未達目標速度」時加力 ——\n" +
             "  初速夠高時一次力都不會加，整段突進純靠殘餘動量滑行。\n\n" +
             "0.1 保留一點慣性，讓起手不會像撞到牆一樣硬停。")]
    [Range(0f, 1f)]
    [SerializeField] private float meleeDashEntrySpeedFactor = 0.1f;

    [Tooltip("攔截解算的速度比上限。目標速度 / 突進速度 超過這個值就不解，直接純追蹤。\n" +
             "接近 1 時二次方程趨於退化，會解出極遠或負的根 —— 那些解看起來合法\n" +
             "（方向也對），實際上是朝一個永遠追不到的幻影衝過去。")]
    [Range(0.3f, 0.99f)]
    [SerializeField] private float interceptMaxSpeedRatio = 0.85f;

    [Tooltip("攔截點距離的上限倍率，相對於「目標當前距離」。\n" +
             "解出的攔截點比目標遠這麼多倍就拒絕 —— 合理的提前量不會離譜到那種程度。")]
    [SerializeField] private float interceptMaxLeadMultiplier = 2.5f;

    [Tooltip("攔截解對應的預估抵達時間上限（秒）。超過就拒絕。\n" +
             "太遠的解在抵達前戰況早就變了，追它不如純追蹤。")]
    [SerializeField] private float interceptMaxSolveTime = 1.0f;

    [Tooltip("突進結束時在 Console 輸出診斷：預測 vs 實際、結束原因、最終偏差分解。")]
    [SerializeField] private bool logMeleeDashDiagnostics = false;

    [Tooltip("AnimEvent_MeleeBrake 的煞車時間（秒）。0.08~0.15 通常就很俐落。")]
    [SerializeField] private float meleeBrakeDuration = 0.12f;

    [Tooltip("煞車後保留多少比例的原速度（0~1）。\n\n" +
             "0 = 完全停住（很生硬，像急煞）。\n" +
             "0.25 = 卸掉大半衝勢但仍在滑行，比較像機體慣性。\n" +
             "方向不變，只縮短度 —— 收招後仍朝原方向緩緩前進。")]
    [Range(0f, 1f)]
    [SerializeField] private float meleeBrakeSpeedFactor = 0.25f;

    [Tooltip("突進期間的最大轉向速率（度/秒）。\n" +
             "高速戰鬥下一次性攔截幾乎必定落空，所以突進全程持續重算攔截點。\n" +
             "但轉向有上限 —— 角度太離譜就是追不上，保留「位置沒站好會 miss」的懲罰。\n" +
             "360 = 每秒一圈，相當靈活；120~180 較誠實。")]
    [SerializeField] private float meleeDashTurnRate = 270f;

    [Tooltip("命中後繼承目標速度的比例（0~1）。\n\n" +
             "0 = 完全不繼承，雙方高速時會立刻滑開（AC6 式，只重視第一下）。\n" +
             "1 = 完全貼住目標（DMC5 式，會失去駕駛機體的感覺）。\n" +
             "0.6~0.8 = 相對漂移大幅降低但仍存在，玩家還是要自己補位。\n\n" +
             "從 hitbox 開啟（命中判定開始）起算，維持到整串連段結束。")]
    [Range(0f, 1f)]
    [SerializeField] private float meleeAnchorFactor = 0.7f;

    [Header("Melee Pitch")]
    [Tooltip("近戰攻擊時，角色朝目標俯仰的最大角度。0 = 停用。\n" +
             "三維突進衝向空中目標時，角色若維持水平會很不協調。")]
    [Range(0f, 80f)]
    [SerializeField] private float meleeMaxPitch = 30f;

    [Tooltip("俯仰角的轉動速度。攻擊結束後也用這個速度轉回 0。")]
    [SerializeField] private float meleePitchSpeed = 8f;

    [Tooltip("近戰滯空期間抵銷多少比例的重力。\n" +
             "從突進開始，一直到這一段攻擊完全結束（StepEnd / 取消）為止。\n" +
             "1 = 完全無重力（打飛行敵人時軌跡筆直），0 = 照常掉落。")]
    [Range(0f, 1f)]
    [SerializeField] private float meleeDashGravityCancel = 1f;

    private bool _meleeDashing;
    private Vector3 _meleeDashDir;
    private float _meleeDashSpeed;        // 這次突進的最大速度
    private float _meleeDashDistance;     // 這次突進的總距離
    private float _meleeDashTravelled;    // 已走距離（相對平台）
    private float _meleeDashStopDistance; // ToTarget 用
    private Rigidbody _meleeDashTarget;   // null = Forward 模式。用 Rigidbody 才讀得到速度做攔截預判
    private AnimationCurve _meleeDashCurve;

    // 滯空跟突進是「不同長度」的兩件事：突進在距離走完就停，
    // 但滯空要持續到整段攻擊結束，否則刀還沒揮完人已經開始掉了。
    private bool _meleeHovering;
    private bool _meleeDashPrevGravity = true;

    private bool _meleeBraking;
    private float _meleeBrakeUntil;
    private Vector3 _meleeBrakeTargetVel;

    public bool IsMeleeDashing => _meleeDashing;
    public bool IsMeleeBraking => _meleeBraking;

    /// <summary>近戰滯空中（從突進開始到整段攻擊結束）。給後處理 / VFX 判斷用。</summary>
    public bool IsMeleeHovering => _meleeHovering;

    /// <summary>目前是否錨定在某個近戰目標上。</summary>
    public bool IsMeleeAnchored => _meleeAnchorTarget != null;

    /// <summary>
    /// 收招煞車。由 MeleeAttackController 在 AnimEvent_MeleeStepEnd 時呼叫。
    ///
    /// 只歸零「相對平台」的水平速度 —— 在移動的 Landship 上歸零世界速度
    /// 會讓角色被船直接甩到船尾。垂直速度不動，收招時該落地還是要落地。
    ///
    /// 注意：被 Dash 取消時不會走這裡。玩家按 Shift 是要逃走，煞車會擋住他。
    /// </summary>
    public void BeginMeleeBrake()
    {
        // Dash 進行中不接受煞車。控制器那邊已經有 _attacking 守衛擋掉
        // 「被取消的刀稍後才觸發 Brake」的情況，這裡是第二道防線 ——
        // 煞車把玩家的逃生 Dash 歸零是絕對不能發生的事。
        if (Time.time < dashActiveUntil) return;

        // 在煞車開始的當下擷取速度，目標設成它的一個比例。
        // 每幀重算的話，衰減中的速度會讓目標一路跟著往下掉，最後還是歸零。
        Vector3 platformVel = GetMobilePlatformVelocity();
        Vector3 vRel = (playerRigidbody != null)
            ? playerRigidbody.linearVelocity - platformVel
            : Vector3.zero;

        _meleeBrakeTargetVel = vRel * Mathf.Clamp01(meleeBrakeSpeedFactor);

        _meleeBraking = true;
        _meleeBrakeUntil = Time.time + Mathf.Max(0f, meleeBrakeDuration);
    }

    public void CancelMeleeBrake()
    {
        _meleeBraking = false;
        _meleeBrakeUntil = 0f;
        _meleeBrakeTargetVel = Vector3.zero;
    }

    // 滯空期間的殘留重力。
    //
    // useGravity 在 BeginMeleeHover 就關掉了，所以這裡是「加回沒被抵銷的那部分」，
    // 而不是「抵銷掉一部分」—— 方向搞反的話角色會被往上推飛。
    //   meleeDashGravityCancel = 1 → 完全無重力，什麼都不加
    //   meleeDashGravityCancel = 0 → 加回全部重力，等同沒有滯空
    // ────────────────────────────────────────────────
    //  近戰錨定（部分速度繼承）
    //
    //  高速戰鬥的核心問題：所有近戰數值都在世界空間定義，但交戰發生在一個
    //  高速移動的參考系裡。相對速度 8 m/s 時，dashStopDistance = 0.2 只夠撐
    //  0.025 秒 —— 一個物理幀都不到就滑開了。
    //
    //  錨定把目標速度的一部分注入玩家，讓相對速度降下來，數值才回到「雙方
    //  靜止」的前提。factor 不設 1 是刻意的：保留一點漂移，玩家仍要自己補位，
    //  不會變成貼在敵人身上。
    //
    //  作法是「增量注入」：每幀只補上與上次注入量的差額，不去覆寫速度，
    //  所以突進、煞車、一般移動都能照常運作，互不干擾。
    // ────────────────────────────────────────────────

    private Rigidbody _meleeAnchorTarget;
    private Vector3 _appliedAnchorVel;

    // ───── 突進診斷 ─────
    private enum MeleeDashEndReason { DistanceExhausted, ReachedStopDistance, ExternalStop }

    private float _dbgStartTime;
    private Vector3 _dbgStartPos;
    private Vector3 _dbgTargetStartPos;
    private Vector3 _dbgTargetVelRel;
    private Vector3 _dbgInterceptPoint;
    private bool _dbgHasIntercept;
    private float _dbgPredictedTime;
    private float _dbgPredictedDist;
    private float _dbgPeakSpeed;
    private bool _dbgCaptured;
    private string _dbgStopSource = "unknown";

    // 目前的俯仰角（度，正值 = 低頭 / 往下看，Unity 的 X 軸旋轉方向）
    private float _meleePitchCurrent;

    /// <summary>
    /// 更新近戰俯仰角。
    ///
    /// 只在近戰攻擊進行中朝目標俯仰，攻擊結束就平滑回 0 —— 不然角色會維持
    /// 斜著跑步。整段攻擊都有角度（不只突進），因為近戰在這遊戲是主武器，
    /// 揮刀時對著目標比只在突進時對著更一致。
    /// </summary>
    private void UpdateMeleePitch()
    {
        float desired = 0f;

        bool attacking = (meleeController != null) && meleeController.IsAttacking;

        if (attacking && meleeMaxPitch > 0f && characterModel != null)
        {
            Transform target = GetMeleePitchTarget();
            if (target != null)
            {
                Vector3 dir = target.position - characterModel.position;

                if (dir.sqrMagnitude > 0.0001f)
                {
                    // asin(y) 得到與水平面的夾角。取負是因為 Unity 的 X 軸正旋轉是低頭。
                    float angle = Mathf.Asin(Mathf.Clamp(dir.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
                    desired = Mathf.Clamp(-angle, -meleeMaxPitch, meleeMaxPitch);
                }
            }
        }

        _meleePitchCurrent = Mathf.Lerp(
            _meleePitchCurrent, desired, Time.deltaTime * Mathf.Max(0.01f, meleePitchSpeed));

        if (Mathf.Abs(_meleePitchCurrent) < 0.01f) _meleePitchCurrent = 0f;
    }

    // 錨定中的目標優先（那是真正在打的對象），否則用鎖定目標
    private Transform GetMeleePitchTarget()
    {
        if (_meleeAnchorTarget != null && _meleeAnchorTarget.gameObject.activeInHierarchy)
            return _meleeAnchorTarget.transform;

        if (PlayerAiming.Instance != null)
        {
            var rb = PlayerAiming.Instance.GetTargetRigidbody();
            if (rb != null) return rb.transform;
        }

        return null;
    }

    /// <summary>
    /// 開始錨定。由 MeleeAttackController 在 hitbox 開啟時呼叫 ——
    /// 從命中判定開始才跟上，感覺是「砍中了所以跟著走」，
    /// 而不是突進階段就被吸過去。
    /// </summary>
    public void BeginMeleeAnchor(Rigidbody target)
    {
        if (target == null || meleeAnchorFactor <= 0f) return;
        if (_meleeAnchorTarget == target) return;

        // 換目標時先把舊的注入量退掉，避免速度累加
        ReleaseAnchorVelocity();
        _meleeAnchorTarget = target;
    }

    /// <summary>結束錨定。連段整串結束或被取消時呼叫。</summary>
    public void EndMeleeAnchor()
    {
        ReleaseAnchorVelocity();
        _meleeAnchorTarget = null;
    }

    // 把先前注入的速度收回。不收的話玩家會永久保留敵人的速度。
    private void ReleaseAnchorVelocity()
    {
        if (_appliedAnchorVel.sqrMagnitude > 0f && playerRigidbody != null)
            playerRigidbody.linearVelocity -= _appliedAnchorVel;

        _appliedAnchorVel = Vector3.zero;
    }

    private void ApplyMeleeAnchorFixed(float dt)
    {
        if (_meleeAnchorTarget == null)
        {
            if (_appliedAnchorVel.sqrMagnitude > 0f) ReleaseAnchorVelocity();
            return;
        }

        if (playerRigidbody == null) { EndMeleeAnchor(); return; }

        // 目標被銷毀（打死了）→ 解除錨定
        if (!_meleeAnchorTarget.gameObject.activeInHierarchy)
        {
            EndMeleeAnchor();
            return;
        }

        // 目標速度要扣掉平台速度：雙方都站在 Landship 上時，
        // 共同的船速不該被當成需要跟隨的相對運動。
        Vector3 platformVel = GetMobilePlatformVelocity();
        Vector3 targetVelRel = _meleeAnchorTarget.linearVelocity - platformVel;

        Vector3 desired = targetVelRel * Mathf.Clamp01(meleeAnchorFactor);

        // 增量注入：只補差額，不覆寫既有速度
        Vector3 delta = desired - _appliedAnchorVel;
        playerRigidbody.linearVelocity += delta;
        _appliedAnchorVel = desired;
    }

    private void ApplyMeleeHoverFixed(float dt)
    {
        if (!_meleeHovering || playerRigidbody == null) return;

        float residual = 1f - Mathf.Clamp01(meleeDashGravityCancel);
        if (residual <= 0f) return;

        playerRigidbody.AddForce(Physics.gravity * residual, ForceMode.Acceleration);
    }

    private void ApplyMeleeBrakeFixed(float dt)
    {
        // ★ 用 bool 當閘門，不要用「時間 + duration」組合條件。
        //   之前寫成 (Time.time >= until && duration > 0) 的話，duration = 0 會讓
        //   整個條件恆為 false —— 煞車每幀都跑，把 Dash 在內的所有水平移動都吃掉。
        if (!_meleeBraking) return;

        if (playerRigidbody == null) { CancelMeleeBrake(); return; }

        // duration = 0 表示立即歸零，這一幀做完就結束
        if (meleeBrakeDuration > 0f && Time.time >= _meleeBrakeUntil)
        {
            CancelMeleeBrake();
            return;
        }

        Vector3 platformVel = GetMobilePlatformVelocity();
        Vector3 vRel = playerRigidbody.linearVelocity - platformVel;

        // 三維煞車：垂直分量也要煞。
        // 向上突進 25 m/s 打空中目標之後若保留 Y，角色會繼續往上飛。
        // 歸零 Y 之後重力接手，角色停在半空再落下 —— 空中攻擊的標準收尾。
        // 落地狀態下 vRel.y 本來就接近 0，所以不影響地面戰鬥。
        float k = (meleeBrakeDuration > 0f)
            ? Mathf.Clamp01(dt / meleeBrakeDuration * 3f)
            : 1f;

        vRel = Vector3.Lerp(vRel, _meleeBrakeTargetVel, k);

        // 夠接近目標速度就收工。注意這裡比的是「與目標的差距」而不是速度本身 ——
        // 保留 1/4 動量時速度永遠不會歸零，用絕對值判斷會一直煞下去。
        if ((vRel - _meleeBrakeTargetVel).sqrMagnitude < 0.01f || meleeBrakeDuration <= 0f)
        {
            vRel = _meleeBrakeTargetVel;
            CancelMeleeBrake();
        }

        playerRigidbody.linearVelocity = vRel + platformVel;
    }

    /// <summary>
    /// 開始一次近戰突進。由 MeleeAttackController 在 AnimEvent_MeleeDashStart 時呼叫。
    /// speed 與 distance 已經由控制器依能量結算過（強化 / 衰弱模式）。
    /// </summary>
    public void BeginMeleeDash(Vector3 direction, float speed, float distance,
                               AnimationCurve curve, Rigidbody target, float stopDistance)
    {
        // 三維：不再壓平 Y。飛行敵人在水平突進下是打不到的。
        if (direction.sqrMagnitude < 0.0001f || distance <= 0f || speed <= 0f)
        {
            StopMeleeDash("invalidDashParams");
            return;
        }

        _meleeDashDir = direction.normalized;
        _meleeDashSpeed = speed;
        _meleeDashDistance = distance;
        _meleeDashTravelled = 0f;
        _meleeDashCurve = curve;
        _meleeDashTarget = target;
        _meleeDashStopDistance = Mathf.Max(0f, stopDistance);
        _meleeDashing = true;

        ApplyDashEntryBraking();

        _dbgStartTime = Time.time;
        _dbgStartPos = (playerRigidbody != null) ? playerRigidbody.position : transform.position;
        _dbgTargetStartPos = (target != null) ? target.position : Vector3.zero;
        _dbgPeakSpeed = 0f;
        _dbgCaptured = false;
        _dbgHasIntercept = false;

        BeginMeleeHover();
    }

    // 突進起手的速度歸零。
    //
    // 這不是手感選項，是正確性需求：攔截解算假設你從當前位置以 dashSpeed
    // 朝攔截點前進。帶著既有速度進入的話，實際速度變成「原速度 + 突進加速」，
    // 方向與大小雙雙偏離解算前提。
    //
    // 更糟的是 ApplyMeleeDashFixed 只在「未達目標速度」時加力 —— 初速夠高時
    // 一次力都不會加，整段突進純靠殘餘動量往原方向滑行。
    //
    // 相對平台計算：在移動的 Landship 上歸零世界速度會讓角色被船甩到船尾。
    // 三維歸零（含 Y）：突進本身是三維的，殘留的垂直速度同樣會污染解算。
    private void ApplyDashEntryBraking()
    {
        if (playerRigidbody == null) return;

        Vector3 platformVel = GetMobilePlatformVelocity();
        Vector3 vRel = playerRigidbody.linearVelocity - platformVel;

        playerRigidbody.linearVelocity =
            vRel * Mathf.Clamp01(meleeDashEntrySpeedFactor) + platformVel;

        // 起手歸零後，先前的煞車視窗已無意義 —— 留著會把突進的第一波加速吃掉
        CancelMeleeBrake();
    }

    /// <summary>
    /// 開始滯空。關掉重力，讓突進與後續揮擊維持在同一個高度。
    /// 由 BeginMeleeDash 自動呼叫，持續到 EndMeleeHover。
    /// </summary>
    private void BeginMeleeHover()
    {
        if (_meleeHovering || playerRigidbody == null) return;

        _meleeHovering = true;
        _meleeDashPrevGravity = playerRigidbody.useGravity;
        playerRigidbody.useGravity = false;
    }

    /// <summary>
    /// 結束滯空、恢復重力。由 MeleeAttackController 在整段攻擊結束
    /// （StepEnd / Dash 取消）時呼叫 —— 不是突進結束時。
    /// </summary>
    public void EndMeleeHover()
    {
        if (!_meleeHovering) return;

        _meleeHovering = false;
        if (playerRigidbody != null)
            playerRigidbody.useGravity = _meleeDashPrevGravity;
    }

    /// <summary>停止突進。距離走完、撞到停止距離、StepEnd、或被 Dash 取消時呼叫。</summary>
    // 攔截解算 + 防呆。
    //
    // MathToolKit.InterceptionPoint 解的是二次方程，在「目標速度接近或超過自身速度」時
    // 會退化：解出的根可能在極遠處或負的。那些解通過方向檢查（Dot > 0）卻完全不可用 ——
    // 實測見過解出 229 公尺外的攔截點，玩家朝幻影衝過去，最後差了 49 公尺。
    //
    // 三道防呆，任一不過就退回純追蹤（朝目標當前位置，仍然每幀重算）：
    //   1) 速度比太高 → 根本不解
    //   2) 攔截點比目標遠太多倍 → 拒絕
    //   3) 預估抵達時間太長 → 拒絕（那麼久之後戰況早變了）
    private bool TrySolveIntercept(Vector3 selfPos, Vector3 targetPos,
                                   Vector3 toTarget, Vector3 targetVelRel,
                                   out Vector3 intercept)
    {
        intercept = targetPos;

        if (targetVelRel.sqrMagnitude <= 0.01f) return false;

        float dashSpeed = Mathf.Max(0.01f, _meleeDashSpeed);

        // 1) 速度比：接近 1 時方程退化
        if (targetVelRel.magnitude / dashSpeed >= interceptMaxSpeedRatio) return false;

        if (!MathToolKit.InterceptionPoint(targetPos, selfPos, targetVelRel, dashSpeed, out var solved))
            return false;

        Vector3 toIntercept = solved - selfPos;
        if (toIntercept.sqrMagnitude <= 0.0001f) return false;

        // 解在背後
        if (Vector3.Dot(toIntercept.normalized, toTarget.normalized) <= 0f) return false;

        float interceptDist = toIntercept.magnitude;
        float targetDist = toTarget.magnitude;

        // 2) 提前量離譜
        if (interceptDist > targetDist * Mathf.Max(1f, interceptMaxLeadMultiplier)) return false;

        // 3) 抵達時間太長
        if (interceptDist / dashSpeed > Mathf.Max(0.05f, interceptMaxSolveTime)) return false;

        intercept = solved;
        return true;
    }

    // 突進結束診斷。回答三個問題：
    //   1) 我到得夠快嗎？      預測時間 vs 實際時間、峰值 / 平均速度 vs 解算假設速度
    //   2) 我的距離預算夠嗎？  已走距離 vs 預算、攔截點距離 vs 預算
    //   3) 我最後差在哪？      把最終誤差分解成「沿目標移動方向」與「側向」兩個分量
    //
    // 「落在對方後方」= alongTargetVel 為負值且絕對值大 → 抵達太慢。
    // 「停在半路」    = travelled 貼齊預算且 finalGap 很大 → 距離不足。
    private void LogDashDiagnostics(MeleeDashEndReason reason)
    {
        if (!logMeleeDashDiagnostics || !_meleeDashing) return;

        float elapsed = Time.time - _dbgStartTime;
        Vector3 endPos = (playerRigidbody != null) ? playerRigidbody.position : transform.position;

        float avgSpeed = (elapsed > 0.0001f) ? (_meleeDashTravelled / elapsed) : 0f;

        string targetPart = "target=none";

        if (_meleeDashTarget != null)
        {
            Vector3 targetPos = _meleeDashTarget.position;
            Vector3 gap = targetPos - endPos;

            // 誤差分解：沿著目標移動方向的分量最有診斷價值。
            // 負值代表目標已經走到我前面去了 —— 我抵達得太慢。
            float along = 0f, lateral = gap.magnitude;

            if (_dbgTargetVelRel.sqrMagnitude > 0.01f)
            {
                Vector3 vDir = _dbgTargetVelRel.normalized;
                along = Vector3.Dot(gap, vDir);
                lateral = (gap - vDir * along).magnitude;
            }

            float targetMoved = (targetPos - _dbgTargetStartPos).magnitude;

            targetPart =
                $"finalGap={gap.magnitude:F2} (沿目標移動方向={along:F2}, 側向={lateral:F2}) | " +
                $"targetSpeed={_dbgTargetVelRel.magnitude:F1} targetMoved={targetMoved:F2} | " +
                $"stopDist={_meleeDashStopDistance:F2}";
        }

        string predictPart = _dbgHasIntercept
            ? $"預測: dist={_dbgPredictedDist:F2} time={_dbgPredictedTime:F3}s (假設 {_meleeDashSpeed:F1} m/s)"
            : "預測: 無攔截解（純追蹤）";

        // 時間比：> 1 表示實際比解算慢，攔截點就會落在目標後方
        float timeRatio = (_dbgPredictedTime > 0.0001f) ? (elapsed / _dbgPredictedTime) : 0f;

        string reasonText = (reason == MeleeDashEndReason.ExternalStop)
            ? $"ExternalStop({_dbgStopSource})"
            : reason.ToString();

        Debug.Log(
            $"[MeleeDash] {reasonText} | {predictPart} | " +
            $"實際: time={elapsed:F3}s (×{timeRatio:F2}) travelled={_meleeDashTravelled:F2}/{_meleeDashDistance:F2} | " +
            $"speed peak={_dbgPeakSpeed:F1} avg={avgSpeed:F1} | {targetPart}", this);
    }

    /// <summary>
    /// 停止突進。source 只用於診斷 —— 突進被誰中止是最關鍵的除錯資訊，
    /// 因為「還沒靠近就被停掉」跟「到了但打不中」是完全不同的問題。
    /// </summary>
    public void StopMeleeDash(string source = "unknown")
    {
        _dbgStopSource = source;
        StopMeleeDashInternal(MeleeDashEndReason.ExternalStop);
    }

    private void StopMeleeDashInternal(MeleeDashEndReason reason)
    {
        LogDashDiagnostics(reason);

        // 註：這裡刻意不恢復重力。滯空要撐到整段攻擊結束（EndMeleeHover），
        //     否則突進一停人就開始掉，刀還沒揮完就離開目標高度了。
        _meleeDashing = false;
        _meleeDashTarget = null;
        _meleeDashCurve = null;
    }

    private void ApplyMeleeDashFixed(float dt)
    {
        if (!_meleeDashing) return;
        if (playerRigidbody == null) { StopMeleeDash("noRigidbody"); return; }

        Vector3 platformVel = GetMobilePlatformVelocity();
        Vector3 vRel = playerRigidbody.linearVelocity - platformVel;

        // ToTarget：每幀重算方向，並且朝「攔截點」而不是目標當前位置。
        //
        // 朝當前位置衝是純追尾 —— 你永遠跑向敵人剛剛在的地方，形成尾隨曲線，
        // 橫向移動的敵人幾乎打不到。攔截解算出「我用這個速度前進，會在哪裡跟它相遇」，
        // 效果等同子彈的提前量。
        //
        // 【方向 A】方向修正是「持續」的，但有轉向速率上限（meleeDashTurnRate）。
        // 系統只幫忙修掉高速造成的小誤差，角度偏太多還是追不上 —— 玩家仍然要
        // 負責把自己送到大致正確的位置。
        if (_meleeDashTarget != null)
        {
            Vector3 selfPos = playerRigidbody.position;
            Vector3 targetPos = _meleeDashTarget.position;

            Vector3 toTarget = targetPos - selfPos;   // 三維，不壓平

            if (toTarget.magnitude <= _meleeDashStopDistance)
            {
                StopMeleeDashInternal(MeleeDashEndReason.ReachedStopDistance);
                return;
            }

            // 目標速度也要扣掉平台速度：兩者都站在同一艘船上時，
            // 共同的船速不該被算成需要提前量的相對運動。
            Vector3 targetVelRel = _meleeDashTarget.linearVelocity - platformVel;

            Vector3 aimPoint = targetPos;

            if (TrySolveIntercept(selfPos, targetPos, toTarget, targetVelRel, out var intercept))
            {
                aimPoint = intercept;

                if (!_dbgCaptured)
                {
                    _dbgHasIntercept = true;
                    _dbgInterceptPoint = intercept;
                }
            }

            // 第一幀捕捉解算前提，供結束時比對
            if (!_dbgCaptured)
            {
                _dbgCaptured = true;
                _dbgTargetVelRel = targetVelRel;
                _dbgPredictedDist = (aimPoint - selfPos).magnitude;

                // 解算器假設的是「立刻且持續以 _meleeDashSpeed 前進」
                _dbgPredictedTime = _dbgPredictedDist / Mathf.Max(0.01f, _meleeDashSpeed);
            }

            Vector3 desiredDir = aimPoint - selfPos;

            if (desiredDir.sqrMagnitude > 0.0001f)
            {
                desiredDir.Normalize();

                // 轉向速率上限：保留「角度太離譜就打不中」的懲罰
                float maxRadians = meleeDashTurnRate * Mathf.Deg2Rad * dt;
                _meleeDashDir = Vector3.RotateTowards(_meleeDashDir, desiredDir, maxRadians, 0f).normalized;

                // ★ 角色朝向也要跟著轉。
                //   attackDesiredForward 是攻擊當下設定一次就不動的，只改移動方向
                //   會讓角色橫著飄、刀刃卻朝原方向揮 —— 追到了也砍不到。
                //   RotateCharacter 只處理水平朝向，所以這裡壓平。
                if (attackFacingActive)
                {
                    Vector3 flatAim = new Vector3(_meleeDashDir.x, 0f, _meleeDashDir.z);
                    if (flatAim.sqrMagnitude > 0.0001f)
                        attackDesiredForward = flatAim.normalized;
                }
            }
        }

        // 進度用「相對平台」的累積位移，不能用世界座標距離
        float vParallelRel = Vector3.Dot(vRel, _meleeDashDir);
        if (vParallelRel > 0f)
        {
            _meleeDashTravelled += vParallelRel * dt;
            if (vParallelRel > _dbgPeakSpeed) _dbgPeakSpeed = vParallelRel;
        }

        if (_meleeDashTravelled >= _meleeDashDistance)
        {
            StopMeleeDashInternal(MeleeDashEndReason.DistanceExhausted);
            return;
        }

        float progress = Mathf.Clamp01(_meleeDashTravelled / Mathf.Max(0.0001f, _meleeDashDistance));

        float factor = (_meleeDashCurve != null && _meleeDashCurve.length > 0)
            ? _meleeDashCurve.Evaluate(progress)
            : 1f;
        factor = Mathf.Max(meleeDashMinSpeedFactor, factor);

        float targetSpeed = _meleeDashSpeed * factor;

        // 重力由 ApplyMeleeHoverFixed 統一處理（滯空期間 useGravity 已關閉），
        // 這裡不要再加任何垂直補償，否則會變成向上推力。

        // 已經達到這一刻的目標速度就不再推（跟 ApplyDashFixed 同邏輯）
        if (vParallelRel >= targetSpeed) return;

        playerRigidbody.AddForce(_meleeDashDir * meleeDashAccel, ForceMode.Acceleration);
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

        // 俯仰角：只在近戰攻擊期間朝目標抬頭 / 低頭，其餘時間平滑回 0。
        // 注意膠囊碰撞體不會跟著轉 —— 視覺斜著、碰撞維持直立是標準作法。
        UpdateMeleePitch();

        if (desiredForward.HasValue)
        {
            // 用 Quaternion 而不是直接寫 forward：forward 只能表達方向，
            // 要把 pitch 疊在 yaw 之上必須走旋轉組合。
            Quaternion yawRot = Quaternion.LookRotation(desiredForward.Value, Vector3.up);
            Quaternion targetRot = yawRot * Quaternion.Euler(_meleePitchCurrent, 0f, 0f);

            characterModel.rotation = Quaternion.Slerp(
                characterModel.rotation,
                targetRot,
                Time.deltaTime * attackRotateSpeed
            );
        }
        else if (Mathf.Abs(_meleePitchCurrent) > 0.01f)
        {
            // 沒有轉向需求但 pitch 還沒歸零時，維持當前 yaw 只轉 pitch
            Vector3 flatForward = characterModel.forward;
            flatForward.y = 0f;

            if (flatForward.sqrMagnitude > 0.001f)
            {
                Quaternion yawRot = Quaternion.LookRotation(flatForward.normalized, Vector3.up);
                Quaternion targetRot = yawRot * Quaternion.Euler(_meleePitchCurrent, 0f, 0f);

                characterModel.rotation = Quaternion.Slerp(
                    characterModel.rotation,
                    targetRot,
                    Time.deltaTime * attackRotateSpeed
                );
            }
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

        float targetMagnitude = Mathf.Clamp01(new Vector2(targetX, targetY).magnitude);
        if (targetMagnitude < 0.001f)
        {
            targetX = 0f;
            targetY = 0f;
        }

        animX = Mathf.MoveTowards(animX, targetX, movementBlendSpeed * Time.deltaTime);
        animY = Mathf.MoveTowards(animY, targetY, movementBlendSpeed * Time.deltaTime);

        playerAnimation.SetMovementParameters(animX, animY);
    }
    private void StartAimHold(float seconds)
    {
        if (seconds <= 0f) return;
        _aimHoldUntil = Mathf.Max(_aimHoldUntil, Time.time + seconds);
    }

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

    public bool IsFlyingActive => !grounded && Time.time <= flyRequestUntil;

    public bool IsDashActive => Time.time <= dashActiveUntil;
    public bool IsDashAttackBlocked => Time.time <= dashAttackLockUntil;

    // 玩家相對「腳下平台」的水平速度。沒站平台 => 等於世界速度；站在移動的船上不動 => ≈0

    public float HorizontalSpeedRelativeToPlatform
    {
        get
        {
            if (playerRigidbody == null) return 0f;
            Vector3 vRel = playerRigidbody.linearVelocity - GetMobilePlatformVelocity();
            vRel.y = 0f;
            return vRel.magnitude;
        }
    }

    public float VerticalVelocity
    {
        get
        {
            if (playerRigidbody == null) return 0f;
            return playerRigidbody.linearVelocity.y;
        }
    }

    // Mobile Platform carry (velocity-based)
    private bool _onMobilePlatform = false;
    private Rigidbody _mobilePlatformRb = null;
    private Transform _mobilePlatformTf;
    private Quaternion _mobilePlatformLastRot;
    private bool _mobilePlatformRotInit;
    // 對外公開：玩家目前站著的移動平台（沒站平台 = null）。給殘影 parent 用。
    public Transform CurrentPlatform => _onMobilePlatform ? _mobilePlatformTf : null;
    public void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Mobile Platform")) return;

        var rb = other.GetComponentInParent<Rigidbody>();
        if (rb == null) return;

        if (_mobilePlatformRb != rb)
        {
            _mobilePlatformRb = rb;
            _mobilePlatformTf = rb.transform;

            _mobilePlatformLastRot = rb.rotation;
            _mobilePlatformRotInit = true;
        }

        _onMobilePlatform = true;
        playerAnimation.SetSimulationSpaceLandship();
        attackManager.SetOnShip(rb);

        if (PlayerAiming.Instance != null)
            PlayerAiming.Instance.SetPlatform(_mobilePlatformTf);   // 讓鏡頭跟船的朝向
    }
    public void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Mobile Platform")) return;

        _onMobilePlatform = false;
        _mobilePlatformRb = null;
        _mobilePlatformTf = null;
        _mobilePlatformRotInit = false;
        playerAnimation.SetSimulationSpaceWorld();
        attackManager.SetOffShip();

        if (PlayerAiming.Instance != null)
            PlayerAiming.Instance.SetPlatform(null);
    }

    private Vector3 GetMobilePlatformVelocity()
    {
        if (!_onMobilePlatform || _mobilePlatformRb == null) return Vector3.zero;
        return _mobilePlatformRb.linearVelocity; // Unity 6
    }
    private void CarryWithPlatformRotation(float dt)
    {
        if (!_onMobilePlatform || _mobilePlatformRb == null || _mobilePlatformTf == null) return;
        if (dt <= 0f) return;

        Quaternion current = _mobilePlatformRb.rotation;

        if (!_mobilePlatformRotInit)
        {
            _mobilePlatformLastRot = current;
            _mobilePlatformRotInit = true;
            return;
        }

        Quaternion delta = current * Quaternion.Inverse(_mobilePlatformLastRot);

        // 讓玩家的位置繞著平台 pivot 旋轉
        Vector3 pivot = _mobilePlatformTf.position;
        Vector3 p = playerRigidbody.position;
        Vector3 newP = pivot + (delta * (p - pivot));
        playerRigidbody.MovePosition(newP);

        // 讓角色模型也吃到同一個 yaw 旋轉
        // 這一步可以避開你 RotateCharacter() 對模型朝向的覆寫感
        Vector3 axis; float angle;
        delta.ToAngleAxis(out angle, out axis);
        if (axis.sqrMagnitude > 0.000001f)
        {
            axis.Normalize();
            float yaw = Vector3.Dot(axis, Vector3.up) * angle;
            if (Mathf.Abs(yaw) > 0.0001f)
            {
                characterModel.Rotate(0f, yaw, 0f, Space.World);

            }
        }

        _mobilePlatformLastRot = current;
    }
    private void HandleDustEffect()
    {
        // 用相對平台速度：站在移動的船上不動 => ≈0 => 不噴
        float speed = HorizontalSpeedRelativeToPlatform;

        if (!grounded || speed < dustMinSpeed) return;

        if (Time.time >= _nextDustTime)
        {
            float t = Mathf.InverseLerp(dustMinSpeed, dustMaxSpeed, speed); // 0~1，自動 clamp
            float interval = Mathf.Lerp(dustIntervalSlow, dustIntervalFast, t);
            _nextDustTime = Time.time + interval;
            if (!_onMobilePlatform)
            {
                playerAnimation.DustEffect();
            }
            else
            {
                playerAnimation.DustEffect_OnShip();
            }
        }
    }
}