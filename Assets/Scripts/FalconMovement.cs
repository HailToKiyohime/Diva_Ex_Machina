using UnityEngine;

public class FalconMovement : ModularEntityMovement
{
    [Header("Hover")]
    [SerializeField] private float damping = 8f;
    [SerializeField] private float heightTolerance = 0.02f;
    [SerializeField] private float hoverRaycastDistance = 100f;

    // 懸停 raycast 的結果（brain 若需要「腳下地面」的資訊可以直接讀，不用再打一次射線）
    public bool HasGroundBelow { get; private set; }
    public Vector3 GroundBelowPoint { get; private set; }

    // ★ 不在 FixedUpdate 呼叫 VerticalMovement —— 改由 brain 的每個 state 各自呼叫，
    //   因為每個 state 的飛行高度不同。base.FixedUpdate 仍負責 GroundCheck + 水平移動。

    /// <summary>
    /// 維持在「腳下地面 + targetHeight」的高度。由 brain 的各個 state behaviour 呼叫。
    /// </summary>
    public void VerticalMovement(float targetHeight)
    {
        // 往下找真正的地面：groundPoint 是隼自己的腳，會跟著隼飛，不能當高度基準
        HasGroundBelow = Physics.Raycast(groundPoint.position, Vector3.down, out RaycastHit hit,
                                         hoverRaycastDistance, whatIsGround);
        if (!HasGroundBelow) return;   // 下方沒地面（飛出地圖 / 峽谷）→ 這幀不施力，自由落下

        GroundBelowPoint = hit.point;

        float realTargetHeight = hit.point.y + targetHeight;
        float heightError = realTargetHeight - groundPoint.position.y;

        float verticalAcceleration = heightError * modularEntityStats.accelerationSpeed;
        verticalAcceleration -= entityRigidbody.linearVelocity.y * damping;

        verticalAcceleration = Mathf.Clamp(
            verticalAcceleration,
            -modularEntityStats.accelerationSpeed,
            modularEntityStats.accelerationSpeed
        );

        entityRigidbody.AddForce(Vector3.up * verticalAcceleration, ForceMode.Acceleration);

        // 已經到位就把殘餘垂直速度歸零，避免無止盡的微幅上下抖動
        if (Mathf.Abs(heightError) < heightTolerance &&
            Mathf.Abs(entityRigidbody.linearVelocity.y) < 0.05f)
        {
            Vector3 velocity = entityRigidbody.linearVelocity;
            velocity.y = 0f;
            entityRigidbody.linearVelocity = velocity;
        }
    }

    /// <summary>
    /// 與 base 唯一的差別：減速時用 decelerationSpeed，加速時才用 accelerationSpeed。
    /// brain 靠 throttle（接近航點就變小）降低目標速度，這裡負責「真的煞得下來」，
    /// 速度低了迴轉半徑才小（r = v / ω），轉彎才靈活。
    /// moveDirection 為零時目標速度為零 → 一樣走減速路徑 → 停下來懸停。
    /// </summary>
    protected override void ApplyHorizontalMovementFixed(float dt)
    {
        Vector3 platformVel = GetMobilePlatformVelocity();
        Vector3 vRel = entityRigidbody.linearVelocity - platformVel;

        Vector3 horizontalRel = new Vector3(vRel.x, 0f, vRel.z);
        Vector3 targetHorizontalRel = moveDirection * modularEntityStats.sprintSpeed;

        float rate = (horizontalRel.magnitude > targetHorizontalRel.magnitude)
            ? modularEntityStats.decelerationSpeed
            : modularEntityStats.accelerationSpeed;

        horizontalRel = Vector3.MoveTowards(horizontalRel, targetHorizontalRel, rate * dt);

        entityRigidbody.linearVelocity = new Vector3(horizontalRel.x, vRel.y, horizontalRel.z) + platformVel;
    }


}