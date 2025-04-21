using System;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;

public class KalmanPrediction
{
    // State vector [x, y, theta]
    public Vector<float> stateEstimate;

    // Covariance matrix
    public Matrix<float> covarianceEstimate;

    // Time step
    public float deltaTime;

    // Matrices
    private Matrix<float> A;
    private Matrix<float> C;
    private Matrix<float> R;
    private Matrix<float> Q;

    // Constructor
    public KalmanPrediction(float deltaTime = 0.1f, float Rx = 0.1f, float Ry = 0.1f, float Rtheta = 0.1f, float Qx = 0.1f, float Qy = 0.1f, float Qtheta = 0.1f)
    {
        this.deltaTime = deltaTime;
        this.Rx = Rx;
        this.Ry = Ry;
        this.Rtheta = Rtheta;

        // Initial state and covariance
        stateEstimate = Vector<float>.Build.DenseOfArray(new float[] { 0f, 0f, 0f });
        covarianceEstimate = Matrix<float>.Build.DenseIdentity(3);

        // State transition matrix (identity)
        A = Matrix<float>.Build.DenseIdentity(3);

        // Observation matrix (identity)
        C = Matrix<float>.Build.DenseIdentity(3);

        // Motion noise covariance
        R = Matrix<float>.Build.DenseOfArray(new float[,] {
            { Rx, 0f, 0f },
            { 0f, Ry, 0f },
            { 0f, 0f, Rtheta }
        });

        // Observation noise covariance
        Q = Matrix<float>.Build.DenseOfArray(new float[,] {
            { Qx, 0f, 0f },
            { 0f, Qy, 0f },
            { 0f, 0f, Qtheta }
        });
    }

    // Compute control matrix B
    private Matrix<float> ComputeB(float theta)
    {
        return Matrix<float>.Build.DenseOfArray(new float[,] {
            { deltaTime * Mathf.Cos(theta), 0f },
            { deltaTime * Mathf.Sin(theta), 0f },
            { 0f, deltaTime }
        });
    }

    // Kalman Filter update function
    public void KalmanFilter(Vector<float> control, Vector<float> observation)
    {
        // Prediction
        var (predState, predCov) = PredictionStep(stateEstimate, covarianceEstimate, control);

        // Correction
        var (corrState, corrCov) = CorrectionStep(predState, predCov, observation);

        // Update state
        stateEstimate = corrState;
        covarianceEstimate = corrCov;
    }

    // Prediction step
    private (Vector<float>, Matrix<float>) PredictionStep(Vector<float> prevState, Matrix<float> prevCov, Vector<float> control)
    {
        float theta = prevState[2];
        Matrix<float> B = ComputeB(theta);

        Vector<float> predictedState = A * prevState + B * control;
        Matrix<float> predictedCovariance = A * prevCov * A.Transpose() + R;

        return (predictedState, predictedCovariance);
    }

    // Correction step
    private (Vector<float>, Matrix<float>) CorrectionStep(Vector<float> predictedState, Matrix<float> predictedCov, Vector<float> observation)
    {
        Matrix<float> S = C * predictedCov * C.Transpose() + Q;
        Matrix<float> K = predictedCov * C.Transpose() * S.Inverse();

        Vector<float> updatedState = predictedState + K * (observation - C * predictedState);
        Matrix<float> updatedCovariance = (Matrix<float>.Build.DenseIdentity(3) - K * C) * predictedCov;

        return (updatedState, updatedCovariance);
    }
}
