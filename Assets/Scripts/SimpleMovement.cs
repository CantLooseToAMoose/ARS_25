using UnityEngine;
using UnityEditor;

[RequireComponent(typeof(Rigidbody))]
public class SimpleMovement : MonoBehaviour
{
    public float speed = 5f;
    public float rotationSpeed = 200f;

    private Rigidbody rb;

    private float moveInput = 0f;
    private float rotationInput = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Debug.Log("Movement Controller Initialized");
    }

    void FixedUpdate()
    {
        ApplyMovement();
        ApplyRotation();
    }

    // --- Public API Methods ---

    public void Move(float input)
    {
        moveInput = Mathf.Clamp(input, -1f, 1f); // Normalize input
    }

    public void Rotate(float input)
    {
        rotationInput = Mathf.Clamp(input, -1f, 1f); // Normalize input
    }

    // --- Internal Movement Logic ---

    private void ApplyMovement()
    {
        Vector3 forwardVelocity = transform.forward * (moveInput * speed);
        rb.velocity = new Vector3(forwardVelocity.x, rb.velocity.y, forwardVelocity.z);
    }

    private void ApplyRotation()
    {
        if (Mathf.Abs(rotationInput) > 0f)
        {
            float rotation = rotationInput * rotationSpeed * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.Euler(0f, rotation, 0f);
            rb.MoveRotation(rb.rotation * deltaRotation);
        }
    }

    // --- Debug Text Gizmos ---

    void OnDrawGizmosSelected()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

#if UNITY_EDITOR
        Vector3 labelPos = transform.position + Vector3.up * 2.5f + Vector3.back * 1f;

        float velocityMag = rb.velocity.magnitude;

        // This is the rotation speed you're commanding (in degrees/sec)
        float angularVelocityDeg = rotationInput * rotationSpeed;

        string text = $"Vel: {velocityMag:F2} m/s\nAngVel: {angularVelocityDeg:F2} °/s";

        GUIStyle style = new GUIStyle
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.black },
            alignment = TextAnchor.MiddleCenter
        };

        Handles.Label(labelPos, text, style);
#endif
    }

}