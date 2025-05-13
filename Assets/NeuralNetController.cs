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

    public int mapDiscretizationFactor_x = 10;
    public int mapDiscretizationFactor_y = 10;

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


        // Set the nn input; goalHeading (2), position goal (2), pose estimate (3), lidarDistances (12), discretized map

        bool mapping = false; // TODO ; set mapping

        int inputDim = 2 + 12;
        if (mapping)
        {
            inputDim += 3 + mapDiscretizationFactor_x * mapDiscretizationFactor_y;
        }
        float[] input = new float[2 + 12];

        // Set the goal heading
        input[0] = goalHeading[0];
        input[1] = goalHeading[1];

        // Set the lidar distances (12)
        for (int i = 0; i < lidarDistances.Length; i++)
        {
            input[i + 2] = lidarDistances[i] / LidarSensors.maxLength;
        }

        // 

        if (mapping)
        {
            // Set goal position
            input[14] = goalPosition.x / mapSize.x;
            input[15] = goalPosition.z / mapSize.y;

            // Set the pose estimate
            input[16] = estimatePosition.x / mapSize.x;
            input[17] = estimatePosition.y / mapSize.y;
            input[18] = estimateAngle / 360;

            // Assume Mapping.map is a 2D array of some kind (e.g., int[,], float[,], or a custom struct[,])
            

            var map = Mapping.map;
            int width = Mapping.maxWidth - Mapping.minWidth;
            int height = Mapping.maxHeight - Mapping.minHeight;

            // Output array: coarser grid
            float[,] coarseMap = new float[mapDiscretizationFactor_x, mapDiscretizationFactor_y];

            for (int i = 0; i < mapDiscretizationFactor_x; i++)
            {
                for (int j = 0; j < mapDiscretizationFactor_y; j++)
                {
                    int corner_x = i * (width / mapDiscretizationFactor_x);
                    int corner_y = j * (height / mapDiscretizationFactor_y);

                    // Calculate the average value in the sub-grid
                    for (int x = corner_x; x < corner_x + (width / mapDiscretizationFactor_x); x++)
                    {
                        for (int y = corner_y; y < corner_y + (height / mapDiscretizationFactor_y); y++)
                        {
                            if (x >= 0 && x < map.GetLength(0) && y >= 0 && y < map.GetLength(1))
                            {
                                coarseMap[i, j] += Mapping.ObtainProbabilityOccupied(x, y);  // TODO: CHECK THIS
                            }
                        }
                    }

                    // Normalize the average value
                    coarseMap[i, j] /= (width / mapDiscretizationFactor_x) * (height / mapDiscretizationFactor_y);

                    // Set the input value
                    input[i * mapDiscretizationFactor_y + j + 7 + 12] = coarseMap[i, j];
                }
            }
        }

        Debug.Log("Input:" + input);

        previousControl = NeuralNet.FeedForward(input, nnWeight);
        // Debug.Log("Neural net controls: x:" + previousControl[0] + "y:" + previousControl[1]);
        movement.Move(previousControl[0]);
        movement.Rotate(previousControl[1]);
    }

    // This method disables the NN controller after goal is reached
    public void StopControl()
    {
        enabled = false;
        movement.Move(0f);
        movement.Rotate(0f);
        Debug.Log("NeuralNetController stopped");
    }
}