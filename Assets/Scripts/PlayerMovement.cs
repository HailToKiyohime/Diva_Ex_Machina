using SadnessMonday.BetterPhysics;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Character orientation")]
    //[SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform characterOrientation;
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
    // Update is called once per frame
    public void Update()
    {
        EnergyRegenerationCheck();
    }
    public void FixedUpdate()
    {
        GroundCheck();
    }
    public void HorizontalMovement(float moveX, float moveZ)
    {
        moveDirection = characterOrientation.forward * moveZ + characterOrientation.right * moveX;
        Vector3 targetVelocity = moveDirection.normalized * PlayerStats.Instance.sprintSpeed;
        playerRigidbody.AddForce(targetVelocity - playerRigidbody.velocity, ForceMode.Acceleration);
    }
    public bool JumpAction()
    {
        if(grounded && readyToJump)
        {
            readyToJump = false;
            playerRigidbody.velocity = new Vector3(playerRigidbody.velocity.x, 0, playerRigidbody.velocity.z);
            playerRigidbody.AddForceWithoutLimit(transform.up * convertJumpHeightToForce(PlayerStats.Instance.jumpHeight), ForceMode.Impulse);
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
        playerRigidbody.AddForceWithoutLimit(transform.up * PlayerStats.Instance.flyForce, ForceMode.Force);
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
        // ¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X
        // 1) dynamic ground check
        // ¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X¡X
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
}
