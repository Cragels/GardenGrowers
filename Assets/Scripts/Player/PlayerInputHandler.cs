using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Action Asset")]
    [SerializeField] private InputActionAsset playerControls;

    [Header("Action Map Name Reference")]
    [SerializeField] private string actionMapName = "Player";

    [Header("Action Name Reference")]
    [SerializeField] private string movement = "Movement";
    [SerializeField] private string rotation = "Rotation";
    [SerializeField] private string jump = "Jump";
    [SerializeField] private string sprint = "Sprint";
    [SerializeField] private string inventory = "Inventory";
    [SerializeField] private string interact = "Interact";
    [SerializeField] private string pause = "Pause";

    //[Header("Mirror Rotation Action Names")]
    //[SerializeField] private string mirrorScroll = "MirrorScroll";
    //[SerializeField] private string mirrorAxisSwitch = "MirrorAxisSwitch";
    //[SerializeField] private string mirrorLock = "MirrorLock";

    public static PlayerInputHandler Instance { get; private set; }

    //[SerializeField] private UIManager uiManager;



    private InputAction movementAction;
    private InputAction rotationAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction inventoryAction;
    private InputAction interactAction;
    private InputAction pauseAction;

    private InputAction rotateScrollAction;
    private InputAction rotateAxisSwitchAction;
    private InputAction rotateLockAction;

    public Vector2 MovementInput {  get; private set; }
    public Vector2 RotationInput { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool SprintTriggered { get; private set; }
   // public bool InventoryTriggered { get; private set; }
    public bool InteractTriggered { get; private set; }

    public bool RotateAxisSwitchTriggered { get; private set; }
    public bool RotateLockTriggered { get; private set; }

    private void Awake()
    {
        Instance = this; 

        InputActionMap mapReference = playerControls.FindActionMap(actionMapName);

        movementAction = mapReference.FindAction(movement);
        rotationAction = mapReference.FindAction(rotation);
        jumpAction = mapReference.FindAction(jump);
        sprintAction = mapReference.FindAction(sprint);
        inventoryAction = mapReference.FindAction(inventory);
        interactAction = mapReference.FindAction(interact);

        pauseAction = mapReference.FindAction(pause);

        //rotateScrollAction = mapReference.FindAction(mirrorScroll);
        //rotateAxisSwitchAction = mapReference.FindAction(mirrorAxisSwitch);
        //rotateLockAction = mapReference.FindAction(mirrorLock);

        SubscribeActionValuesToInputEvents();
    }

    private void SubscribeActionValuesToInputEvents()
    {
        movementAction.performed += inputInfo => MovementInput = inputInfo.ReadValue<Vector2>();
        movementAction.canceled += inputInfo => MovementInput = Vector2.zero;

        rotationAction.performed += inputInfo => RotationInput = inputInfo.ReadValue<Vector2>();
        rotationAction.canceled += inputInfo => RotationInput = Vector2.zero;

        jumpAction.performed += inputInfo => JumpTriggered = true;
        jumpAction.canceled += inputInfo => JumpTriggered = false;

        sprintAction.performed += inputInfo => SprintTriggered = true;
        sprintAction.canceled += inputInfo => SprintTriggered = false;


        interactAction.performed += inputInfo => InteractTriggered = true;
        interactAction.canceled += inputInfo => InteractTriggered = false;

        pauseAction.performed += inputInfo => UIManager.Instance.PauseMenuControle();

        inventoryAction.performed += inputInfo => UIManager.Instance.InventoryUIControl();

        //rotateAxisSwitchAction.performed += inputInfo => RotateAxisSwitchTriggered = true;
        //rotateAxisSwitchAction.canceled += inputInfo => RotateAxisSwitchTriggered = false;
        //rotateLockAction.performed += inputInfo => RotateLockTriggered = true;
        //rotateLockAction.canceled += inputInfo => RotateLockTriggered = false;

    }
    /*
    public bool ConsumeScan()
    {
        if (!ScanTriggered) return false;
        ScanTriggered = false;
        return true;
    }
    */
    public bool ConsumeInteract()
    {
        if (!InteractTriggered) return false;
        InteractTriggered = false;
        return true;
    }

    /*
    public bool ConsumeRotateAxisSwitch()
    {
        if (!RotateAxisSwitchTriggered) return false;
        RotateAxisSwitchTriggered = false;
        return true;
    }
    public bool ConsumeRotateLock()
    {
        if (!RotateLockTriggered) return false;
        RotateLockTriggered = false;
        return true;
    }

    // Scroll is a per-frame delta, so read it live rather than cache it from an event.
    public float GetRotateScrollY()
    {
        return rotateScrollAction == null ? 0f : rotateScrollAction.ReadValue<Vector2>().y;
    }
    */

    private void OnEnable()
    {
        playerControls.FindActionMap(actionMapName).Enable();
    }

    private void OnDisable() 
    {
        playerControls.FindActionMap(actionMapName).Disable();
    }
}
