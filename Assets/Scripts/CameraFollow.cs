using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Tooltip("The target the camera should follow")]
    public Transform target;

    [Tooltip("Offset from the target's position")]
    public Vector3 offset = new Vector3(0f, 5f, -10f);

    [Tooltip("How smooth the camera movement is")]
    public float smoothSpeed = 0.125f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        transform.position = smoothedPosition;

        // transform.LookAt(target);
    }
}
