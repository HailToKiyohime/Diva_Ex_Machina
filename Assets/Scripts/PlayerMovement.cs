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

    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private Transform groundPoint;
    [SerializeField] private LayerMask whatIsGround;
    [Header("Animation")]
    [SerializeField] private PlayerAnimation playerAnimation;
    [SerializeField] private float movementBlendSpeed = 3f; // 控制 0↔1 的快慢
    private float movementBlend = 0f;                      // 目前的動畫參數值
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

        if (moveDirection.sqrMagnitude < 0.0001f)
            return;

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
        Vector3 targetForward = Vector3.zero;
        float targetBlend = 0f;
        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Vector3 faceDirection = moveDirection.normalized;
            targetForward = new Vector3(faceDirection.x, 0, faceDirection.z);
            characterModel.forward = Vector3.Slerp(characterModel.forward, targetForward, Time.deltaTime * 10f);

            targetBlend = 1f;
        }
        else
        {
            targetBlend = 0f;
        }
        // 讓 movementBlend 以一定速度慢慢逼近 targetBlend（0 或 1）
        movementBlend = Mathf.MoveTowards(
            movementBlend,
            targetBlend,
            movementBlendSpeed * Time.deltaTime
        );
        // 把平滑後的值送進 Animator（x 先維持 0）
        playerAnimation.SetMovementParameters(0f, movementBlend);
    }
}
