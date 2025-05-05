using Unity.Mathematics;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using System;
using System.Collections.Generic;

public class Mapping
{
    public float[,] map; // 2D grid array to represent the map

    // Occupancy grid parameters
    public float priorLogOdds = 0.0f; // Prior log odds for each grid cell being occupied : log (p_occupied / p_free)
    public float freeGridOdds = -0.5f; // Log odds for free grid cells
    public float occupiedGridOdds = 0.5f; // Log odds for occupied grid cells
    public float resolution; // Meaning the amount of continuous space needed for one grid (currently the same for width and height)
    public float width, height; // 
    
    /// <summary>please change if we change the size and coordinates of our map
    /// currently these are the coordinates of the corners of our map idk how to get these cleaner unity noob.</summary>
    public float minWidth = -25;
    public float maxWidth = 25;
    public float minHeight = -25;
    public float maxHeight = 25;
    
    
    public Mapping(float worldWidth, float worldHeight, float resolution) // expects total length and width in continuous lentghs
    {
        this.resolution = resolution;
        this.width  = Mathf.CeilToInt(worldWidth  / resolution);
        this.height = Mathf.CeilToInt(worldHeight / resolution);
        
        map = new float[width+1, height+1];
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                map[i, j] = priorLogOdds;
            }
        }
    }

    // Define the Grid structure
    public struct Grid
    {
        public int x, y; // Coordinates of the grid cell
        public bool occupied; // Occupied status (true = occupied, false = free)

        public Grid(int x, int y, bool occupied)
        {
            this.x = x;
            this.y = y;
            this.occupied = occupied;
        }
    }

    // This function uses the lidar sensors to match the beams to grids
    public Grid[] RetrieveGrids(LidarSensors sensor, float stepSize) 
    {
        RaycastResult[] scan   = sensor.LastScan;             // contains Vector3 direction; float distance; Vector3 hitPoint; bool hit;
        Vector3 origin = sensor.transform.position;   // Vector3 .x ==x, .y == height, .z == y

        var grids = new List<Grid>();
        foreach (var beam in scan)
        {
            // Make the first cell occupied cell if we hit something
            float maxDist = beam.hit 
                ? beam.distance 
                : sensor.maxLength;

            if (beam.hit)
            {
                Vector2Int hitIndex = WorldToGrid(beam.hitPoint.x, beam.hitPoint.z);
                grids.Add(new Grid(hitIndex.x, hitIndex.y, true));
            }

            // 2) Walk from the origin of the sensor with some stepsize to the end
            int steps = Mathf.CeilToInt(maxDist/ stepSize);
            for (int i = 1; i < steps; i++)
            {
                float d = i * stepSize;
                Vector3 newPos = origin + beam.direction * d;
                Vector2Int hitIndex = WorldToGrid(newPos.x, newPos.z);
                Grid cell = new Grid(hitIndex.x, hitIndex.y, false);
                if (!grids.Exists(g => g.x == cell.x && g.y == cell.y))
                {
                    grids.Add(cell);
                }
                
            }
        }

        return grids.ToArray();
    }

    // This function updates the map with the new beams - Occupancy grid mapping
    public void UpdateMap(LidarSensors sensor, float stepSize)
    {
        Grid[] grids = RetrieveGrids(sensor, stepSize);

        foreach (var grid in grids)
        {
            // Get the current log odds for this grid cell
            float logOdds = map[grid.x, grid.y];

            if (grid.occupied)
            {
                // Update log odds for occupied cell
                logOdds += occupiedGridOdds - priorLogOdds;
            }
            else
            {
                // Update log odds for free cell
                logOdds += freeGridOdds - priorLogOdds;
            }

            // Update the map with the new log odds value
            map[grid.x, grid.y] = logOdds;
        }
    }

    // Function to convert log odds to probability of occupancy
    public float obtainProbabilityOccupied(int x, int y)
    {
        // Convert log odds to probability
        float logOdds = map[x, y];
        return 1.0f - 1.0f / (1.0f + Mathf.Exp(logOdds)); // Convert log odds to probability
    }

    public Vector2Int WorldToGrid(float x, float y)
    {
        int xGrid = Mathf.FloorToInt((x - minWidth)  / resolution);
        int yGrid = Mathf.FloorToInt((y - minHeight) / resolution);
        return new Vector2Int(xGrid, yGrid);
    }

    public Vector2 WridToWorld(int x, int y)
    {
        float xWorld = (x * resolution) - maxWidth;
        float yWorld = (y * resolution) - maxHeight;
        return new Vector2(xWorld, yWorld);
    }
}
