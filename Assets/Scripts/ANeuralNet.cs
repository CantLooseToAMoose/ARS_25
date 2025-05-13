using System;
using UnityEngine;

public static class ANeuralNet
{
    /// <summary>
    /// feeds forward through an arbitrary MLP with tanh activations everywhere.
    /// </summary>
    /// <param name="inputs">
    ///    your input vector (length = layerSizes[0])
    /// </param>
    /// <param name="weightsAndBiases">
    ///    a single flat array containing, **in order** for each layer l=0…L–1:
    ///      • (layerSizes[l+1] × layerSizes[l]) weights  
    ///      • layerSizes[l+1] biases  
    /// </param>
    /// <param name="layerSizes">
    ///    an int[] of length ≥2, where  
    ///      layerSizes[0] = input size,  
    ///      layerSizes[1-n-2] = hidden sizes,  
    ///      layerSizes[n-1] = output size (must be 2 here)  
    /// </param>
    /// <returns>
    ///    a float[] of length = layerSizes.Last(), with tanh applied to each output
    /// </returns>
    public static float[] FeedForward(
        float[] inputs,
        float[] weightsAndBiases,
        int[] layerSizes
    )
    {
        if (layerSizes == null || layerSizes.Length < 2)
            throw new ArgumentException("Need at least input and output layers");

        int L = layerSizes.Length - 1;            
        if (layerSizes[L] != 2)
            throw new ArgumentException("Final layer size must be 2");

        if (inputs.Length != layerSizes[0])
            throw new ArgumentException($"Expected {layerSizes[0]} inputs");
        
        int required = 0;
        for (int l = 0; l < L; l++)
            required += layerSizes[l] * layerSizes[l+1]   
                      + layerSizes[l+1];                       

        if (weightsAndBiases.Length != required)
            throw new ArgumentException(
              $"Expected {required} parameters, got {weightsAndBiases.Length}");

        // one-pass: read weights/biases, compute each layer
        float[] activation = inputs;
        int idx = 0;

        for (int l = 0; l < L; l++)
        {
            int inSize  = layerSizes[l];
            int outSize = layerSizes[l+1];
            var next    = new float[outSize];

            // for each neuron j in the next layer
            for (int j = 0; j < outSize; j++)
            {
                // dot product
                float sum = 0f;
                for (int i = 0; i < inSize; i++, idx++)
                {
                    sum += weightsAndBiases[idx] * activation[i];
                }

                // bias
                sum += weightsAndBiases[idx++];
                
                // tanh activation on hidden _and_ final
                next[j] = MathF.Tanh(sum);
            }

            activation = next;
        }
        // Debug.Log("Weights: [" + string.Join(", ", weightsAndBiases) + "]");
        // Debug.Log("Inputs: [" + string.Join(", ", inputs) + "]");

        // activation now length==2
        // Debug.Log("ANeuralNet output: [" + string.Join(", ", activation) + "]");

        return activation;
    }
}