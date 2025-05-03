using UnityEngine;
using Unity.Mathematics;

public class Mapping
{
    public float[,] map; // 2D grid array to represent the map

    // Occupancy grid parameters
    public float priorLogOdds = 0.0f; // Prior log odds for each grid cell
    public float freeGridOdds = -0.5f; // Log odds for free grid cells
    public float occupiedGridOdds = 0.5f; // Log odds for occupied grid cells

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

    public initializeMap(int width, int height)
    {
        map = new float[width, height]; // Initialize the map with given dimensions
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                map[i, j] = priorLogOdds; // Set all cells to the prior log odds
            }
        }
    }

    // This function uses the lidar sensors to match the beams to grids
    public Grid[] retrieveGrids(object beams)
    {
        // TODO: Implement LIDAR matching to grid cells.
        return new Grid[0]; // Returning an empty array as a placeholder
    }

    // This function updates the map with the new beams - Occupancy grid mapping
    public void updateMap(object beams)
    {
        Grid[] grids = retrieveGrids(beams);

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
    public obtainProbabilityOccupied(int x, int y)
    {
        // Convert log odds to probability
        float logOdds = map[x, y];
        return 1.0f - 1.0f / (1.0f + Mathf.Exp(logOdds)); // Convert log odds to probability
    }
}
