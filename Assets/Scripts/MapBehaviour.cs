using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapBehaviour : MonoBehaviour
{
    public Localizer Localizer;
    public LidarSensors LidarSensors;
    public Mapping Mapping;
    [Header("Map Parameters")] public int worldWidth = 50;
    public int worldHeight = 50;
    public float mapResolution = 0.05f;
    public float beamStepSize = 0.3f;
    private int logInt = 0;

    private void Start()
    {
        Mapping = new Mapping(worldWidth, worldHeight, mapResolution);
    }

    private void Update()
    {
        Mapping.UpdateMap(LidarSensors, Localizer, beamStepSize);
        logInt++;
        if (logInt % 1000 == 0)
        {
            float[,] map = Mapping.map;
            int rows = map.GetLength(0);
            int cols = map.GetLength(1);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    sb.Append(map[y, x].ToString("F2")).Append(" ");
                }
                sb.AppendLine();
            }

            Debug.Log(sb.ToString());
        }
    }
}