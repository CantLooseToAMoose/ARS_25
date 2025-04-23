using UnityEngine;
using UnityEditor;

public class GaussianSampler
{
    // Sample from a normal distribution with given mean and standard deviation
    public static float SampleGaussian(float mean = 0f, float stdDev = 1f)
    {
        // Generate two uniform random numbers between 0 and 1
        float u1 = 1.0f - Random.value; // avoid 0
        float u2 = 1.0f - Random.value;

        // Box-Muller transform
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) *
                              Mathf.Sin(2.0f * Mathf.PI * u2);

        // Scale and shift to get desired mean and standard deviation
        return mean + stdDev * randStdNormal;
    }
}