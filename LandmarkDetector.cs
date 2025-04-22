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

    private void Update()
    {
        DetectVisibleLandmarks();
        Vector3 obs = GetObservation();
        Debug.Log($"pose ≈ {obs:F1}°");


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

                    // Normalize φ to [-π, π]
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
    private Vector2 SolveSubset(IReadOnlyList<TriangulationAnchor> subset)
    {
        int m = subset.Count;
        if (m < 3) return Vector2.positiveInfinity;

        Vector2 p1 = subset[0].pos;
        float   r1 = subset[0].r;

        double a11 = 0, a12 = 0, b1 = 0;
        double a21 = 0, a22 = 0, b2 = 0;

        for (int i = 1; i < m; ++i)
        {
            Vector2 pi = subset[i].pos;
            float   ri = subset[i].r;

            double Ai = 2 * (pi.x - p1.x);
            double Bi = 2 * (pi.y - p1.y);
            double Ci = pi.x * pi.x - p1.x * p1.x
                + pi.y * pi.y - p1.y * p1.y
                + r1 * r1     - ri * ri;

            a11 += Ai * Ai; a12 += Ai * Bi; b1 += Ai * Ci;
            a21 += Bi * Ai; a22 += Bi * Bi; b2 += Bi * Ci;
        }

        double det = a11 * a22 - a12 * a21;
        if (Mathf.Abs((float)det) < 1e-6f)   // nearly singular – ignore this subset
            return Vector2.positiveInfinity;

        return new Vector2(
            (float)((b1 * a22 - a12 * b2) / det),
            (float)((a11 * b2 - b1 * a21) / det));
    }
    public Vector2 TryTriangulate()
    {
        int n = triangulationAnchors.Count;
        if (n < 3) return Vector2.zero;      // need at least three

        // ● Exactly 3 anchors → just solve once
        if (n == 3) return SolveSubset(triangulationAnchors);

        // ● More than 3 → average the pose from every ⟨i,j,k⟩ triple
        Vector2 sum = Vector2.zero;
        int     good = 0;

        var triple = new TriangulationAnchor[3];
        for (int i = 0; i < n - 2; ++i)
        for (int j = i + 1; j < n - 1; ++j)
        for (int k = j + 1; k < n; ++k)
        {
            triple[0] = triangulationAnchors[i];
            triple[1] = triangulationAnchors[j];
            triple[2] = triangulationAnchors[k];

            Vector2 est = SolveSubset(triple);
            if (float.IsInfinity(est.x)) continue;   // error in the triple

            sum += est;
            ++good;
        }

        return good > 0 ? sum / good : Vector2.zero;
    }
    
    private float EstimateHeading(Vector2 agentPos)
    {
        int m = measurements.Count;
        if (m < 2) return float.NaN;

        float sinSum = 0f, cosSum = 0f;

        for (int i = 0; i < m; ++i)
        {
            Vector2 lmPos = triangulationAnchors[i].pos;

            float phiGlobal = Mathf.Atan2(lmPos.y - agentPos.y,
                lmPos.x - agentPos.x);

            float theta_i = phiGlobal - measurements[i].bearing;

            // wrap to (‑π, π] for the averaging step
            theta_i = Mathf.Repeat(theta_i + Mathf.PI, 2f * Mathf.PI) - Mathf.PI;

            sinSum += Mathf.Sin(theta_i);
            cosSum += Mathf.Cos(theta_i);
        }

        // mean angle in (‑π, π]
        float theta = Mathf.Atan2(sinSum / m, cosSum / m);

        // ----- convert to [0, 2π) -----
        if (theta < 0f) theta += 2f * Mathf.PI;          // now 0 … 2π

        return theta * Mathf.Rad2Deg;                    // 0 … 360°
    }

    public Vector3 GetObservation()
    {
        Vector2 loc = TryTriangulate();
        float heading = EstimateHeading(loc);
        
        return new Vector3(loc[0], loc[1], heading);
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

