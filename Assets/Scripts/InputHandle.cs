using UnityEngine;

public class InputHandle : MonoBehaviour
{
    public float MovementInput { get; private set; }
    public float TurnInput { get; private set; }
    public bool JumpPress { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool JumpHeld { get; private set; }
    public bool PausePressed { get; private set; }

    void Update()
    {
        MovementInput = Input.GetAxis("Vertical");
        TurnInput = Input.GetAxis("Horizontal");
        JumpPress = Input.GetButtonDown("Jump");
        JumpHeld = Input.GetButton("Jump");
        SprintHeld = Input.GetKey(KeyCode.LeftShift);
        PausePressed = Input.GetKeyDown(KeyCode.Escape);
    }
}
