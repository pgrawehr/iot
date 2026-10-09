using System;
using System.Threading;
using MathNet.Numerics.LinearAlgebra;
using UnitsNet;
using UnitsNet.Units;
using static System.Math;

namespace DisplayControl;

/// <summary>
/// Extended Kalman Filter for various input sensors: GNSS, attitude and magnetometer are currently supported.
/// </summary>
public class EkfOrientationWithGnss
{
    // Average radius of the earth, in meters
    public const double EARTH_RADIUS = 6378137.0;

    // State vector [qx, qy, qz, qw, x, y, z, vx, vy, vz, bias_gyroX, bias_gyroY, bias_gyroZ]
    MathNet.Numerics.LinearAlgebra.Vector<double> x;

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
        x = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfArray(new double[] { 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

        // Initialize state covariance matrix
        P = Matrix<double>.Build.DenseIdentity(13);

        // Initialize process noise covariance matrix
        Q = Matrix<double>.Build.DenseDiagonal(13, 13, 0.001);

        // Initialize measurement noise covariance matrix for IMU
        R_imu = Matrix<double>.Build.DenseDiagonal(6, 6, 0.03);

        // Initialize measurement noise covariance matrix for GNSS
        R_gnss = Matrix<double>.Build.DenseDiagonal(6, 6, 0.05);

        // Initialize identity matrix
        I = Matrix<double>.Build.DenseIdentity(13);
    }

    public void UpdateIMUData(double gx, double gy, double gz, double ax, double ay, double az, double mx, double my, double mz, double inclination, double declination)
    {
        // Predict step
        Predict(gx, gy, gz, ax, ay, az);

        // Update step with IMU data
        UpdateIMU(ax, ay, az, mx, my, mz, inclination, declination);
    }

    public void UpdateGNSSData(double px, double py, double pz, double vx, double vy, double vz)
    {
        // Update step with GNSS data
        UpdateGNSS(px, py, pz, vx, vy, vz);
    }

    private void Predict(double gx, double gy, double gz, double ax, double ay, double az)
    {
        double qx = x[0];
        double qy = x[1];
        double qz = x[2];
        double qw = x[3];
        double vx = x[7];
        double vy = x[8];
        double vz = x[9];
        double bias_gx = x[10];
        double bias_gy = x[11];
        double bias_gz = x[12];

        // Gyroscope measurements with bias correction
        double gx_corrected = gx - bias_gx;
        double gy_corrected = gy - bias_gy;
        double gz_corrected = gz - bias_gz;

        // Quaternion derivative
        double dq0 = 0.5 * (-qx * gx_corrected - qy * gy_corrected - qz * gz_corrected);
        double dq1 = 0.5 * (qw * gx_corrected + qy * gz_corrected - qz * gy_corrected);
        double dq2 = 0.5 * (qw * gy_corrected - qx * gz_corrected + qz * gx_corrected);
        double dq3 = 0.5 * (qw * gz_corrected + qx * gy_corrected - qy * gx_corrected);

        // Update quaternion
        double qx_pred = qx + dq1 * dt;
        double qy_pred = qy + dq2 * dt;
        double qz_pred = qz + dq3 * dt;
        double qw_pred = qw + dq0 * dt;

        // Normalize quaternion
        double norm = Math.Sqrt(qx_pred * qx_pred + qy_pred * qy_pred + qz_pred * qz_pred + qw_pred * qw_pred);
        qx_pred /= norm;
        qy_pred /= norm;
        qz_pred /= norm;
        qw_pred /= norm;

        // State transition model for position and velocity
        double x_pred = x[4] + vx * dt + 0.5 * ax * dt * dt;
        double y_pred = x[5] + vy * dt + 0.5 * ay * dt * dt;
        double z_pred = x[6] + vz * dt + 0.5 * az * dt * dt;

        double vx_pred = vx + ax * dt;
        double vy_pred = vy + ay * dt;
        double vz_pred = vz + az * dt;

        // Update state prediction
        x[0] = qx_pred;
        x[1] = qy_pred;
        x[2] = qz_pred;
        x[3] = qw_pred;
        x[4] = x_pred;
        x[5] = y_pred;
        x[6] = z_pred;
        x[7] = vx_pred;
        x[8] = vy_pred;
        x[9] = vz_pred;

        // State transition Jacobian matrix
        Matrix<double> F = Matrix<double>.Build.DenseOfArray(new double[,]
        {
            { 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, -0.5 * qx * dt, -0.5 * qy * dt, -0.5 * qz * dt },
            { 0, 1, 0, 0, 0, 0, 0, 0, 0, 0,  0.5 * qw * dt, -0.5 * qz * dt,  0.5 * qy * dt },
            { 0, 0, 1, 0, 0, 0, 0, 0, 0, 0,  0.5 * qz * dt,  0.5 * qw * dt, -0.5 * qx * dt },
            { 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, -0.5 * qy * dt,  0.5 * qx * dt,  0.5 * qw * dt },
            { 0, 0, 0, 0, 1, 0, 0, dt, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 1, 0, 0, dt, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 1, 0, 0, dt, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0 },
            { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 }
        });

        // Update covariance prediction
        P = F * P * F.Transpose() + Q;
    }

    private void UpdateIMU(double ax, double ay, double az, double mx, double my, double mz, double inclination, double declination)
    {
        // Quaternion components
        double qx = x[0];
        double qy = x[1];
        double qz = x[2];
        double qw = x[3];

        // Rotation matrix from quaternion
        Matrix<double> R = Matrix<double>.Build.DenseOfArray(new double[,]
        {
            { 1 - 2 * (qy * qy + qz * qz), 2 * (qx * qy - qz * qw), 2 * (qx * qz + qy * qw) },
            { 2 * (qx * qy + qz * qw), 1 - 2 * (qx * qx + qz * qz), 2 * (qy * qz - qx * qw) },
            { 2 * (qx * qz - qy * qw), 2 * (qy * qz + qx * qw), 1 - 2 * (qx * qx + qy * qy) }
        });

        // Expected gravity vector in body frame
        MathNet.Numerics.LinearAlgebra.Vector<double> g = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfArray(new double[] { 0, 0, 1 });
        MathNet.Numerics.LinearAlgebra.Vector<double> g_pred = R * g;

        // Compute the expected magnetic field vector in the earth frame
        double Bx = Math.Cos(inclination) * Math.Cos(declination);
        double By = Math.Cos(inclination) * Math.Sin(declination);
        double Bz = Math.Sin(inclination);

        // Expected magnetic field vector in body frame
        Vector<double> m_pred = R * Vector<double>.Build.DenseOfArray(new double[] { Bx, By, Bz });


        // Measurement vector
        MathNet.Numerics.LinearAlgebra.Vector<double> z = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfArray(new double[] { ax, ay, az, mx, my, mz });

        // Measurement prediction
        MathNet.Numerics.LinearAlgebra.Vector<double> h = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfArray(new double[] { g_pred[0], g_pred[1], g_pred[2], m_pred[0], m_pred[1], m_pred[2] });

        // Measurement Jacobian matrix
        Matrix<double> H = Matrix<double>.Build.Dense(6, 13);
        H.SetRow(0, new double[] { 2 * qy, -2 * qz, 2 * qw, -2 * qx, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
        H.SetRow(1, new double[] { -2 * qx, -2 * qw, -2 * qz, -2 * qy, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
        H.SetRow(2, new double[] { 0, 2 * qx, 2 * qy, 2 * qz, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
        H.SetRow(3, new double[] { 2 * qw, 2 * qx, 2 * qy, 2 * qz, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
        H.SetRow(4, new double[] { -2 * qz, 2 * qy, 2 * qx, 2 * qw, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
        H.SetRow(5, new double[] { 2 * qy, 2 * qz, 2 * qw, 2 * qx, 0, 0, 0, 0, 0, 0, 0, 0, 0 });

        // Kalman gain
        Matrix<double> S = H * P * H.Transpose() + R_imu;
        Matrix<double> K = P * H.Transpose() * S.Inverse();

        // Update state estimate
        x = x + K * (z - h);

        // Normalize quaternion
        double norm = Math.Sqrt(x[0] * x[0] + x[1] * x[1] + x[2] * x[2] + x[3] * x[3]);
        x[0] /= norm;
        x[1] /= norm;
        x[2] /= norm;
        x[3] /= norm;

        // Update covariance estimate
        P = (I - K * H) * P;
    }

    private void UpdateGNSS(double px, double py, double pz, double vx, double vy, double vz)
    {
        // Measurement vector
        MathNet.Numerics.LinearAlgebra.Vector<double> z = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.DenseOfArray(new double[] { px, py, pz, vx, vy, vz });

        // Measurement prediction
        MathNet.Numerics.LinearAlgebra.Vector<double> h = x.SubVector(4, 6);

        // Measurement Jacobian matrix
        Matrix<double> H = Matrix<double>.Build.Dense(6, 13);
        H[0, 4] = 1;
        H[1, 5] = 1;
        H[2, 6] = 1;
        H[3, 7] = 1;
        H[4, 8] = 1;
        H[5, 9] = 1;

        // Kalman gain
        Matrix<double> S = H * P * H.Transpose() + R_gnss;
        Matrix<double> K = P * H.Transpose() * S.Inverse();

        // Update state estimate
        x = x + K * (z - h);

        // Update covariance estimate
        P = (I - K * H) * P;
    }

    private void GetEulerAngles(double qx, double qy, double qz, double qw, out Angle ayaw, out Angle apitch, out Angle aroll)
    {
        double w2 = qw * qw;
        double x2 = qx * qx;
        double y2 = qy * qy;
        double z2 = qz * qz;
        double unitLength = w2 + x2 + y2 + z2;    // Normalised == 1, otherwise correction divisor.
        double abcd = qw * qx + qy * qz;
        double eps = 2 * Double.Epsilon;
        double yaw, pitch, roll;
        if (abcd > (0.5 - eps) * unitLength)
        {
            yaw = 2 * Atan2(qy, qw);
            pitch = PI;
            roll = 0;
        }
        else if (abcd < (-0.5 + eps) * unitLength)
        {
            yaw = -2 * Atan2(qy, qw);
            pitch = -PI;
            roll = 0;
        }
        else
        {
            double adbc = qw * qz - qx * qy;
            double acbd = qw * qy - qx * qz;
            yaw = Atan2(2 * adbc, 1 - 2 * (z2 + x2));
            pitch = Asin(2 * abcd / unitLength);
            roll = Atan2(2 * acbd, 1 - 2 * (y2 + x2));
        }

        ayaw = Angle.FromRadians(yaw).ToUnit(AngleUnit.Degree);
        apitch = Angle.FromRadians(pitch).ToUnit(AngleUnit.Degree);
        aroll = Angle.FromRadians(roll).ToUnit(AngleUnit.Degree);
    }

    public void PrintState()
    {
        Console.WriteLine($"Quaternion: ({x[0]}, {x[1]}, {x[2]}, {x[3]})");
        GetEulerAngles(x[0], x[1], x[2], x[3], out Angle heading, out Angle pitch, out Angle roll);
        Console.WriteLine($"Orientation: {heading}, {pitch}, {roll}");
        Console.WriteLine($"Position: ({x[4]}, {x[5]}, {x[6]}) meters");
        Console.WriteLine($"Velocity: ({x[7]}, {x[8]}, {x[9]}) meters/second");
    }

    public static void RunTest(string[] args)
    {
        var initialPosition = SphericalToCartesian(48.5, 9.2, EARTH_RADIUS + 400);
        EkfOrientationWithGnss ekf = new EkfOrientationWithGnss();
        // Set a non-zero initial position
        ekf.x[4] = initialPosition[0];
        ekf.x[5] = initialPosition[1];
        ekf.x[6] = initialPosition[2];

        // Simulating continuous IMU and GNSS data update
        while (!Console.KeyAvailable)
        {
            // Example input data (replace with actual sensor readings)
            double gx = 0.00, gy = 0.00, gz = 0.00; // gyroscope values
            double ax = 0.0, ay = 0.0, az = 1.0; // acceleration values
            double mx = 1.0, my = 0.0, mz = 0.0; // magnetometer values
            double px = initialPosition[0], py = initialPosition[1], pz = initialPosition[2]; // position in ECEF coordinates
            double vx = 0.0, vy = 0.0, vz = 0.0; // velocity in 3 dimensions

            ekf.UpdateIMUData(gx, gy, gz, ax, ay, az, mx, my, mz, 45, 5);
            ekf.UpdateGNSSData(px, py, pz, vx, vy, vz);
            ekf.PrintState();
            Thread.Sleep(50);
        };

        Console.ReadKey(true);
    }

    /// <summary>
    /// Converts position in spherical coordinates (lat/lon/altitude) to cartesian (XYZ) coordinates.
    /// </summary>
    /// <param name="latitude">Latitude (Angle)</param>
    /// <param name="longitude">Longitude (Angle)</param>
    /// <param name="radius">Radius (OBS: not altitude)</param>
    /// <returns>Coordinates converted to cartesian (XYZ)</returns>
    public static Vector<double> SphericalToCartesian(
        double latitude,
        double longitude,
        double radius)
    {
        double latRadians = latitude / 180 * PI;
        double lonRadians = longitude / 180 * PI;

        double radCosLat = radius * Math.Cos(latRadians);

        return Vector<double>.Build.DenseOfArray(new double[]
        {
            radCosLat * Math.Cos(lonRadians),
            radCosLat * Math.Sin(lonRadians),
            radius * Math.Sin(latRadians)
        });
    }

    /// <summary>
    /// Rotates the object to the world
    /// </summary>
    public static Matrix<double> RotateObjectToWorld(double lat, double lon, double alt, double heading, double pitch, double roll)
    {
        Matrix<double> rotationMatrix = Matrix4D.ObjectRotationHeadingPitchRoll(heading, pitch, roll);

        double r = EARTH_RADIUS + alt;
        rotationMatrix = Matrix4D.TranslationMatrix(0, 0, r) * rotationMatrix;

        // And rotate from there to the correct point on earth.
        rotationMatrix = Matrix4D.RotationMatrixY(Matrix4D.DegreesToRadians(90 - lat)) * rotationMatrix;
        rotationMatrix = Matrix4D.RotationMatrixZ(Matrix4D.DegreesToRadians(lon)) * rotationMatrix;

        rotationMatrix.Transpose(); // This matrix must be in row-vector order for directx

        return rotationMatrix;
    }
}
