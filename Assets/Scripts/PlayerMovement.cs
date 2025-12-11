using SadnessMonday.BetterPhysics;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Character orientation")]
    //[SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform characterOrientation;
    [SerializeField] private Transform characterModel;
    [Header("Rigidbody")]
    [SerializeField] private BetterRigidbody playerRigidbody;
    [Header("Movement Settings")]
    [SerializeField] private bool grounded;
    [SerializeField] private bool readyToJump;
    [SerializeField] private float jumpCooldown = 0.2f;
    [SerializeField] private bool canRegenerateEnergy;
    [SerializeField] private Vector3 moveDirection = new Vector3(0, 0, 0);
    [SerializeField] private float horizontalDeceleration = 25f;

    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private Transform groundPoint;
    [SerializeField] private LayerMask whatIsGround;
    [Header("Animation")]
    [SerializeField] private PlayerAnimation playerAnimation;
    [SerializeField] private float movementBlendSpeed = 3f; // 控制 0↔1 的快慢
    private float movementBlend = 0f;                      // 目前的動畫參數值
    private float animX = 0f; // 水平（左右）動畫輸入
    private float animY = 0f; // 垂直（前後）動畫輸入
    // Update is called once per frame
    public void Update()
    {
        EnergyRegenerationCheck();
        RotateCharacter();
    }
    public void FixedUpdate()
    {
        GroundCheck();
    }
    public void HorizontalMovement(float moveX, float moveZ)
    {
        // 1) 取得水平 forward/right（把 y 設成 0 再正規化）
        Vector3 forward = characterOrientation.forward;
        forward.y = 0f;
        forward = forward.normalized;

        Vector3 right = characterOrientation.right;
        right.y = 0f;
        right = right.normalized;

        // 2) 用「壓扁後」的方向算移動
        moveDirection = forward * moveZ + right * moveX;

        // === 新增：沒有輸入時，強力減速 ===
        if (moveDirection.sqrMagnitude < 0.0001f)
        {
            // 取出目前速度
            Vector3 v = playerRigidbody.velocity;

            // 只處理水平速度（x,z），y 保留給重力 & 跳躍
            Vector3 horizontalVel = new Vector3(v.x, 0f, v.z);

            if (horizontalVel.sqrMagnitude > 0.0001f)
            {
                // 用 MoveTowards 把水平速度快速拉向 0
                horizontalVel = Vector3.MoveTowards(
                    horizontalVel,
                    Vector3.zero,
                    horizontalDeceleration * Time.deltaTime
                );

                // 寫回剛體速度
                playerRigidbody.velocity = new Vector3(horizontalVel.x, v.y, horizontalVel.z);
            }

            // 沒有輸入就不再做加速
            return;
        }


        moveDirection = moveDirection.normalized;

        Vector3 targetVelocity = moveDirection * PlayerStats.Instance.sprintSpeed;

        // 3) 只對水平速度做修正
        Vector3 currentVelocity = playerRigidbody.velocity;
        Vector3 currentHorizontalVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);

        Vector3 deltaHorizontalVelocity = targetVelocity - currentHorizontalVelocity;


        playerRigidbody.AddForce(deltaHorizontalVelocity, ForceMode.Acceleration);
    }
    public bool JumpAction()
    {
        if(grounded && readyToJump)
        {
            readyToJump = false;
            playerRigidbody.velocity = new Vector3(playerRigidbody.velocity.x, 0, playerRigidbody.velocity.z);
            playerRigidbody.AddForceWithoutLimit(Vector3.up * convertJumpHeightToForce(PlayerStats.Instance.jumpHeight), ForceMode.Impulse);
            Invoke("ResetJump", jumpCooldown);
            return true;
        }else if(!grounded && readyToJump && PlayerStats.Instance.currentEnergy > 0)
        {
            FlyAction();
        }
        return false;
    }

    public void FlyAction()
    {
        CancelInvoke("ResetEnergyRegenerate");
        canRegenerateEnergy = false;
        playerRigidbody.AddForceWithoutLimit(Vector3.up * PlayerStats.Instance.flyForce, ForceMode.Force);
        PlayerStats.Instance.currentEnergy = PlayerStats.Instance.currentEnergy - Time.deltaTime * PlayerStats.Instance.flyEnergyCost;
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
        else if (PlayerStats.Instance.currentEnergy < PlayerStats.Instance.maxEnergy && !canRegenerateEnergy && PlayerStats.Instance.currentEnergy == 0)
        {
            Invoke("ResetEnergyRegenerate", 3);
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
        float castDistance = groundCheckDistance + playerRigidbody.velocity.y * Time.fixedDeltaTime;
        RaycastHit hit;
        bool didHit = Physics.Raycast(groundPoint.position,
            Vector3.down,
            out hit,
            castDistance,
            whatIsGround
        );
        grounded = didHit;
    }
    private void ResetJump()
    {
        readyToJump = true;
    }
    private void RotateCharacter()
    {
        // 先處理角色朝向
        if (PlayerAiming.Instance.lockOn)
        {
            Vector3 lookDirection = PlayerAiming.Instance.aimingPoint.position - characterModel.position;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Vector3 targetForward = lookDirection.normalized;
                characterModel.forward = Vector3.Slerp(
                    characterModel.forward,
                    targetForward,
                    Time.deltaTime * 10f
                );
            }
        }
        else
        {
            if (moveDirection.sqrMagnitude > 0.001f)
            {
                Vector3 faceDirection = moveDirection.normalized;
                Vector3 targetForward = new Vector3(faceDirection.x, 0, faceDirection.z);
                characterModel.forward = Vector3.Slerp(
                    characterModel.forward,
                    targetForward,
                    Time.deltaTime * 10f
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
}
