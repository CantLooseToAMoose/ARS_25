using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KalManPrediction
{
    // State vector (x, y, theta)
    public Vector3 stateEstimate;

    // Covariance matrix
    public Matrix3x3 covarianceEstimate;

    // Control input (linear velocity and angular velocity)
    private Vector2 control;

    // Observation
    private Vector3 observation;

    // Time step for the prediction model
    public float deltaTime;

    // Noise parameters for motion model
    private float Rx;  // Noise in x direction
    private float Ry;  // Noise in y direction
    private float Rtheta;  // Noise in theta direction

    // State transition matrix (identity for simplicity)
    private Matrix3x3 A = new Matrix3x3(
        1, 0, 0,
        0, 1, 0,
        0, 0, 1
    );

    // Control input matrix (will be computed based on the theta)
    private Matrix3x2 B;

    // Observation matrix (identity for simplicity)
    private Matrix3x3 C = new Matrix3x3(
        1, 0, 0,
        0, 1, 0,
        0, 0, 1
    );

    // Noise covariance matrix for the motion model
    private Matrix3x3 R;

    // Noise covariance matrix for the observation model
    private Matrix3x3 Q;

    // Constructor
    public KalmanPrediction(float Rx = 0.1f, float Ry = 0.1f, float Rtheta = 0.1f, float Qx = 0.1f, float Qy = 0.1f, float Qtheta = 0.1f)
    {
        this.Rx = Rx;
        this.Ry = Ry;
        this.Rtheta = Rtheta;

        // Initialize the noise covariance matrix for the motion model
        this.R = new Matrix3x3(
            Rx, 0, 0,
            0, Ry, 0,
            0, 0, Rtheta
        );

        // Initialize the noise covariance matrix for the observation model
        this.Q = new Matrix3x3(
            Qx, 0, 0,
            0, Qy, 0,
            0, 0, Qtheta
        );
    }

    // Method to compute the control input matrix B based on the current orientation (theta)
    private B ComputeB(float theta)
    {
        return new Matrix3x2(
            deltaTime * (float)Math.Cos(theta), 0,
            deltaTime * (float)Math.Sin(theta), 0,
            0, deltaTime
        );
    }

    // Kalman Filter update function (prediction and correction steps)
    public void KalmanFilter(control control, observation observation){
        stateEstimate prevStateEstimate = this.stateEstimate;
        covarianceEstimate prevCovarianceEstimate = this.covarianceEstimate;
        
        // Prediction step
        (stateEstimate, covarianceEstimate) = PredictionStep(prevStateEstimate, prevCovarianceEstimate, control)

        // Correction step
        (correctedStateEstimate, correctedCovarianceEstimate) = CorrectionStep(stateEstimate, covarianceEstimate, observation);

        // Update the state and covariance estimates
        this.stateEstimate = correctedStateEstimate;
        this.covarianceEstimate = correctedCovarianceEstimate;
    }

    // Prediction step of the Kalman Filter
    private (stateEstimate, covarianceEstimate) PredictionStep(stateEstimate prevStateEstimate, covarianceEstimate prevCovarianceEstimate, control control) {
        (float x, float y, float theta) = prevStateEstimate;

        // State prediction: state' = A * state + B * control
        stateEstimate = A * prevStateEstimate + ComputeB(theta) * control;

        // Covariance prediction: covariance' = A * covariance * A^T + R
        covarianceEstimate = A * prevCovarianceEstimate * A.Transpose() + R;
        
        return (stateEstimate, covarianceEstimate);
    }

    // Correction step of the Kalman Filter
    private (stateEstimate, covarianceEstimate) CorrectionStep(stateEstimate stateEstimate, covarianceEstimate covarianceEstimate, observation observation) {
        // Compute the Kalman Gain
        Matrix3x3 K = covarianceEstimate * C.Transpose() * (C * covarianceEstimate * C.Transpose() + Q).Inverse();

        // Update the state estimate with the observation
        correctedStateEstimate = stateEstimate + K * (observation - C * stateEstimate);

        // Update the covariance estimate
        correctedCovarianceEstimate = (Matrix3x3.Identity - K * C) * covarianceEstimate;
        
        return (correctedStateEstimate, correctedCovarianceEstimate)
    }
}
