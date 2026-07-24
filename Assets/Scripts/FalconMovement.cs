using UnityEngine;
using UnityEngine.EventSystems;

public class FalconMovement : ModularEntityMovement
{
    [Header("Flight")]
    [SerializeField] private float hoverHeight = 10f;
    [SerializeField] private float cruiseSpeedRatio = 1f;   // 巡航速度 = sprintSpeed × 此比例
    [SerializeField] private float damping = 8f;
    [SerializeField] private float heightTolerance = 0.02f;
    [SerializeField] private float hoverRaycastDistance = 100f;

    public override void FixedUpdate()
    {
        base.FixedUpdate();              // GroundCheck + ApplyHorizontalMovementFixed（下面已 override）
        VerticalMovement(hoverHeight);   // 飛行體永遠維持高度 → 不需要 brain 每個狀態各叫一次
    }

    // 永遠沿機首前進：完全忽略 moveDirection 的方向與大小
    protected override void ApplyHorizontalMovementFixed(float dt)
    {
        Vector3 platformVel = GetMobilePlatformVelocity();
        Vector3 vRel = entityRigidbody.linearVelocity - platformVel;

        // 目標速度方向 = 機首方向，不是 brain 給的方向
        Vector3 targetHorizontalRel = MeshForward * (modularEntityStats.sprintSpeed * cruiseSpeedRatio);

        Vector3 horizontalRel = new Vector3(vRel.x, 0f, vRel.z);
        horizontalRel = Vector3.MoveTowards(horizontalRel, targetHorizontalRel,
                                            modularEntityStats.accelerationSpeed * dt);

        entityRigidbody.linearVelocity = new Vector3(horizontalRel.x, vRel.y, horizontalRel.z) + platformVel;
    }

    public void VerticalMovement(float targetHeight)
    {
        // 往下找真正的地面（groundPoint 是隼自己的腳，不能當基準）
        if (!Physics.Raycast(groundPoint.position, Vector3.down, out RaycastHit hit,
                             hoverRaycastDistance, whatIsGround))
            return;

        float heightError = (hit.point.y + targetHeight) - groundPoint.position.y;

        float a = heightError * modularEntityStats.accelerationSpeed;
        a -= entityRigidbody.linearVelocity.y * damping;
        a = Mathf.Clamp(a, -modularEntityStats.accelerationSpeed, modularEntityStats.accelerationSpeed);

        entityRigidbody.AddForce(Vector3.up * a, ForceMode.Acceleration);

        if (Mathf.Abs(heightError) < heightTolerance && Mathf.Abs(entityRigidbody.linearVelocity.y) < 0.05f)
        {
            Vector3 v = entityRigidbody.linearVelocity;
            v.y = 0f;
            entityRigidbody.linearVelocity = v;
        }
    }
}