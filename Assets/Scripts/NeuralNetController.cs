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
    public MapBehaviour MapBehaviour;

    public int localMapSize = 10;


    [HideInInspector] public float[] lidarDistances = new float[12];
    [HideInInspector] public float[] goalHeading = new float[2];
    [HideInInspector] private float[] previousControl = new float[2];
    [HideInInspector] public float[] nnWeight;
    private EvolutionExperimentManager _experimentManager;

    [Header("Map Size")] public Vector2 mapSize = new Vector2(50, 50);

    [Header("Existing Weights")] public bool useExistingWeights = false;
    public string pathToExistingWeightsFile = "best_weights.csv";

    [Header("Debug")] public bool debug;

    public void SetWeights(float[] weights)
    {
        nnWeight = weights;
    }

    private void Start()
    {
        if (useExistingWeights)
        {
            var loaded = LoadWeightsFromCSV(pathToExistingWeightsFile);
            if (loaded != null)
            {
                Debug.Log("Loaded Weights: [" + string.Join(", ", loaded) + "]");
                SetWeights(loaded);
                Debug.Log("Loaded weights from file: " + pathToExistingWeightsFile);
            }
            else
            {
                Debug.LogWarning("Failed to load weights from file. Falling back to random weights.");
            }
        }

        if (MapBehaviour == null)
        {
            if (!TryGetComponent<MapBehaviour>(out MapBehaviour mapBehaviour))
            {
                Debug.LogWarning("MapBehaviour component not found on this GameObject.");
            }
            else
            {
                MapBehaviour = mapBehaviour;
            }
        }

        // Find the EvolutionExperimentManager in the scene
        _experimentManager = FindObjectOfType<EvolutionExperimentManager>();
        if (_experimentManager == null)
        {
            Debug.LogWarning("EvolutionExperimentManager not found in the scene.");
        }
        else
        {
            if (debug)
            {
                Debug.Log("EvolutionExperimentManager assigned successfully.");
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

                    string[] parts = line.Split(';');
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

        return weights.ToArray();
    }

    void FixedUpdate()
    {
        // Get all Lidar distances
        LidarSensors.RaycastResult[] lastScan = LidarSensors.LastScan;
        if (lastScan != null)
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
        Vector2 estimatePosition = new Vector2(stateEstimate.y, stateEstimate.x); // (x, z) mapping

         // Fix: Convert radians to degrees, flip clockwise to CCW, wrap to [0,360)
        float estimateAngle = (360f - (stateEstimate.z * Mathf.Rad2Deg)) % 360f;

        // Fix: Convert heading-from-Z to heading-from-X
        float estimateAngleFromX = estimateAngle +90f;

        Vector2 goalDifferenceVector = goalPosition2D - estimatePosition;
        float distance = goalDifferenceVector.magnitude;

        // Angle to goal from X+
        float beta = math.atan2(goalDifferenceVector.y, goalDifferenceVector.x) * Mathf.Rad2Deg;

        // Shortest signed angle between current heading and goal direction
        float alpha = Mathf.DeltaAngle(estimateAngleFromX, beta);

        if (debug)
        {
            Debug.Log("Estimate Angle (wrapped CCW): " + estimateAngle);
            Debug.Log("Beta (to goal from X+): " + beta);
            Debug.Log("Distance to goal: " + distance);
            Debug.Log("Angle to goal (alpha): " + alpha);
        }

        // Normalize for neural net input
        goalHeading = new[] { distance / mapSize.magnitude, alpha / 180f };

        // Debug.Log("Goal Vector: Radius{" + distance + "},Angle:{" + alpha + "}");


        // Set the nn input; goalHeading (2), position goal (2), pose estimate (3), lidarDistances (12), discretized map

        // Enable mapping if MapBehaviour is active
        bool mapping = MapBehaviour != null && MapBehaviour.isActiveAndEnabled;


        int inputDim = 2 + 2 + 12;
        if (mapping)
        {
            inputDim += 5 + localMapSize * localMapSize;
        }

        float[] input = new float[inputDim];

        // Set the goal heading
        input[0] = goalHeading[0];
        input[1] = goalHeading[1];

        // Set previous control
        input[2] = previousControl[0];
        input[3] = previousControl[1];

        // Set the lidar distances (12)
        for (int i = 0; i < lidarDistances.Length; i++)
        {
            input[i + 4] = lidarDistances[i] / LidarSensors.maxLength;
        }

        // 

        if (mapping)
        {
            // Set goal position
            input[16] = goalPosition.x / mapSize.x;
            input[17] = goalPosition.z / mapSize.y;

            // Set the pose estimate
            var esitimatePosX = estimatePosition.x / mapSize.x;
            var esitimatePosY = estimatePosition.y / mapSize.y;

            input[18] = esitimatePosX;
            input[19] = esitimatePosY;
            input[20] = estimateAngle / 360;

            // Assume Mapping.map is a 2D array of some kind (e.g., int[,], float[,], or a custom struct[,])
            var map = MapBehaviour.Mapping.map;
            int width = (int)Math.Round(MapBehaviour.Mapping.maxWidth - MapBehaviour.Mapping.minWidth);
            int height = (int)Math.Round(MapBehaviour.Mapping.maxHeight - MapBehaviour.Mapping.minHeight);
            // Getting a local map based on the agents 5, 5 surrounding, so check on the agents position for the relevant tiles
            int count = 0;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    // Check if the current grid cell is within the local map size, based on esitimatePosX and esitimatePosY
                    if (i >= esitimatePosX - localMapSize / 2 && i <= esitimatePosX + localMapSize / 2 &&
                        j >= esitimatePosY - localMapSize / 2 && j <= esitimatePosY + localMapSize / 2)
                    {
                        // Normalize the map value to be between 0 and 1
                        input[count + 21] = 1.0f - 1.0f / (1.0f + Mathf.Exp(map[i, j]));

                        count++;
                    }
                }
            }
        }

        if (debug)
        {
            Debug.Log("Input: [" + string.Join(", ", input) + "]");
        }

        previousControl = ANeuralNet.FeedForward(input, nnWeight, new[] { inputDim, 16, 8, 2 });
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