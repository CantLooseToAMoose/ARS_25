using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class NeuralNetController : MonoBehaviour
{
    public SimpleMovement movement;
    public LidarSensors LidarSensors;
    public Localizer Localizer;
    public AgentExperimentController ExperimentController;

    [HideInInspector] public float[] lidarDistances = new float[12];
    [HideInInspector] public float[] goalHeading = new float[2];
    [HideInInspector] private float[] previousControl = new float[2];
    [HideInInspector] public float[] nnWeight = new float[78];

    [Header("Map Size")] public Vector2 mapSize = new Vector2(50, 50);

    [Header("Existing Weights")] public bool useExistingWeights = false;
    public string pathToExistingWeightsFile = "best_weights.csv";

    [Header("Debug")] public bool debug;

    public void SetWeights(float[] weights)
    {
        if (weights.Length != 78)
        {
            Debug.LogError("Incorrect weight vector size");
            return;
        }

        nnWeight = weights;
    }

    private void Start()
    {
        if (useExistingWeights)
        {
            var loaded = LoadWeightsFromCSV(pathToExistingWeightsFile);
            if (loaded != null)
            {
                SetWeights(loaded);
                Debug.Log("✅ Loaded weights from file: " + pathToExistingWeightsFile);
                return;
            }
            else
            {
                Debug.LogWarning("⚠ Failed to load weights from file. Falling back to random weights.");
            }
        }
    }

    private float[] LoadWeightsFromCSV(string filename)
    {
        string path = Path.Combine(Application.dataPath, filename);
        if (!File.Exists(path))
        {
            Debug.LogError($"CSV file not found: {path}");
            return null;
        }

        List<float> weights = new List<float>();
        try
        {
            using (StreamReader reader = new StreamReader(path))
            {
                string line;
                bool isHeader = true;
                while ((line = reader.ReadLine()) != null)
                {
                    if (isHeader)
                    {
                        isHeader = false;
                        continue;
                    }

                    string[] parts = line.Split(',');
                    if (parts.Length >= 2 && float.TryParse(parts[1], out float weight))
                    {
                        weights.Add(weight);
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Error reading weights CSV: " + e.Message);
            return null;
        }

        if (weights.Count != 78)
        {
            Debug.LogError($"CSV does not contain 78 weights (found {weights.Count})");
            return null;
        }

        return weights.ToArray();
    }

    void FixedUpdate()
    {
        if (nnWeight.Length != 78)
        {
            Debug.LogError("NN has the Wrong Size");
            return;
        }

        // Get all Lidar distances
        LidarSensors.RaycastResult[] lastScan = LidarSensors.LastScan;
        if (lastScan!=null)
        {
            for (int i = 0; i < lastScan.Length; i++)
            {
                lidarDistances[i] = lastScan[i].distance;
            }
        }


        // Calculate goal heading
        Vector3 goalPosition = ExperimentController.goalTransform.position;
        Vector2 goalPosition2D = new Vector2(goalPosition.x, goalPosition.z);
        Vector3 stateEstimate = Localizer.stateEstimate;
        Vector2 estimatePosition = new Vector2(stateEstimate.y, stateEstimate.x);
        float estimateAngle = stateEstimate.z * Mathf.Rad2Deg;
        Vector2 goalDifferenceVector = goalPosition2D - estimatePosition;
        float distance = goalDifferenceVector.magnitude;
        float beta = math.atan2(goalDifferenceVector.y, goalDifferenceVector.x) * Mathf.Rad2Deg;
        float alpha = estimateAngle - beta + 90;
        if (alpha > 180) alpha -= 360;

        if (debug)
        {
            Debug.Log("Estimate Angle: " + estimateAngle);
            Debug.Log("Beta: " + beta);
            Debug.Log("Distance to goal: " + distance);
            Debug.Log("Angle to goal: " + alpha);
        }

        goalHeading = new[] { distance / mapSize.magnitude, alpha / 180 };

        // Feed inputs into network
        float[] input = new float[16];
        input[0] = previousControl[0];
        input[1] = previousControl[1];
        for (int i = 0; i < lidarDistances.Length; i++)
        {
            input[i + 2] = lidarDistances[i] / LidarSensors.maxLength;
        }

        input[14] = goalHeading[0];
        input[15] = goalHeading[1];

        previousControl = NeuralNet.FeedForward(input, nnWeight);
        // Debug.Log("Neural net controls: x:" + previousControl[0] + "y:" + previousControl[1]);
        movement.Move(previousControl[0]);
        movement.Rotate(previousControl[1]);
    }

    // 🔴 This method disables the NN controller after goal is reached
    public void StopControl()
    {
        enabled = false;
        movement.Move(0f);
        movement.Rotate(0f);
        Debug.Log("🛑 NeuralNetController stopped");
    }
}