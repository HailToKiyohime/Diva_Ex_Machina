using UnityEngine;
using UnityEngine.EventSystems;

public class ModularEntityMovement : MonoBehaviour
{
    [Header("Rigidbody")]
    [SerializeField] protected Rigidbody entityRigidbody;
    [Header("Entity Stats")]
    [SerializeField] protected ModularEntityStats modularEntityStats;

    [SerializeField] protected Transform entityOrientation;
    [SerializeField] protected Transform entityMesh;
    protected Vector3 moveDirection = new Vector3(0, 0, 0);

    [Header("Jump Setting")]
    [SerializeField] protected Transform groundPoint;
    [SerializeField] protected LayerMask whatIsGround;
    [SerializeField] protected bool grounded;
    [SerializeField] protected bool readyToJump;
    protected float jumpCooldown = 0.25f;
    protected float groundCheckDistance = 0.1f;

    public virtual void FixedUpdate()
    {
        GroundCheck();
        ApplyHorizontalMovementFixed(Time.fixedDeltaTime);
    }

    protected virtual void ApplyHorizontalMovementFixed(float dt)
    {

        Vector3 platformVel = GetMobilePlatformVelocity();

        // 用「相對平台」速度來做加速/減速
        Vector3 vWorld = entityRigidbody.linearVelocity;
        Vector3 vRel = vWorld - platformVel;

        Vector3 horizontalRel = new Vector3(vRel.x, 0f, vRel.z);

        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            horizontalRel = Vector3.MoveTowards(horizontalRel, Vector3.zero, modularEntityStats.decelerationSpeed * dt);
            Vector3 outRel = new Vector3(horizontalRel.x, vRel.y, horizontalRel.z);
            entityRigidbody.linearVelocity = outRel + platformVel;   // ✅ 設定一次，不累加
            return;
        }

        Vector3 targetHorizontalRel = moveDirection * modularEntityStats.sprintSpeed;
        horizontalRel = Vector3.MoveTowards(horizontalRel, targetHorizontalRel, modularEntityStats.accelerationSpeed * dt);

        Vector3 outRel2 = new Vector3(horizontalRel.x, vRel.y, horizontalRel.z);
        entityRigidbody.linearVelocity = outRel2 + platformVel;      // ✅ 設定一次，不累加
    }

    public virtual void HorizontalMovement(float moveX, float moveZ)
    {
        Vector3 forward = entityOrientation.forward;
        forward.y = 0f;

        Vector3 right = entityOrientation.right;
        right.y = 0f;

        if (forward.sqrMagnitude > 0.0001f) forward.Normalize();
        if (right.sqrMagnitude > 0.0001f) right.Normalize();

        moveDirection = forward * moveZ + right * moveX;

        if (moveDirection.sqrMagnitude < 0.0001f)
            moveDirection = Vector3.zero;
        else if (moveDirection.sqrMagnitude > 1f)
            moveDirection.Normalize();
    }
    protected virtual void VerticalMovement()
    {
        if (grounded && readyToJump)
        {
            readyToJump = false;
            entityRigidbody.linearVelocity = new Vector3(entityRigidbody.linearVelocity.x, 0, entityRigidbody.linearVelocity.z);
            entityRigidbody.AddForce(Vector3.up * convertJumpHeightToForce(modularEntityStats.jumpHeight), ForceMode.Impulse);
            Invoke("ResetJump", jumpCooldown);
        }
    }

    protected virtual float convertJumpHeightToForce(float jumpHeight)
    {
        float g = Mathf.Abs(Physics.gravity.y);
        float v0 = Mathf.Sqrt(2f * g * jumpHeight);
        float mass = entityRigidbody.mass;
        float impulseMagnitude = mass * v0;

        return impulseMagnitude;
    }
    public virtual void ResetJump()
    {
        readyToJump = true;
    }

    public void GroundCheck()
    {
        float castDistance = groundCheckDistance + entityRigidbody.linearVelocity.y * Time.fixedDeltaTime;
        RaycastHit hit;
        bool didHit = Physics.Raycast(groundPoint.position,
            Vector3.down,
            out hit,
            castDistance,
            whatIsGround
        );
        grounded = didHit;
    }

    // ── Mobile Platform carry (velocity-based) ────────────────────────────
    //
    // 原本這裡有一組自己的 _onMobilePlatform / _mobilePlatformRb，靠本檔的
    // OnTriggerStay 設成 true —— 但沒有 OnTriggerExit，所以它永遠不會變回 false。
    // 實體踏上船一次之後，就算走回地面、走到地圖另一頭，ApplyHorizontalMovementFixed
    // 還是會一直把船速加進去，表現成「被一股看不見的力往船的方向拖著跑」。
    //
    // 現在改成讀 ShipPassenger。專案裡本來就有兩套獨立的「在不在船上」狀態
    // （這一套給移動速度用，ShipPassenger 那套給 PathFinder 用），
    // 收斂成一個真相來源就不會再有兩邊不一致的問題。
    [Header("Mobile Platform")]
    [Tooltip("留空會自動在自己或父物件上找。ShipPassenger 必須跟實體的 collider 在同一個 GameObject 上，才收得到 trigger 訊息。")]
    [SerializeField] protected ShipPassenger shipPassenger;
    private bool _passengerResolved;

    /// <summary>
    /// 延遲解析，不用 Awake —— 子類別（例如 FalconMovement）如果自己宣告了 Awake，
    /// 會蓋掉基底的 Awake，那樣 shipPassenger 就永遠是 null 而且很難查。
    /// </summary>
    protected ShipPassenger Passenger
    {
        get
        {
            if (!_passengerResolved)
            {
                _passengerResolved = true;
                if (shipPassenger == null) shipPassenger = GetComponent<ShipPassenger>();
                if (shipPassenger == null) shipPassenger = GetComponentInParent<ShipPassenger>();
            }
            return shipPassenger;
        }
    }

    protected virtual Vector3 GetMobilePlatformVelocity()
    {
        ShipPassenger p = Passenger;
        if (p == null || !p.isOnShip) return Vector3.zero;

        Rigidbody platformRb = p.PlatformRigidbody;
        return (platformRb != null) ? platformRb.linearVelocity : Vector3.zero;   // Unity 6
    }

    public virtual void RotateMesh(float direction, float maxDegrees = -1f)
    {
        if (direction == 0f) return;
        float step = modularEntityStats.rotationSpeed * Time.fixedDeltaTime;
        if (maxDegrees >= 0f) step = Mathf.Min(step, maxDegrees);   // 不超過剩餘角度 → 不過頭
        entityMesh.Rotate(Vector3.up, direction * step);
    }

    // OnTriggerStay 已移除 —— 平台偵測統一由 ShipPassenger 負責。
    // 舊版本在這裡每個 physics step 對每個重疊 collider 呼叫一次
    // GetComponentInParent<Rigidbody>()，那本身也是不必要的開銷。

    public Vector3 MeshForward
    {
        get
        {
            Vector3 f = entityMesh.forward;
            f.y = 0f;
            return f.sqrMagnitude > 0.0001f ? f.normalized : entityMesh.forward;
        }
    }

    public Transform GetGroundPoint()
    {
        return groundPoint;
    }
}