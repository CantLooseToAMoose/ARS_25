using UnityEngine;

public class ExperimentLauncher : MonoBehaviour
{
    
    public RobotNavigationExperimentController experimentController;

    public void RunExperiment()
    {
        float[][] weightsForAgents = new float[experimentController.numberOfAgents][];
        for (int i = 0; i < experimentController.numberOfAgents; i++)
        {
            weightsForAgents[i] = GenerateRandomWeights(); // or load from file/genetic algorithm etc.
        }

        experimentController.RunExperiment(weightsForAgents);
        
    }

    float[] GenerateRandomWeights()
    {
        float[] w = new float[78];
        for (int i = 0; i < w.Length; i++)
        {
            w[i] = UnityEngine.Random.Range(0f, 1f);
        }

        return w;
    }
    
    void PrintFinalResults()
    {
        foreach (var result in experimentController.results.Values)
        {
            Debug.Log($"Agent {result.AgentId}: Time={result.TimeElapsed:F2}s, Distance={result.TotalDistance:F2}m, Collisions={result.Collisions}, GoalReached={result.GoalReached}");
        }
    }

}