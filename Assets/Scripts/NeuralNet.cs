using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor; 




public class NeuralNet{
    const int InputSize  = 16;
    const int HiddenSize =  4;
    const int OutputSize =  2;
    const int WeightsIH  = InputSize  * HiddenSize;  //  64
    const int WeightsHO  = HiddenSize * OutputSize; //   8
    const int TotalWeights = WeightsIH + WeightsHO; //  72
    
    public static float[] FeedForward(float[] inputs, float[] weights) //TODO incorporate the bias should be straightforward but logic remains the same.
    {
        // so first I have to subdivide the weights into their separate arrays for the "dot products."
        //architecture: I have input of size 12+4 == 16 so for the weights I first need 4*[16] to go to hidden and then from hidden to output I need 2*[4] makes weights needed == 72
        //Intermediates or not I can also just loop once and do a running dot product and store the results on the go each time I pass 16 weights same applies to hidden to ouput
        if (inputs.Length  != InputSize) 
            throw new ArgumentException($"Expected {InputSize} inputs");
        if (weights.Length != TotalWeights) 
            throw new ArgumentException($"Expected {TotalWeights} weights");
        
        float[] hidden = new float[HiddenSize]; // [4] hidden layer
        int idx = 0;
        for (int j = 0; j < HiddenSize; j++)
        {
            float sum = 0f;

            for (int i = 0; i < InputSize; i++, idx++)
            {
                sum += weights[idx] * inputs[i];
            }
            hidden[j] = Sigmoid(sum);
        }

        // 2) hidden → output
        float[] outputs = new float[OutputSize];
        for (int k = 0; k < OutputSize; k++)
        {
            float sum = 0f;
            // dot product of row k in the HO matrix
            for (int j = 0; j < HiddenSize; j++, idx++)
            {
                sum += weights[idx] * hidden[j];
            }
            outputs[k] = MathF.Tanh(sum);
        }

        return outputs;
    }

    public static float Sigmoid(float x) //had a chat with the GPT and it warned me for overflow so I changed it to its recommendation so this never overflows since it always
    // picks the formule in a way where your large preactivation you always take the negative exponential of it,
    {
        if (x >= 0f)
        {
            float z = MathF.Exp(-x);
            return 1f / (1f + z);
        }
        else
        {
            float z = MathF.Exp(x);
            return z / (1f + z);
        }
    }

    
}
