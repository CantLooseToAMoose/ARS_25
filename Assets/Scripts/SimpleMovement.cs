using System;
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

    [Header("Noise Settings")] public float movementNoise = 0.1f;
    public float rotationNoise = 0.1f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        Debug.Log("Movement Controller Initialized");
    }


    void Update()
    {
        ApplyMovement();
        ApplyRotation();
        // Debug.Log("Velocity: " + rb.velocity);
    }

    public float GetForwardVelocity()
    {
        return speed * moveInput;
    }

    public float GetRotationalVelocity()
    {
        return rotationSpeed * rotationInput;
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
        // If moveInput == NaN, it will be ignored
        if (float.IsNaN(moveInput))
        {
            // Debug.Log("NaN detected in move input");
            // Debug.Log($"moveInput: {moveInput}, speed: {speed}");
            return;
        }

        if (moveInput == 0f)
        {
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
            return;
        }

        float noise = 1f + GaussianSampler.SampleGaussian(0f, movementNoise);

        Vector3 forwardVelocity = transform.forward * (moveInput * noise * speed);

        // Check for bugs
        if (float.IsNaN(forwardVelocity.x) || float.IsNaN(forwardVelocity.z))
        {
            // Debug.Log("NaN detected in forward velocity");
            // Debug.Log($"moveInput: {moveInput}, noise: {noise}, speed: {speed}, forwardVelocity: {forwardVelocity}");
            forwardVelocity = Vector3.zero;
        }

        rb.velocity = new Vector3(forwardVelocity.x, rb.velocity.y, forwardVelocity.z);
    }

    private void ApplyRotation()
    {
        // If rotationInput == NaN, it will be ignored
        if (float.IsNaN(rotationInput))
        {
            // Debug.Log("NaN detected in rotation input");
            // Debug.Log($"rotationInput: {rotationInput}, rotationSpeed: {rotationSpeed}");
            return;
        }

        if (rotationInput == 0f)
        {
            return;
        }

        float noise = GaussianSampler.SampleGaussian(0, rotationNoise);

        float rotation = rotationInput * rotationSpeed * Time.deltaTime + noise * Time.deltaTime;
        Quaternion deltaRotation = Quaternion.Euler(0f, rotation, 0f);

        // Check for bugs
        if (float.IsNaN(deltaRotation.x) || float.IsNaN(deltaRotation.y) || float.IsNaN(deltaRotation.z))
        {
            // Debug.Log("NaN detected in delta rotation");
            // Debug.Log($"rotationInput: {rotationInput}, rotationSpeed: {rotationSpeed}, deltaRotation: {deltaRotation}");
            return;
        }

        rb.MoveRotation(rb.rotation * deltaRotation);
    }

    // --- Debug Text Gizmos ---

    void OnDrawGizmosSelected()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

#if UNITY_EDITOR
        Vector3 labelPos = transform.position + Vector3.up * 2.5f + Vector3.back * 1f;

        float velocityMag = rb.velocity.magnitude;

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