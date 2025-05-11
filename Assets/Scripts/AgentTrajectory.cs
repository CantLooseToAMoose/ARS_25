using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteAlways] // Allows drawing in Edit mode too
public class AgentTrajectory : MonoBehaviour
{
    [Header("Trajectory Settings")] public float minDistance = 0.1f; // Minimum distance between recorded points
    public float dotSpacing = 4f; // Spacing between dots in pixels

    private List<Vector3> trajectoryPoints = new List<Vector3>();
    private Vector3 lastPosition;

    public bool debug;

    private void Start()
    {
        trajectoryPoints.Clear();
        lastPosition = transform.position;
        trajectoryPoints.Add(lastPosition);
    }

    private void Update()
    {
        if (Vector3.Distance(transform.position, lastPosition) > minDistance)
        {
            trajectoryPoints.Add(transform.position);
            lastPosition = transform.position;
        }
    }

    private void OnDrawGizmos()
    {
        if (!debug)
        {
            return;
        }
#if UNITY_EDITOR
        if (trajectoryPoints.Count < 2) return;

        Handles.color = Color.blue;

        for (int i = 0; i < trajectoryPoints.Count - 1; i++)
        {
            Handles.DrawDottedLine(trajectoryPoints[i], trajectoryPoints[i + 1], dotSpacing);
        }
#endif
    }

    public void ClearTrajectory()
    {
        trajectoryPoints.Clear();
        lastPosition = transform.position;
        trajectoryPoints.Add(lastPosition);
    }
}