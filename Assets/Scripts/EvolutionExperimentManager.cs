﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionExperimentManager : MonoBehaviour
{
    public RobotNavigationExperimentController experimentController;
    public int populationSize = 5;
    public int genomeLength = 78;
    public float mutationRate = 0.01f;
    public float crossOverRate = 0.7f;
    public float fitnessThreshold = 0.95f;
    public int maxGenerations = 15;

    public float timeScale = 5f;

    private int currentGeneration = 0;
    private List<float[]> population;
    private EvolutionaryAlgorithm ea;

    private float[] bestIndividual;
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
        Time.timeScale = timeScale; // or higher (10, 20...), depending on your system performance

        StartCoroutine(DelayedStart());
    }

    private IEnumerator DelayedStart()
    {
        yield return new WaitForSeconds(0.5f); // delay to let cleanup finish
        Debug.Log($"🚀 Starting generation {currentGeneration + 1}/{maxGenerations}");

        float[][] batchWeights = new float[experimentController.numberOfAgents][];
        for (int i = 0; i < experimentController.numberOfAgents; i++)
        {
            batchWeights[i] = population[i];
        }

        experimentController.RunExperiment(batchWeights);
    }


    public void OnGenerationComplete(Dictionary<int, AgentExperimentResult> results)
    {
        Debug.Log($"✅ Generation {currentGeneration + 1} complete");

        var fitnesses = new List<float>(populationSize);
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
        }

        int numParents = Mathf.Max(2, populationSize / 2);
        var parents = ea.SelectParents(population, fitnesses, numParents);
        population = ea.CrossoverAndMutate(parents);

        currentGeneration++;

        fitnessLog.Add(new FitnessLogEntry
        {
            Generation = currentGeneration,
            Fitnesses = new List<float>(fitnesses)
        });

        if (currentGeneration < maxGenerations)
        {
            StartGeneration();
        }
        else
        {
            Time.timeScale = 1f;
            Debug.Log("🎉 Evolution complete");
            ExportFitnessLogToCSV("fitness_log.csv");
            ExportBestIndividualToCSV("best_weights.csv");
        }
    }

    private void ExportFitnessLogToCSV(string filename)
    {
        string path = System.IO.Path.Combine(Application.dataPath, filename);
        using (System.IO.StreamWriter writer = new System.IO.StreamWriter(path))
        {
            // Header
            writer.WriteLine("Generation,Fitness");

            foreach (var entry in fitnessLog)
            {
                foreach (var fitness in entry.Fitnesses)
                {
                    writer.WriteLine($"{entry.Generation},{fitness}");
                }
            }
        }

        Debug.Log($"📄 Fitness log exported to: {path}");
    }

    private void ExportBestIndividualToCSV(string filename)
    {
        if (bestIndividual == null) return;

        string path = System.IO.Path.Combine(Application.dataPath, filename);
        using (System.IO.StreamWriter writer = new System.IO.StreamWriter(path))
        {
            writer.WriteLine("GeneIndex,Weight");
            for (int i = 0; i < bestIndividual.Length; i++)
            {
                writer.WriteLine($"{i},{bestIndividual[i]}");
            }
        }

        Debug.Log($"🏆 Best weights exported to: {path} (Fitness: {bestFitness:F3})");
    }


    private float CalculateFitness(AgentExperimentResult result)
    {
        float fitness = 0f;

        if (result.GoalReached)
        {
            fitness += 2f;
        }
        else
        {
            // Penalize based on final distance to goal (normalized)
            float normalizedDistance = Mathf.Clamp01(result.FinalDistanceToGoal / experimentController.spawnRadius);
            fitness -= normalizedDistance; // penalize large distances
        }

        // Reward faster solutions
        // fitness += Mathf.Clamp01(1f - (result.TimeElapsed / experimentController.maxExperimentTime));

        // Penalize collisions
        fitness -= 0.1f * result.Collisions;

        return Mathf.Max(0f, fitness);
    }
}

public class FitnessLogEntry
{
    public int Generation;
    public List<float> Fitnesses;
}