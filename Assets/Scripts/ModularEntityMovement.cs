using UnityEngine;
using UnityEngine.EventSystems;

public class ModularEntityMovement : MonoBehaviour
{
    [Header("Rigidbody")]
    [SerializeField] private Rigidbody entityRigidbody;
    [Header("Entity Stats")]
    [SerializeField] private ModularEntityStats modularEntityStats;

    [SerializeField] private Transform entityOrientation;
    private Vector3 moveDirection = new Vector3(0, 0, 0);

    [Header("Jump Setting")]
    [SerializeField] private Transform groundPoint;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private bool grounded;
    [SerializeField] private bool readyToJump;
    private float jumpCooldown = 0.25f;
    private float groundCheckDistance = 0.1f;

    public void FixedUpdate()
    {
        GroundCheck();
        ApplyHorizontalMovementFixed(Time.fixedDeltaTime);
    }

    private void ApplyHorizontalMovementFixed(float dt)
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

    public void HorizontalMovement(float moveX, float moveZ)
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
        else
            moveDirection.Normalize();
    }
    public void VerticalMovement()
    {
        if (grounded && readyToJump)
        {
            readyToJump = false;
            entityRigidbody.linearVelocity = new Vector3(entityRigidbody.linearVelocity.x, 0, entityRigidbody.linearVelocity.z);
            entityRigidbody.AddForce(Vector3.up * convertJumpHeightToForce(modularEntityStats.jumpHeight), ForceMode.Impulse);
            Invoke("ResetJump", jumpCooldown);
        }
    }

    public float convertJumpHeightToForce(float jumpHeight)
    {
        float g = Mathf.Abs(Physics.gravity.y);
        float v0 = Mathf.Sqrt(2f * g * jumpHeight);
        float mass = entityRigidbody.mass;
        float impulseMagnitude = mass * v0;

        return impulseMagnitude;
    }
    private void ResetJump()
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

    // Mobile Platform carry (velocity-based)
    private bool _onMobilePlatform = false;
    private Rigidbody _mobilePlatformRb = null;
    private Transform _mobilePlatformTf;
    private Quaternion _mobilePlatformLastRot;
    private bool _mobilePlatformRotInit;

    private Vector3 GetMobilePlatformVelocity()
    {
        if (!_onMobilePlatform || _mobilePlatformRb == null) return Vector3.zero;
        return _mobilePlatformRb.linearVelocity; // Unity 6
    }

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
    }
}
