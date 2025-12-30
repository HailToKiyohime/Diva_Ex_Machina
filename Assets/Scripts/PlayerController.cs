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

        if (playerControls.Player.Jump.IsPressed())
        {
            playerMovement.JumpAction();
        }

        if (playerControls.Player.Sprint.WasPressedThisFrame())
        {
            playerMovement.DashAction();
        }

        // Reload gate:
        // - If Reload is held and Attack is pressed on a hand:
        //     * Range weapon -> Reload
        //     * Melee/None   -> treat as normal attack (so melee still works while holding Reload)
        bool reloadHeld = playerControls.Player.Reload.IsPressed();
        bool leftPressed = playerControls.Player.LeftHandAttack.WasPressedThisFrame();
        bool rightPressed = playerControls.Player.RightHandAttack.WasPressedThisFrame();

        var stats = PlayerStats.Instance;

        if (reloadHeld && (leftPressed || rightPressed))
        {
            if (leftPressed)
            {
                bool leftIsRange = (stats != null && stats.leftHand.weaponKind == HandWeaponKind.Range);
                if (leftIsRange) attackManager.StartReload(attackManager.leftWeapon);
                else playerMovement.ProcessAttackFacingAndAttack(attackManager, attackManager.leftWeapon, playerControls.Player.LeftHandAttack);
            }

            if (rightPressed)
            {
                bool rightIsRange = (stats != null && stats.rightHand.weaponKind == HandWeaponKind.Range);
                if (rightIsRange) attackManager.StartReload(attackManager.rightWeapon);
                else playerMovement.ProcessAttackFacingAndAttack(attackManager, attackManager.rightWeapon, playerControls.Player.RightHandAttack);
            }

            return;
        }

        // Normal attack (range or melee is decided inside PlayerMovement.ProcessAttackFacingAndAttack)
        playerMovement.ProcessAttackFacingAndAttack(attackManager, attackManager.leftWeapon, playerControls.Player.LeftHandAttack);
        playerMovement.ProcessAttackFacingAndAttack(attackManager, attackManager.rightWeapon, playerControls.Player.RightHandAttack);
    }
}
