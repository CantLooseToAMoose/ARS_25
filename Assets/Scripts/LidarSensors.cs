using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using UnityEditor;

public class LidarSensors : MonoBehaviour
{
    [Tooltip("The number of Rays simulated")]
    public int numberOfRays;

    [Tooltip("The maximum Length for the Lidar sensors")]
    public float maxLength;

    [Tooltip("The object mask for what should be detected by the Lidar Sensors")]
    public LayerMask sensorMask;

    [Tooltip("Offset of the Lidar sensor from the Center")]
    public Vector3 offset;

    [Tooltip("Distance from center to begin raycasting (to simulate radius)")]
    public float radiusOffset = 0f;

    public RaycastResult[] LastScan { get; private set; }

    #region Debug Settings

    [Header("Debug Settings")] 
    [Tooltip("The radius of the sphere drawn at hit points")]
    public float hitSphereSize = 0.1f;

    [Tooltip("Enable to draw lidar debug gizmos")]
    public bool drawDebugGizmos = true;

    #endregion

    #region Private Values

    private float maxAngle = 180;
    private float minAngle = -180;

    #endregion

    public struct RaycastResult
    {
        public Vector3 direction;
        public bool hit;
        public float distance;
        public Vector3 hitPoint;
    }

    private void Update()
    {
        Vector3 origin = transform.position;
        RaycastResult[] rayResults = PerformRaycasts(origin);
        LastScan = rayResults;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos) return;

        Vector3 origin = transform.position;
        RaycastResult[] rayResults = PerformRaycasts(origin);

#if UNITY_EDITOR
        GUIStyle labelStyle = new GUIStyle
        {
            fontSize = 14,
            normal = { textColor = Color.black }
        };
#endif

        Gizmos.color = Color.green;

        foreach (var result in rayResults)
        {
            Vector3 rayStart = origin + result.direction * radiusOffset; 

            if (result.hit)
            {
                Gizmos.DrawRay(rayStart, result.direction * result.distance);
                Gizmos.DrawSphere(rayStart + result.direction * result.distance, hitSphereSize);
            }
            else
            {
                Gizmos.DrawRay(rayStart, result.direction * result.distance);
            }

#if UNITY_EDITOR
            Vector3 labelPosition = rayStart + result.direction * (result.distance * 0.5f) + Vector3.up * 0.1f;
            Handles.Label(labelPosition, result.distance.ToString("F2") + "m", labelStyle);
#endif
        }
    }

    private RaycastResult[] PerformRaycasts(Vector3 origin)
    {
        Vector3[] directions = GetAllRayDirections();
        RaycastResult[] results = new RaycastResult[directions.Length];

        for (int i = 0; i < directions.Length; i++)
        {
            Vector3 direction = directions[i];

            RaycastHit hit;
            bool hitSomething = Physics.Raycast(origin, direction, out hit, maxLength, sensorMask);

            float rawDistance = hitSomething ? hit.distance : maxLength;
            float adjustedDistance = Mathf.Max(0f, rawDistance - radiusOffset); 

            results[i] = new RaycastResult
            {
                direction = direction,
                hit = hitSomething,
                distance = adjustedDistance,
                hitPoint = origin + direction * rawDistance 
            };
        }

        return results;
    }

    private Vector3[] GetAllRayDirections()
    {
        float rot = transform.rotation.eulerAngles.y;
        Vector3[] directions = new Vector3[numberOfRays];
        float angleStep = (maxAngle - minAngle) / numberOfRays;

        for (int i = 0; i < numberOfRays; i++)
        {
            float angle = rot + minAngle + angleStep * i;
            angle = math.radians(angle);
            directions[i] = new Vector3(math.sin(angle), 0, math.cos(angle));
        }

        return directions;
    }
}
