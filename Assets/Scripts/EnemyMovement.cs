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
                                                            // Start is called once before the first execution of Update after the MonoBehaviour is created
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
        Vector3 v = enemyRigidbody.linearVelocity;
        Vector3 horizontalVel = new Vector3(v.x, 0f, v.z);
        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            horizontalVel = Vector3.MoveTowards(horizontalVel, Vector3.zero, stats.decelerationSpeed * dt);
            enemyRigidbody.linearVelocity = new Vector3(horizontalVel.x, v.y, horizontalVel.z);
            return;
        }
        // 有輸入：用 acceleration 推向目標水平速度（同樣只處理 x/z）
        Vector3 targetHorizontalVel = moveDirection * stats.speed;
        horizontalVel = Vector3.MoveTowards(horizontalVel, targetHorizontalVel, stats.accelerationSpeed * dt);

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
