using UnityEngine;

public class PlayerInputController : MonoBehaviour
{
    public SimpleMovement movement;
    public float timeScale=1f;

    void Update()
    {
        
        Time.timeScale = timeScale;
        float vertical = Input.GetAxisRaw("Vertical");
        float horizontal = Input.GetAxisRaw("Horizontal");

        movement.Move(vertical);
        movement.Rotate(horizontal);
    }
}
