using UnityEngine;
using UnityEngine.EventSystems;

public class EnemyMovement : MonoBehaviour
{
    private EnemyStats stats;
    [SerializeField] private Vector3 moveDirection = new Vector3(0, 0, 0);

    [Header("Rigidbody")]
    [SerializeField] private Rigidbody enemyRigidbody;

    [Header("Rotation")]
    [SerializeField] private Transform rotateRoot;          // 想旋轉的物件（通常是敵人根物件 transform，或 WolfMesh）
    [SerializeField] private float turnSpeedDeg = 720f;     // 每秒旋轉角度（度/秒）

    [Header("Turn Deceleration")]
    [Tooltip("當面向與移動方向夾角大於此角度時，開始減速（用 decelerationSpeed）")]
    [SerializeField] private float turnSlowStartAngle = 45f;

    [Tooltip("夾角到達此角度時，減速到最慢（速度倍率 = minTurnSpeedFactor）")]
    [SerializeField] private float turnSlowFullAngle = 180f;

    [Tooltip("大轉向時最低速度倍率（0.2 = 20% speed）")]
    [Range(0f, 1f)]
    [SerializeField] private float minTurnSpeedFactor = 0.2f;

    void Awake()
    {
        stats = GetComponent<EnemyStats>();
        if (rotateRoot == null) rotateRoot = transform;
    }

    public void FixedUpdate()
    {
        ApplyHorizontalMovementFixed(Time.fixedDeltaTime);
        RotateToMoveDirectionFixed(Time.fixedDeltaTime);
    }

    public void HorizontalMovement(float moveX, float moveZ)
    {
        // 只更新 moveDirection（給 RotateCharacter / 動畫用），不要在 Update 內碰剛體
        Vector3 forward = enemyRigidbody.transform.forward;
        forward.y = 0f;

        Vector3 right = enemyRigidbody.transform.right;
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

    private void ApplyHorizontalMovementFixed(float dt)
    {
        if (stats == null || enemyRigidbody == null) return;

        Vector3 v = enemyRigidbody.linearVelocity;
        Vector3 horizontalVel = new Vector3(v.x, 0f, v.z);

        // 1) 無輸入：照你原本邏輯用 decelerationSpeed 歸零
        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            horizontalVel = Vector3.MoveTowards(horizontalVel, Vector3.zero, stats.decelerationSpeed * dt);
            enemyRigidbody.linearVelocity = new Vector3(horizontalVel.x, v.y, horizontalVel.z);
            return;
        }

        // 2) 有輸入：計 target 水平速度，但如果轉向角太大，先減速（用 decelerationSpeed）
        Vector3 desiredDir = moveDirection;
        desiredDir.y = 0f;
        if (desiredDir.sqrMagnitude > 0.0001f) desiredDir.Normalize();

        // 用 rotateRoot（或剛體）作為「目前面向」
        Vector3 facing = (rotateRoot != null) ? rotateRoot.forward : enemyRigidbody.transform.forward;
        facing.y = 0f;
        if (facing.sqrMagnitude > 0.0001f) facing.Normalize();

        float angle = 0f;
        if (facing.sqrMagnitude > 0.0001f && desiredDir.sqrMagnitude > 0.0001f)
            angle = Vector3.Angle(facing, desiredDir);

        // 角度映射：45° -> 1.0 speed；到 turnSlowFullAngle -> minTurnSpeedFactor
        float t = 0f;
        if (angle > turnSlowStartAngle)
            t = Mathf.InverseLerp(turnSlowStartAngle, Mathf.Max(turnSlowStartAngle + 0.01f, turnSlowFullAngle), angle);

        float speedFactor = Mathf.Lerp(1f, minTurnSpeedFactor, t);
        Vector3 targetHorizontalVel = desiredDir * (stats.speed * speedFactor);

        // ✅ 關鍵：大轉向時用 decelerationSpeed 拉低速度，角度細先用 accelerationSpeed
        float rate = (angle > turnSlowStartAngle) ? stats.decelerationSpeed : stats.accelerationSpeed;

        horizontalVel = Vector3.MoveTowards(horizontalVel, targetHorizontalVel, rate * dt);
        enemyRigidbody.linearVelocity = new Vector3(horizontalVel.x, v.y, horizontalVel.z);
    }

    public void SetWorldMoveDirection(Vector3 worldDir)
    {
        worldDir.y = 0f;

        if (worldDir.sqrMagnitude < 0.0001f)
        {
            moveDirection = Vector3.zero;
            return;
        }

        moveDirection = worldDir.normalized; // 直接用世界方向，AI 最穩
    }

    private void RotateToMoveDirectionFixed(float dt)
    {
        // 沒有移動輸入就不轉（避免停下來還亂轉）
        if (moveDirection.sqrMagnitude < 0.0001f) return;

        Vector3 dir = moveDirection;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;
        dir.Normalize();

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        Quaternion newRot = Quaternion.RotateTowards(enemyRigidbody.rotation, targetRot, turnSpeedDeg * dt);

        enemyRigidbody.MoveRotation(newRot);
    }
}
