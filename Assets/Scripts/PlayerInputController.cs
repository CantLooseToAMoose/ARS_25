using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    public SimpleMovement movement;

    void Update()
    {
        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");

        movement.Move(vertical);
        movement.Rotate(horizontal);
    }
}
