using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotNavigationExperimentController : MonoBehaviour
{
    public GameObject agentPrefab;
    public GameObject goalPrefab;
    public int numberOfAgents = 10;
    public float spawnRadius = 10f;
    public float maxExperimentTime = 10f;

    public LayerMask obstacleLayer; // expose in inspector


    private int nextAgentId = 0;
    public Dictionary<int, GameObject> agentsDictionary = new Dictionary<int, GameObject>();

    // Call this externally to start with weight sets
    public void RunExperiment(float[][] allAgentWeights)
    {
        StartCoroutine(RunExperimentRoutine(allAgentWeights));
    }

    private IEnumerator RunExperimentRoutine(float[][] allAgentWeights)
    {
        if (allAgentWeights.Length != numberOfAgents)
        {
            Debug.LogError("Mismatch between number of agents and weight sets.");
            yield break;
        }

        Vector3 sharedSpawnPos;
        Vector3 sharedGoalPos;

        const int maxAttempts = 100;
        int attempt = 0;
        bool foundValidPositions = false;

        do
        {
            sharedSpawnPos = Random.insideUnitSphere * spawnRadius;
            sharedSpawnPos.y = 0f;

            sharedGoalPos = Random.insideUnitSphere * spawnRadius;
            sharedGoalPos.y = 0f;

            float distance = Vector3.Distance(sharedSpawnPos, sharedGoalPos);

            bool spawnClear = !Physics.CheckSphere(sharedSpawnPos, 1.2f, obstacleLayer);
            bool goalClear = !Physics.CheckSphere(sharedGoalPos, 0.6f, obstacleLayer);


            if (distance >= spawnRadius / 3f && spawnClear && goalClear)
            {
                foundValidPositions = true;
            }

            attempt++;

            if (attempt >= maxAttempts)
            {
                Debug.LogWarning("⚠ Failed to find valid spawn/goal positions after many attempts.");
                break;
            }
        } while (!foundValidPositions);

        if (!foundValidPositions)
        {
            yield break; // cancel experiment if valid positions couldn't be found
        }

        for (int i = 0; i < numberOfAgents; i++)
        {
            GameObject agent = Instantiate(agentPrefab, sharedSpawnPos, Quaternion.identity);
            GameObject goal = Instantiate(goalPrefab, sharedGoalPos, Quaternion.identity);

            int agentId = nextAgentId++;
            agentsDictionary.Add(agentId, agent);

            var controller = agent.GetComponent<AgentExperimentController>();
            controller.goalTransform = goal.transform;
            controller.ExperimentController = this;
            controller.AgentId = agentId;

            var nn = agent.GetComponent<NeuralNetController>();
            nn.ExperimentController = controller;
            nn.SetWeights(allAgentWeights[i]);

            agent.name = $"Agent_{agentId}";

            controller.StartExperiment(maxExperimentTime);
        }

        yield return null;
    }


    public Dictionary<int, AgentExperimentResult> results = new Dictionary<int, AgentExperimentResult>();

    public void SubmitResult(AgentExperimentResult result)
    {
        results[result.AgentId] = result;
        Debug.Log($"[ExperimentController] Result received from Agent {result.AgentId}");

        if (results.Count >= numberOfAgents)
        {
            Debug.Log("✅ All agent experiments complete!");
            OnAllExperimentsComplete();
        }
    }


    private void OnAllExperimentsComplete()
    {
        // Notify the evolution manager
        var manager = FindObjectOfType<EvolutionExperimentManager>();
        if (manager != null)
        {
            manager.OnGenerationComplete(results); // ✅ Let the manager handle next round
        }

        // Cleanup
        foreach (var kvp in agentsDictionary)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }

        foreach (var goal in GameObject.FindGameObjectsWithTag("Goal"))
        {
            Destroy(goal);
        }

        agentsDictionary.Clear();
        results.Clear();
        nextAgentId = 0;
    }
}