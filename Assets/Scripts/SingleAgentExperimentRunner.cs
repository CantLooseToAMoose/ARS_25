using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class SingleAgentExperimentRunner : MonoBehaviour
{
    public GameObject agentPrefab;
    public GameObject goalPrefab;
    public float spawnRadius = 10f;
    public float maxExperimentTime = 10f;
    public LayerMask obstacleLayer;
    public int batchSize = 10;
    public int totalTrials = 100;

    public string outputCsvFileName = "SingleAgentResults.csv";

    private List<AgentExperimentResult> results = new List<AgentExperimentResult>();

    private int completedCount = 0;
    private int currentBatchSize = 0;

    public void RunExperiments()
    {
        StartCoroutine(RunBatchedExperiments(totalTrials, batchSize));
    }

    private IEnumerator RunBatchedExperiments(int totalTrials, int batchSize)
    {
        int trialsRun = 0;
        int trialId = 0;

        while (trialsRun < totalTrials)
        {
            int trialsThisBatch = Mathf.Min(batchSize, totalTrials - trialsRun);
            currentBatchSize = trialsThisBatch;
            completedCount = 0;

            for (int i = 0; i < trialsThisBatch; i++)
            {
                StartCoroutine(RunSingleExperiment(trialId++));
                trialsRun++;
            }

            while (completedCount < currentBatchSize)
            {
                yield return null;
            }
        }

        SaveResultsToCsv();
    }

    private IEnumerator RunSingleExperiment(int trialId)
    {
        Vector3 spawnPos, goalPos;
        int attempt = 0;
        const int maxAttempts = 100;
        bool found = false;

        do
        {
            spawnPos = Random.insideUnitSphere * spawnRadius;
            spawnPos.y = 1f;
            goalPos = Random.insideUnitSphere * spawnRadius;
            goalPos.y = 1f;

            float distance = Vector3.Distance(spawnPos, goalPos);
            bool spawnClear = !Physics.CheckSphere(spawnPos, 1.2f, obstacleLayer);
            bool goalClear = !Physics.CheckSphere(goalPos, 0.6f, obstacleLayer);

            if (distance >= spawnRadius / 3f && spawnClear && goalClear)
            {
                found = true;
                break;
            }

            attempt++;
        } while (attempt < maxAttempts);

        if (!found)
        {
            Debug.LogWarning($"⚠ Trial {trialId}: Could not find valid spawn/goal positions.");
            completedCount++;
            yield break;
        }

        GameObject agent = Instantiate(agentPrefab, spawnPos, Quaternion.identity);
        GameObject goal = Instantiate(goalPrefab, goalPos, Quaternion.identity);

        var controller = agent.GetComponent<AgentExperimentController>();
        controller.goalTransform = goal.transform;
        controller.AgentId = trialId;
        controller.SingleAgentExperimentRunner = this;

        var nn = agent.GetComponent<NeuralNetController>();
        nn.ExperimentController = controller;

        controller.StartExperiment(maxExperimentTime);

        // We don't need to wait here; SubmitResult() will be called later
        yield return null;
    }

    public void SubmitResult(AgentExperimentResult result)
    {
        results.Add(result);
        completedCount++;
    }

    private void SaveResultsToCsv()
    {
        string path = Path.Combine(Application.dataPath, outputCsvFileName);
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.WriteLine("AgentId,GoalReached,TimeElapsed,TotalDistance,Collisions,FinalDistanceToGoal,Fitness");

            foreach (var result in results)
            {
                writer.WriteLine($"{result.AgentId},{result.GoalReached},{result.TimeElapsed},{result.TotalDistance},{result.Collisions},{result.FinalDistanceToGoal},{result.Fitness}");
            }
        }

        Debug.Log($"✅ Results saved to CSV: {path}");
    }
}
