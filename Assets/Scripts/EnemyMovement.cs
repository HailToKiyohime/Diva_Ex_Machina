using UnityEngine;
using UnityEngine.EventSystems;

public class EnemyMovement : MonoBehaviour
{
    /*
    private EnemyStats stats;
    [SerializeField] private Vector3 moveDirection = new Vector3(0, 0, 0);

    [Header("Rigidbody")]
    [SerializeField] private Rigidbody enemyRigidbody;

    [Header("Rotation")]
    [SerializeField] private Transform rotateRoot;          // 想旋轉的物件（通常是敵人根物件 transform，或 WolfMesh）
    [SerializeField] private float turnSpeedDeg = 720f;     // 每秒旋轉角度（度/秒）


    // ============================
    // Mobile Platform carry (same as PlayerMovement)
    // ============================
    [Header("Mobile Platform Carry")]
    [SerializeField] private string mobilePlatformTag = "Mobile Platform";
    private bool _onMobilePlatform = false;
    private Rigidbody _mobilePlatformRb = null;

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

        Vector3 platformVel = GetMobilePlatformVelocity();

        // ✅ 用「相對平台」速度來做加速/減速
        Vector3 vWorld = enemyRigidbody.linearVelocity;
        Vector3 vRel = vWorld - platformVel;

        Vector3 horizontalRel = new Vector3(vRel.x, 0f, vRel.z);

        // 1) 無輸入：相對速度歸零（平台速度仍會保留）
        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            horizontalRel = Vector3.MoveTowards(horizontalRel, Vector3.zero, stats.decelerationSpeed * dt);
            Vector3 outRel = new Vector3(horizontalRel.x, vRel.y, horizontalRel.z);
            enemyRigidbody.linearVelocity = outRel + platformVel;   // ✅ 加回平台速度
            return;
        }

        // 2) 有輸入：計 target 相對水平速度，轉向大時減速
        Vector3 desiredDir = moveDirection;
        desiredDir.y = 0f;
        if (desiredDir.sqrMagnitude > 0.0001f) desiredDir.Normalize();

        // 夾角（用 rotateRoot forward）
        Vector3 forward = rotateRoot != null ? rotateRoot.forward : transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude > 0.0001f) forward.Normalize();

        float angle = Vector3.Angle(forward, desiredDir);

        Vector3 targetHorizontalRel = desiredDir * stats.speed;

        // ✅ 關鍵：大轉向時用 decelerationSpeed 拉低速度，角度小用 accelerationSpeed
        float rate = stats.accelerationSpeed;

        horizontalRel = Vector3.MoveTowards(horizontalRel, targetHorizontalRel, rate * dt);

        Vector3 outRel2 = new Vector3(horizontalRel.x, vRel.y, horizontalRel.z);
        enemyRigidbody.linearVelocity = outRel2 + platformVel;      // ✅ 加回平台速度
        Debug.Log("platformVel"+platformVel);
    }

    public void SetWorldMoveDirection(Vector3 worldDir)
    {
        worldDir.y = 0f;

        if (worldDir.sqrMagnitude < 0.0001f)
            moveDirection = Vector3.zero;
        else
            moveDirection = worldDir.normalized;
    }

    private void RotateToMoveDirectionFixed(float dt)
    {
        if (rotateRoot == null) return;

        Vector3 dir = moveDirection;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        rotateRoot.rotation = Quaternion.RotateTowards(rotateRoot.rotation, targetRot, turnSpeedDeg * dt);
    }
    // ============================
    // Trigger: detect platform rb (same idea as PlayerMovement)
    // ============================
    public void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(mobilePlatformTag))
        {
            _mobilePlatformRb = other.GetComponentInParent<Rigidbody>();
            _onMobilePlatform = (_mobilePlatformRb != null);
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(mobilePlatformTag))
        {
            _onMobilePlatform = false;
            _mobilePlatformRb = null;
            gameObject.GetComponent<CreatePath>().ClearShipNav(); // 這裡順便通知 CreatePath 離開船了（如果有的話）
        }
    }

    private Vector3 GetMobilePlatformVelocity()
    {
        if (!_onMobilePlatform || _mobilePlatformRb == null) return Vector3.zero;
        return _mobilePlatformRb.linearVelocity; // Unity 6
    }*/
}
