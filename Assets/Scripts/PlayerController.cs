using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{

    public static PlayerController Instance { get; private set; }
    private PlayerControllers playerControls;

    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private AttackManager attackManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        playerControls = new PlayerControllers();  
    }

    private void OnEnable()
    {
        playerControls.Player.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 movementInput = playerControls.Player.Move.ReadValue<Vector2>();
        playerMovement.HorizontalMovement(movementInput.x, movementInput.y);
        bool jumpPressed = playerControls.Player.Jump.IsPressed();
        if (jumpPressed)
        {
            playerMovement.JumpAction();
        }
        if (playerControls.Player.Sprint.WasPressedThisFrame())
        {

        }
        if (playerControls.Player.Reload.IsPressed())
        {
            if (playerControls.Player.LeftHandAttack.WasPressedThisFrame())
            {
                attackManager.StartReload(attackManager.leftWeapon);
            }
            else if (playerControls.Player.RightHandAttack.WasPressedThisFrame())
            {
                attackManager.StartReload(attackManager.rightWeapon);
            }
        }
        else
        {
            playerMovement.ProcessAttackFacingAndShoot(attackManager, attackManager.leftWeapon, playerControls.Player.LeftHandAttack);
            playerMovement.ProcessAttackFacingAndShoot(attackManager, attackManager.rightWeapon, playerControls.Player.RightHandAttack);
        }
    }
}
