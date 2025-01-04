using System;
using System.Threading;
using MathNet.Numerics.LinearAlgebra;

public class EkfOrientationWithGnss
{
    // State vector [roll, pitch, yaw, x, y, z, vx, vy, vz, bias_gyroX, bias_gyroY, bias_gyroZ]
    Vector<double> x;

    // State covariance matrix
    Matrix<double> P;

    // Process noise covariance matrix
    Matrix<double> Q;

    // Measurement noise covariance matrix for IMU
    Matrix<double> R_imu;

    // Measurement noise covariance matrix for GNSS
    Matrix<double> R_gnss;

    // Identity matrix
    Matrix<double> I;

    // Time step
    const double dt = 0.05;

    public EkfOrientationWithGnss()
    {
        // Initialize state vector
        x = Vector<double>.Build.Dense(12);

        // Initialize state covariance matrix
        P = Matrix<double>.Build.DenseIdentity(12);

        // Initialize process noise covariance matrix
        Q = Matrix<double>.Build.DenseDiagonal(12, 12, 0.001);

        // Initialize measurement noise covariance matrix for IMU
        R_imu = Matrix<double>.Build.DenseDiagonal(3, 3, 0.03);

        // Initialize measurement noise covariance matrix for GNSS
        R_gnss = Matrix<double>.Build.DenseDiagonal(6, 6, 0.05);

        // Initialize identity matrix
        I = Matrix<double>.Build.DenseIdentity(12);
    }

    public void UpdateIMUData(double gx, double gy, double gz, double ax, double ay, double az, double mx, double my, double mz)
    {
        // Predict step
        Predict(gx, gy, gz, ax, ay, az);

        // Update step with IMU data
        UpdateIMU(ax, ay, az, mx, my, mz);
    }

    public void UpdateGNSSData(double px, double py, double pz, double vx, double vy, double vz)
    {
        // Update step with GNSS data
        UpdateGNSS(px, py, pz, vx, vy, vz);
    }

    private void Predict(double gx, double gy, double gz, double ax, double ay, double az)
    {
        double roll = x[0];
        double pitch = x[1];
        double yaw = x[2];
        double vx = x[6];
        double vy = x[7];
        double vz = x[8];
        double bias_gx = x[9];
        double bias_gy = x[10];
        double bias_gz = x[11];

        // Gyroscope measurements with bias correction
        double gx_corrected = gx - bias_gx;
        double gy_corrected = gy - bias_gy;
        double gz_corrected = gz - bias_gz;

        // State transition model for orientation
        double roll_pred = roll + dt * (gx_corrected + gy_corrected * Math.Sin(roll) * Math.Tan(pitch) + gz_corrected * Math.Cos(roll) * Math.Tan(pitch));
        double pitch_pred = pitch + dt * (gy_corrected * Math.Cos(roll) - gz_corrected * Math.Sin(roll));
        double yaw_pred = yaw + dt * (gy_corrected * Math.Sin(roll) / Math.Cos(pitch) + gz_corrected * Math.Cos(roll) / Math.Cos(pitch));

        // State transition model for position and velocity
        double x_pred = x[3] + vx * dt + 0.5 * ax * dt * dt;
        double y_pred = x[4] + vy * dt + 0.5 * ay * dt * dt;
        double z_pred = x[5] + vz * dt + 0.5 * az * dt * dt;

        double vx_pred = vx + ax * dt;
        double vy_pred = vy + ay * dt;
        double vz_pred = vz + az * dt;

        // Update state prediction
        x[0] = roll_pred;
        x[1] = pitch_pred;
        x[2] = yaw_pred;
        x[3] = x_pred;
        x[4] = y_pred;
        x[5] = z_pred;
        x[6] = vx_pred;
        x[7] = vy_pred;
        x[8] = vz_pred;

        // State transition Jacobian matrix
        Matrix<double> F = Matrix<double>.Build.DenseOfArray(new double[,]
        {
            { 1, Math.Sin(roll) * Math.Tan(pitch) * dt, Math.Cos(roll) * Math.Tan(pitch) * dt, 0, 0, 0, 0, 0, 0, -dt, Math.Sin(roll) * Math.Tan(pitch) * dt, Math.Cos(roll) * Math.Tan(pitch) * dt },
            { 0, Math.Cos(roll) * dt, -Math.Sin(roll) * dt, 0, 0, 0, 0, 0, 0, 0, Math.Cos(roll) * dt, -Math.Sin(roll) * dt},
            { 0, Math.Sin(roll) / Math.Cos(pitch) * dt, Math.Cos(roll) / Math.Cos(pitch) * dt, 0, 0, 0, 0, 0, 0, 0, Math.Sin(roll) / Math.Cos(pitch) * dt, Math.Cos(roll) / Math.Cos(pitch) * dt },
            { 0, 0, 0, 1, 0, 0, dt, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 1, 0, 0, dt, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 1, 0, 0, dt, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }
        });

        // Update covariance prediction
        P = F * P * F.Transpose() + Q;
    }

    private void UpdateIMU(double ax, double ay, double az, double mx, double my, double mz)
    {
        // Calculate roll and pitch from accelerometer data
        double accelRoll = Math.Atan2(ay, az);
        double accelPitch = Math.Atan2(-ax, Math.Sqrt(ay * ay + az * az));

        // Calculate yaw from magnetometer data
        double magYaw = Math.Atan2(my, mx);

        // Measurement vector
        Vector<double> z = Vector<double>.Build.DenseOfArray(new double[] { accelRoll, accelPitch, magYaw });

        // Measurement prediction
        Vector<double> h = x.SubVector(0, 3);

        // Measurement Jacobian matrix
        Matrix<double> H = Matrix<double>.Build.Dense(3, 12);
        H[0, 0] = 1;
        H[1, 1] = 1;
        H[2, 2] = 1;

        // Kalman gain
        Matrix<double> S = H * P * H.Transpose() + R_imu;
        Matrix<double> K = P * H.Transpose() * S.Inverse();

        // Update state estimate
        x = x + K * (z - h);

        // Update covariance estimate
        P = (I - K * H) * P;
    }

    private void UpdateGNSS(double px, double py, double pz, double vx, double vy, double vz)
    {
        // Measurement vector
        Vector<double> z = Vector<double>.Build.DenseOfArray(new double[] { px, py, pz, vx, vy, vz });

        // Measurement prediction
        Vector<double> h = x.SubVector(3, 6);

        // Measurement Jacobian matrix
        Matrix<double> H = Matrix<double>.Build.Dense(6, 12);
        H[0, 3] = 1;
        H[1, 4] = 1;
        H[2, 5] = 1;
        H[3, 6] = 1;
        H[4, 7] = 1;
        H[5, 8] = 1;

        // Kalman gain
        Matrix<double> S = H * P * H.Transpose() + R_gnss;
        Matrix<double> K = P * H.Transpose() * S.Inverse();

        // Update state estimate
        x = x + K * (z - h);

        // Update covariance estimate
        P = (I - K * H) * P;
    }

    public void PrintState()
    {
        Console.WriteLine($"Roll: {x[0] * 180.0 / Math.PI} degrees");
        Console.WriteLine($"Pitch: {x[1] * 180.0 / Math.PI} degrees");
        Console.WriteLine($"Yaw: {x[2] * 180.0 / Math.PI} degrees");
        Console.WriteLine($"Position: ({x[3]}, {x[4]}, {x[5]}) meters");
        Console.WriteLine($"Velocity: ({x[6]}, {x[7]}, {x[8]}) meters/second");
    }

    public static void RunTest(string[] args)
    {
        EkfOrientationWithGnss ekf = new EkfOrientationWithGnss();
        // Set a non-zero initial position
        ekf.x[3] = 1000;
        ekf.x[4] = 20000;
        ekf.x[5] = 3000;

        // Simulating continuous IMU and GNSS data update
        while (!Console.KeyAvailable)
        {
            // Example input data (replace with actual sensor readings)
            double gx = 0.00, gy = 0.00, gz = 0.00; // gyroscope values
            double ax = 0.0, ay = 0.0, az = 1.0; // acceleration values
            double mx = 0.3, my = 0.4, mz = 0.0; // magnetometer values
            double px = 1000, py = 20000, pz = 3000; // position in ECEF coordinates
            double vx = 0.0, vy = 0.0, vz = 0.0; // velocity in 3 dimensions

            ekf.UpdateIMUData(gx, gy, gz, ax, ay, az, mx, my, mz);
            ekf.UpdateGNSSData(px, py, pz, vx, vy, vz);
            ekf.PrintState();
            Thread.Sleep(50);
        };

        Console.ReadKey(true);
    }
}
