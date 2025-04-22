using UnityEngine;
using System.Collections.Generic;
using System.Numerics;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public struct TriangulationAnchor
{
    public Vector2 pos;   
    public float   r;
    public int signature;

    public TriangulationAnchor(Vector2 p, float range, int s)
    {
        pos = p;
        r   = range;
        signature = s;
    }
}
public struct LandmarkMeasurement
{
    public float range;
    public float bearing;
    public int signature;

    public LandmarkMeasurement(float r, float phi, int s)
    {
        range = r;
        bearing = phi;
        signature = s;
    }
}

public class LandmarkDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 10f;
    public LayerMask landmarkLayer;
    public LayerMask obstructionLayer;

    [Header("Debug")]
    public bool showDebug = true;

    private List<(Vector3, Vector3, bool)> debugRaycasts = new List<(Vector3, Vector3, bool)>();
    public List<LandmarkMeasurement> measurements = new List<LandmarkMeasurement>();
    public  List<TriangulationAnchor> triangulationAnchors = new();
    public Vector3 Observation { get; private set; }

    private void Update()
    {
        DetectVisibleLandmarks();
        var triangulation = new Triangulation(triangulationAnchors, measurements);
        Observation = triangulation.GetObservation();
    }

    private void DetectVisibleLandmarks()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, detectionRadius, landmarkLayer);
        measurements.Clear();
        triangulationAnchors.Clear();
        debugRaycasts.Clear();

        Vector3 agentPos = transform.position;
        float agentTheta = Mathf.Deg2Rad * transform.eulerAngles.y; // Convert to radians

        foreach (var collider in hitColliders)
        {
            Landmark landmark = collider.GetComponent<Landmark>();
            if (landmark != null)
            {
                Vector3 landmarkPos = landmark.transform.position;
                Vector3 direction = (landmarkPos - agentPos).normalized;
                float distance = Vector3.Distance(agentPos, landmarkPos);

                bool isVisible = !Physics.Raycast(agentPos, direction, distance, obstructionLayer);
                debugRaycasts.Add((agentPos, landmarkPos, isVisible));

                if (isVisible)
                {
                    float dx = landmarkPos.x - agentPos.x;
                    float dz = landmarkPos.z - agentPos.z;

                    float r = Mathf.Sqrt(dx * dx + dz * dz);
                    float phi = Mathf.Atan2(dz, dx) - agentTheta;

                    // Normalize 
                    phi = Mathf.Repeat(phi + Mathf.PI, 2 * Mathf.PI) - Mathf.PI;

                    measurements.Add(new LandmarkMeasurement(r, phi, landmark.signature));
                    triangulationAnchors.Add(new TriangulationAnchor(new Vector2(landmarkPos.x, landmarkPos.z),r ,landmark.signature));
                    //
                    // if (showDebug)
                    //     Debug.Log($"Landmark: {landmark.name}, r: {r:F2}, φ: {phi * Mathf.Rad2Deg:F1}°, s: {landmark.signature}");
                }
            }
        }

        if (showDebug)
            Debug.Log($"Visible Landmarks: {measurements.Count}");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        if (showDebug && debugRaycasts != null)
        {
            foreach (var ray in debugRaycasts)
            {
                Gizmos.color = ray.Item3 ? Color.green : Color.red;
                Gizmos.DrawLine(ray.Item1, ray.Item2);
            }
        }
    }
}

