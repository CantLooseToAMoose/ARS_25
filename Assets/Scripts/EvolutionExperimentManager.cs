using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionExperimentManager : MonoBehaviour
{
    public RobotNavigationExperimentController experimentController;
    public int populationSize = 5;
    public int genomeLength = 70;
    public float mutationRate = 0.01f;
    public float crossOverRate = 0.7f;
    public float fitnessThreshold = 0.95f;
    public int maxGenerations = 15;
    public int numParents = 5;

    public float timeScale = 5f;

    private int currentGeneration = 0;
    private List<float[]> population;
    private EvolutionaryAlgorithm ea;

    private float[] bestIndividual; // best across all generations
    private float bestFitness = float.MinValue;

    private List<FitnessLogEntry> fitnessLog = new List<FitnessLogEntry>();

    public void StartEvolutionaryAlgorithm()
    {
        ea = new EvolutionaryAlgorithm
        {
            PopulationSize = populationSize,
            Generations = maxGenerations,
            GenomeLength = genomeLength,
            MutationRate = mutationRate,
            CrossoverRate = crossOverRate,
            FitnessThreshold = fitnessThreshold
        };
        experimentController.numberOfAgents = populationSize;
        population = ea.InitializePopulation();
        StartGeneration();
    }

    private void StartGeneration()
    {
        Time.timeScale = timeScale;
        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(0.5f);
        Debug.Log($"🚀 Starting generation {currentGeneration + 1}/{maxGenerations}");

        float[][] batchWeights = new float[experimentController.numberOfAgents][];
        for (int i = 0; i < experimentController.numberOfAgents; i++)
        {
            batchWeights[i] = population[i];
        }

        experimentController.RunExperiment(batchWeights);
    }

    public bool IsDominated(Dictionary<int, AgentExperimentResult> results, int agentIdx)
    {
        var totalDistance = results[agentIdx].TotalDistance;
        var timeElapsed = results[agentIdx].TimeElapsed;
        var collisionCount = results[agentIdx].Collisions;

        for (int i = 0; i < results.Count; i++)
        {
            if (i == agentIdx) continue;

            var otherTotalDistance = results[i].TotalDistance;
            var otherTimeElapsed = results[i].TimeElapsed;
            var otherCollisionCount = results[i].Collisions;

            if (otherTotalDistance > totalDistance && otherTimeElapsed < timeElapsed &&
                otherCollisionCount <= collisionCount)
            {
                return true;
            }
        }

        return false;
    }

    public void OnGenerationCompleteMultiObj(Dictionary<int, AgentExperimentResult> results)
    {
        var nonDominated = new List<float[]>();

        for (int i = 0; i < results.Count; i++)
        {
            if (!IsDominated(results, i))
            {
                nonDominated.Add(population[i]);
            }
        }

        var newPopulation = new List<float[]>(populationSize);

        for (int i = 0; i < populationSize; i++)
        {
            if (i < nonDominated.Count)
            {
                newPopulation.Add(nonDominated[i]);
            }
            else
            {
                int parent1Index = UnityEngine.Random.Range(0, nonDominated.Count);
                int parent2Index = UnityEngine.Random.Range(0, nonDominated.Count);
                float[] parent1 = nonDominated[parent1Index];
                float[] parent2 = nonDominated[parent2Index];
                float[] child = new float[genomeLength];

                for (int gene = 0; gene < genomeLength; gene++)
                {
                    child[gene] = UnityEngine.Random.value < 0.5f ? parent1[gene] : parent2[gene];
                }

                for (int gene = 0; gene < genomeLength; gene++)
                {
                    if (UnityEngine.Random.value < mutationRate)
                    {
                        child[gene] += UnityEngine.Random.Range(-1f, 1f);
                    }
                }

                newPopulation.Add(child);
            }
        }

        population = newPopulation;
        currentGeneration++;

        if (currentGeneration < maxGenerations)
        {
            StartGeneration();
        }
        else
        {
            Time.timeScale = 1f;
            Debug.Log("🎉 Evolution complete");
        }
    }

    public void OnGenerationComplete(Dictionary<int, AgentExperimentResult> results)
    {
        Debug.Log($"✅ Generation {currentGeneration + 1} complete");

        var fitnesses = new List<float>(populationSize);
        float generationBestFitness = float.NegativeInfinity;
        float[] bestOfCurrentGeneration = null;

        for (int i = 0; i < experimentController.numberOfAgents; i++)
        {
            var result = results[i];
            float fitness = CalculateFitness(result);
            result.Fitness = fitness;
            fitnesses.Add(fitness);

            if (fitness > bestFitness)
            {
                bestFitness = fitness;
                bestIndividual = population[i];
            }

            if (fitness > generationBestFitness)
            {
                generationBestFitness = fitness;
                bestOfCurrentGeneration = population[i];
            }
        }

        Debug.Log($"🏅 Generation {currentGeneration + 1} best fitness: {generationBestFitness:F4}");

        var parents = ea.SelectParents(population, fitnesses, numParents);
        population = ea.CrossoverAndMutate(parents);

        currentGeneration++;

        fitnessLog.Add(new FitnessLogEntry
        {
            Generation = currentGeneration,
            Fitnesses = new List<float>(fitnesses)
        });

        if (currentGeneration % 50 == 0)
        {
            ExportBestIndividualToCSV($"best_weights_gen_{currentGeneration}.csv", bestOfCurrentGeneration, generationBestFitness);
            ExportFitnessLogToCSV("fitness_log.csv");
        }

        if (currentGeneration < maxGenerations)
        {
            StartGeneration();
        }
        else
        {
            Time.timeScale = 1f;
            Debug.Log("🎉 Evolution complete");
            ExportFitnessLogToCSV("fitness_log.csv");
            ExportBestIndividualToCSV("best_weights_final_generation.csv", bestOfCurrentGeneration, generationBestFitness);
        }
    }

    private void ExportFitnessLogToCSV(string filename)
    {
        string path = System.IO.Path.Combine(Application.dataPath, filename);
        using (System.IO.StreamWriter writer = new System.IO.StreamWriter(path))
        {
            writer.WriteLine("Generation;Fitness");

            foreach (var entry in fitnessLog)
            {
                foreach (var fitness in entry.Fitnesses)
                {
                    writer.WriteLine($"{entry.Generation};{fitness}");
                }
            }
        }

        Debug.Log($"📄 Fitness log exported to: {path}");
    }

    private void ExportBestIndividualToCSV(string filename, float[] individual, float fitness)
    {
        if (individual == null) return;

        string path = System.IO.Path.Combine(Application.dataPath, filename);
        using (System.IO.StreamWriter writer = new System.IO.StreamWriter(path))
        {
            writer.WriteLine("GeneIndex;Weight");
            for (int i = 0; i < individual.Length; i++)
            {
                writer.WriteLine($"{i}; {individual[i]}");
            }
        }

        Debug.Log($"🏁 Final generation best weights saved: {path} (Fitness: {fitness:F3})");
    }

    private float CalculateFitness(AgentExperimentResult result)
    {
        float fitness = 0f;

        if (result.GoalReached)
        {
            fitness += 15f;
        }
        else
        {
            float normalizedDistance = Mathf.Clamp01(result.FinalDistanceToGoal / experimentController.spawnRadius);
            fitness += 1f - normalizedDistance;
        }

        fitness += Mathf.Clamp01(1f - (result.TimeElapsed / experimentController.maxExperimentTime));
        fitness -= 0.01f * result.Collisions;

        return fitness;
    }
}

public class FitnessLogEntry
{
    public int Generation;
    public List<float> Fitnesses;
}
