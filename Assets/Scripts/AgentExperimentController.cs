using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SimpleMovement), typeof(Rigidbody))]
public class AgentExperimentController : MonoBehaviour
{
    public Transform goalTransform;
    public float goalThreshold = 1f;

    public RobotNavigationExperimentController ExperimentController;
    public SingleAgentExperimentRunner SingleAgentExperimentRunner;
    public int AgentId { get; set; }
    

    private Vector3 previousPosition;
    private float totalDistance = 0f;
    private float elapsedTime = 0f;
    private int collisionCount = 0;

    public bool goalReached { get; private set; } = false;

    public float obstacleCheckRadius = 1f;
    public LayerMask obstacleLayer;

    private float checkInterval = 0.2f;
    private float nextCheckTime = 0f;

    private NeuralNetController nnController;

    void Start()
    {
        nnController = GetComponent<NeuralNetController>();
        previousPosition = transform.position;
    }
    
    public void StartExperiment(float maxTime)
    {
        StartCoroutine(ExperimentTimeout(maxTime));
    }

    private IEnumerator ExperimentTimeout(float maxTime)
    {
        yield return new WaitForSeconds(maxTime);

        if (!goalReached)
        {
            goalReached = true;
            nnController?.StopControl();
            Debug.Log($"[Agent {AgentId}] Timeout after {maxTime:F2} seconds");
            LogResults();
        }
    }

    void Update()
    {
        if (goalReached) return;

        elapsedTime += Time.deltaTime;

        // Track distance
        float distanceMoved = Vector3.Distance(transform.position, previousPosition);
        totalDistance += distanceMoved;
        previousPosition = transform.position;

        // Check for nearby obstacles periodically
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;

            Collider[] hits = Physics.OverlapSphere(transform.position, obstacleCheckRadius, obstacleLayer);
            if (hits.Length > 0)
            {
                collisionCount++;
            }
        }

        // Check if goal is reached
        if (Vector3.Distance(transform.position, goalTransform.position) < goalThreshold)
        {
            goalReached = true;
            nnController?.StopControl();
            LogResults();
        }
    }

    private void LogResults()
    {
        float finalDistance = Vector3.Distance(transform.position, goalTransform.position);

        AgentExperimentResult result = new AgentExperimentResult
        {
            AgentId = AgentId,
            GoalReached = finalDistance < goalThreshold,
            TimeElapsed = elapsedTime,
            TotalDistance = totalDistance,
            Collisions = collisionCount,
            FinalDistanceToGoal = finalDistance 
        };

        ExperimentController?.SubmitResult(result);
        SingleAgentExperimentRunner?.SubmitResult(result);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, obstacleCheckRadius);
    }
}
