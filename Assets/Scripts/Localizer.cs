using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor; // Required for Handles

public class Localizer : MonoBehaviour
{
    public float Rx = 0.1f;
    public float Ry = 0.1f;
    public float Rtheta = 0.1f;
    public float Qx = 0.1f;
    public float Qy = 0.1f;
    public float Qtheta = 0.1f;

    public SimpleMovement movement;
    public KalmanPrediction kalmanPrediction;
    public LandmarkDetector landmarkDetector;
    public float3 stateEstimate = new float3();
    public float3x3 covarianceEstimate = new float3x3();


    [Header("Debug Settings")] private List<Vector3> trajectoryPoints = new List<Vector3>();
    public float dotSpacing = 4f;

    private void Start()
    {
        kalmanPrediction = new KalmanPrediction(Rx, Ry, Rtheta, Qx, Qy, Qtheta);
        landmarkDetector = GetComponent<LandmarkDetector>();
        stateEstimate.x = transform.position.z;
        stateEstimate.y = transform.position.x;
    }

    private void FixedUpdate()
    {
        kalmanPrediction.deltaTime = Time.deltaTime;
        var control = new float2(movement.GetForwardVelocity(), movement.GetRotationalVelocity());

        // Debug.Log($"Control in Localizer: {control.x}, {control.y}");

        if (control.x == 0 && control.y == 0)
        {
            return;
        }

        control.y = Mathf.Deg2Rad * control.y;
        var prediction = kalmanPrediction.PredictionStep(stateEstimate, covarianceEstimate, control);
        stateEstimate = prediction.Item1;
        covarianceEstimate = prediction.Item2;
        if (landmarkDetector.triangulationAnchors.Count >= 3)
        {
            Vector3 z = landmarkDetector.Observation;
            z = new float3(z.y, z.x, z.z);
            z.z = Mathf.Deg2Rad * z.z;
            var correctedPrediction = kalmanPrediction.CorrectionStep(stateEstimate, covarianceEstimate, (float3)z);
            stateEstimate = correctedPrediction.Item1;
            covarianceEstimate = correctedPrediction.Item2;
        }

        trajectoryPoints.Add(new Vector3(stateEstimate.y, 0, stateEstimate.x));
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        DrawEstimateLabel();
        // DrawPredictedTrajectory();
        // DrawCovarianceEllipse();
    }

    private void DrawEstimateLabel()
    {
        Vector3 worldPosition = transform.position - new Vector3(0, 0.5f, -3);
        Vector3 screenPosition = Camera.current.WorldToScreenPoint(worldPosition);

        if (screenPosition.z > 0)
        {
            Vector2 labelPosition = new Vector2(screenPosition.x, Screen.height - screenPosition.y);
            UnityEditor.Handles.BeginGUI();

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = 14;
            style.normal.textColor = Color.black;
            style.alignment = TextAnchor.MiddleCenter;

            string label =
                $"x: {stateEstimate.y:F2}, y: {stateEstimate.x:F2}, θ: {(math.degrees(stateEstimate.z) % 360 + 360) % 360:F1}°";
            Vector2 size = style.CalcSize(new GUIContent(label));
            GUI.Label(new Rect(labelPosition.x - size.x / 2, labelPosition.y - size.y / 2, size.x, size.y), label,
                style);

            UnityEditor.Handles.EndGUI();
        }
    }

private void DrawCovarianceEllipse()
{
#if UNITY_EDITOR
    if (!Application.isPlaying) return;

    // Extract 2x2 covariance for x and y
    float2 mean = new float2(stateEstimate.x, stateEstimate.y);
    float2x2 cov2D = new float2x2(
        covarianceEstimate.c0.x, covarianceEstimate.c0.y,
        covarianceEstimate.c1.x, covarianceEstimate.c1.y
    );

    // Eigen decomposition
    float trace = cov2D.c0.x + cov2D.c1.y;
    float det = cov2D.c0.x * cov2D.c1.y - cov2D.c0.y * cov2D.c1.x;
    float temp = math.sqrt(math.pow(trace / 2, 2) - det);
    float lambda1 = trace / 2 + temp;
    float lambda2 = trace / 2 - temp;

    // Eigenvectors
    float2 direction;
    if (math.abs(cov2D.c0.y) > 0.001f)
    {
        direction = math.normalize(new float2(lambda1 - cov2D.c1.y, cov2D.c0.y));
    }
    else
    {
        direction = new float2(1, 0);
    }

    // Angle and axes
    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    float a = 2 * math.sqrt(lambda1); // major axis (scaled for visibility)
    float b = 2 * math.sqrt(lambda2); // minor axis

    // Draw ellipse
    Handles.color = Color.green;
    Matrix4x4 matrix = Matrix4x4.TRS(
        new Vector3(mean.y, 0, mean.x), // notice switch x<->y to match your transform mapping
        Quaternion.Euler(0, -angle, 0),
        new Vector3(b, 1, a)
    );
    using (new Handles.DrawingScope(matrix))
    {
        Handles.DrawWireDisc(Vector3.zero, Vector3.up, 1);
    }
#endif
}

    private void DrawPredictedTrajectory()
    {
#if UNITY_EDITOR
        if (trajectoryPoints.Count < 2) return;

        Handles.color = Color.magenta;

        for (int i = 0; i < trajectoryPoints.Count - 1; i++)
        {
            Handles.DrawDottedLine(trajectoryPoints[i], trajectoryPoints[i + 1], dotSpacing);
        }
#endif
    }
}