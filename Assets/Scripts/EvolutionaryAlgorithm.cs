using System;
using System.Collections.Generic;
using UnityEngine;

public class EvolutionaryAlgorithm
{
    // Hyperparameters
    public int PopulationSize { get; set; } = 100;
    public int Generations { get; set; } = 100;
    public float MutationRate { get; set; } = 0.01f;
    public float CrossoverRate { get; set; } = 0.7f;
    public float FitnessThreshold { get; set; } = 0.95f;

    // Individual genome length
    public int GenomeLength { get; set; } = 10;

    private readonly System.Random _rng = new System.Random();

    /// <summary>
    /// Initializes a population of random individuals.
    /// </summary>
    public List<float[]> InitializePopulation()
    {
        var population = new List<float[]>(PopulationSize);
        for (int i = 0; i < PopulationSize; i++)
        {
            var individual = new float[GenomeLength];
            for (int j = 0; j < GenomeLength; j++)
            {
                individual[j] = UnityEngine.Random.Range(-1f, 1f);
            }

            population.Add(individual);
        }

        return population;
    }

    /// <summary>
    /// Evaluates fitness for a single individual.
    /// Replace this with your real evaluation.
    /// </summary>
    private float EvaluateFitness(float[] individual)
    {
        // Placeholder: use your domain-specific fitness calculation here.
        return UnityEngine.Random.Range(0f, 1f);
    }

    /// <summary>
    /// Selects parents by truncated rank-based selection.
    /// </summary>
    public List<float[]> SelectParents(List<float[]> population, List<float> fitnesses, int numParents)
    {
        var paired = new List<(float fitness, float[] genome)>(PopulationSize);
        for (int i = 0; i < PopulationSize; i++)
            paired.Add((fitnesses[i], population[i]));
        paired.Sort((a, b) => b.fitness.CompareTo(a.fitness));

        var parents = new List<float[]>(PopulationSize);

        // Fill exactly PopulationSize
        for (int i = 0; i < PopulationSize; i++)
        {
            // Loop through top-ranked parents in round-robin
            int parentIndex = i % numParents;
            var genomeCopy = (float[])paired[parentIndex].genome.Clone();
            parents.Add(genomeCopy);
        }

        return parents;
    }


    /// <summary>
    /// Applies crossover and mutation to produce the next generation.
    /// </summary>
    public List<float[]> CrossoverAndMutate(List<float[]> population)
    {
        var nextGen = new List<float[]>(PopulationSize);

        for (int i = 0; i < PopulationSize; i++)
        {
            // Start with a copy of parent i
            float[] child = (float[])population[i].Clone();

            // Elitism: keep the best individual
            if (i == 0)
            {
                nextGen.Add(child);
                continue;
            }

            // Crossover
            if (UnityEngine.Random.value < CrossoverRate)
            {
                int mateIndex = UnityEngine.Random.Range(0, PopulationSize);
                float[] mate = population[mateIndex];
                for (int gene = 0; gene < GenomeLength; gene++)
                {
                    if (UnityEngine.Random.value < 0.5f)
                        child[gene] = mate[gene];
                }
            }

            // Mutation
            for (int gene = 0; gene < GenomeLength; gene++)
            {
                if (UnityEngine.Random.value < MutationRate)
                {
                    child[gene] += UnityEngine.Random.Range(-0.1f, 0.1f);
                    // Optional: clamp to [0,1]
                    child[gene] = Mathf.Clamp(child[gene], -1f, 1f);
                }
            }

            nextGen.Add(child);
        }

        return nextGen;
    }

    /// <summary>
    /// Runs the evolutionary algorithm and returns the best-found individual.
    /// </summary>
    public float[] Run()
    {
        var population = InitializePopulation();
        float[] bestIndividual = null;
        float bestFitness = float.MinValue;

        for (int gen = 0; gen < Generations; gen++)
        {
            // Evaluate all individuals
            var fitnesses = new List<float>(PopulationSize);
            for (int i = 0; i < PopulationSize; i++)
            {
                float fit = EvaluateFitness(population[i]);
                fitnesses.Add(fit);

                if (fit > bestFitness)
                {
                    bestFitness = fit;
                    bestIndividual = population[i];
                }
            }

            // Early stopping
            if (bestFitness >= FitnessThreshold)
            {
                Debug.Log($"Threshold reached at generation {gen}: fitness={bestFitness:F3}");
                break;
            }

            // Select parents
            int numParents = Mathf.Max(2, PopulationSize / 5);
            var parents = SelectParents(population, fitnesses, numParents);

            // Generate next generation
            population = CrossoverAndMutate(parents);
        }

        return bestIndividual;
    }
}