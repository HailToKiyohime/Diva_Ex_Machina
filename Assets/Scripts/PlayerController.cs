using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    private PlayerControllers playerControls;

    [SerializeField] private PlayerMovement playerMovement;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerControls = new PlayerControllers();  
    }

    private void OnEnable()
    {
        playerControls.Enable();
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

     }
}
