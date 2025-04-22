using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor; // Required for Handles

public class Localizer : MonoBehaviour
{
    public SimpleMovement movement;
    public KalmanPrediction kalmanPrediction;
    public LandmarkDetector landmarkDetector;
    public float3 stateEstimate = new float3();
    public float3x3 covarianceEstimate = new float3x3();

    [Header("Prediction Settings")] public int predictionSteps = 20; // 🔥 Number of steps to look ahead
    public float predictionStepTime = 0.1f; // 🔥 Time per step
    public Color predictedColor = Color.blue; // 🔥 Color of prediction line

    // Estimate Trajectory
    [Header("Debug Settings")]
    private List<Vector3> trajectoryPoints = new List<Vector3>();
    public float dotSpacing = 4f;    // Spacing between dots in pixels

    private void Start()
    {
        kalmanPrediction = new KalmanPrediction();
        landmarkDetector = GetComponent<LandmarkDetector>();
    }

    private void Update()
    {
        kalmanPrediction.deltaTime = Time.deltaTime;
        var control = new float2(movement.GetForwardVelocity(), movement.GetRotationalVelocity());
        control.y = Mathf.Deg2Rad * control.y; // Convert rotation to radians
        var prediction = kalmanPrediction.PredictionStep(stateEstimate, covarianceEstimate, control);
        if (landmarkDetector.triangulationAnchors.Count < 3)
        {
            stateEstimate = prediction.Item1;
            covarianceEstimate = prediction.Item2;
        }
        else
        {
            Vector3 z = landmarkDetector.Observation;   
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
        DrawPredictedTrajectory();
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

    private void DrawPredictedTrajectory() // 🔥
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