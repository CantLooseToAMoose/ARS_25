using UnityEngine;
using Unity.Mathematics;



public class KalmanPrediction
{
    public float3 stateEstimate;
    public float3x3 covarianceEstimate;

    public float deltaTime;


    private float3x3 A = float3x3.identity;
    private float3x3 C = float3x3.identity;

    private float3x3 R;
    private float3x3 Q;

    public KalmanPrediction(
        float deltaTime = 0.1f,
        float Rx = 0.1f,
        float Ry = 0.1f,
        float Rtheta = 0.1f,
        float Qx = 0.1f,
        float Qy = 0.1f,
        float Qtheta = 0.1f
    )
    {
        this.deltaTime = deltaTime;

        R = new float3x3(
            Rx, 0f, 0f,
            0f, Ry, 0f,
            0f, 0f, Rtheta
        );

        Q = new float3x3(
            Qx, 0f, 0f,
            0f, Qy, 0f,
            0f, 0f, Qtheta
        );

        stateEstimate = float3.zero;
        covarianceEstimate = 10*float3x3.identity;
    }

    private float3x2 ComputeB(float theta)
    {
        return new float3x2(
            deltaTime * math.cos(theta), 0f,
            deltaTime * math.sin(theta), 0f,
            0f, deltaTime
        );
    }

    public (float3, float3x3) PredictionStep(float3 prevState, float3x3 prevCov, float2 control)
    {
        float theta = prevState.z;
        float3x2 B = ComputeB(theta);

        float3 predictedState = math.mul(A, prevState) + math.mul(B, control);
        float3x3 predictedCov = math.mul(math.mul(A, prevCov), math.transpose(A)) + R;

        return (predictedState, predictedCov);
    }

    public (float3, float3x3) CorrectionStep(float3 predictedState, float3x3 predictedCov, float3 observation)
    {
        float3x3 S = math.mul(math.mul(C, predictedCov), math.transpose(C)) + Q;
        float3x3 K = math.mul(math.mul(predictedCov, math.transpose(C)), math.inverse(S));

        float3 updatedState = predictedState + math.mul(K, observation - math.mul(C, predictedState));
        float3x3 updatedCov = math.mul(float3x3.identity - math.mul(K, C), predictedCov);

        return (updatedState, updatedCov);
    }
}
