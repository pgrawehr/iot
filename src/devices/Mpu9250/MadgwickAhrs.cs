using System;
using System.Collections.Generic;
using System.Text;

#pragma warning disable SA1312
namespace Iot.Device.Imu
{
    /// <summary>
    /// MadgwickAHRS class. Implementation of Madgwick's IMU and AHRS algorithms.
    /// </summary>
    /// <remarks>
    /// See: http://www.x-io.co.uk/node/8#openpsourcepahrspandpimupalgorithms
    /// </remarks>
    public class MadgwickAhrs
    {
        private float _zeta;
        private float _beta;

        /// <summary>
        /// Gets or sets the sample period.
        /// </summary>
        public float SamplePeriod { get; set; }

        /// <summary>
        /// Gets or sets the algorithm gain beta.
        /// </summary>
        public float Beta { get; set; }

        /// <summary>
        /// Gets or sets the Quaternion output.
        /// </summary>
        public float[] Quaternion { get; set; }

        private float GyroMeasDrift
        {
            get;
        }

        private float GyroMeasError
        {
            get;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MadgwickAhrs"/> class.
        /// </summary>
        /// <param name="samplePeriod">
        /// Sample period.
        /// </param>
        public MadgwickAhrs(float samplePeriod)
            : this(samplePeriod, 1f)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MadgwickAhrs"/> class.
        /// </summary>
        /// <param name="samplePeriod">
        /// Sample period.
        /// </param>
        /// <param name="beta">
        /// Algorithm gain beta.
        /// </param>
        public MadgwickAhrs(float samplePeriod, float beta)
        {
            SamplePeriod = samplePeriod;
            Beta = beta;
            Quaternion = new float[] { 1f, 0f, 0f, 0f };
            GyroMeasDrift = (float)Math.PI * (2.0f / 180.0f);      // gyroscope measurement drift in rad/s/s (start at 0.0 deg/s/s)
            _zeta = (float)Math.Sqrt(3.0f / 4.0f) * GyroMeasDrift;

            GyroMeasError = (float)Math.PI * (40.0f / 180.0f);     // gyroscope measurement error in rads/s (start at 60 deg/s), then reduce after ~10 s to 3
            _beta = (float)Math.Sqrt(3.0f / 4.0f) * GyroMeasError;  // compute beta
        }

        /// <summary>
        /// Algorithm AHRS update method. Requires only gyroscope and accelerometer data.
        /// </summary>
        /// <param name="gx">
        /// Gyroscope x axis measurement in radians/s.
        /// </param>
        /// <param name="gy">
        /// Gyroscope y axis measurement in radians/s.
        /// </param>
        /// <param name="gz">
        /// Gyroscope z axis measurement in radians/s.
        /// </param>
        /// <param name="ax">
        /// Accelerometer x axis measurement in any calibrated units.
        /// </param>
        /// <param name="ay">
        /// Accelerometer y axis measurement in any calibrated units.
        /// </param>
        /// <param name="az">
        /// Accelerometer z axis measurement in any calibrated units.
        /// </param>
        /// <param name="mx">
        /// Magnetometer x axis measurement in any calibrated units.
        /// </param>
        /// <param name="my">
        /// Magnetometer y axis measurement in any calibrated units.
        /// </param>
        /// <param name="mz">
        /// Magnetometer z axis measurement in any calibrated units.
        /// </param>
        /// <remarks>
        /// Optimised for minimal arithmetic.
        /// Total ±: 160
        /// Total *: 172
        /// Total /: 5
        /// Total sqrt: 5
        /// </remarks>
        public void Update(float gx, float gy, float gz, float ax, float ay, float az, float mx, float my, float mz)
        {
            float q1 = Quaternion[0], q2 = Quaternion[1], q3 = Quaternion[2], q4 = Quaternion[3];   // short name local variable for readability
            float norm;
            float hx, hy, p2bx, p2bz;
            float s1, s2, s3, s4;
            float qDot1, qDot2, qDot3, qDot4;

            // Auxiliary variables to avoid repeated arithmetic
            float p2q1mx;
            float p2q1my;
            float p2q1mz;
            float p2q2mx;
            float p4bx;
            float p4bz;
            float p2q1 = 2f * q1;
            float p2q2 = 2f * q2;
            float p2q3 = 2f * q3;
            float p2q4 = 2f * q4;
            float p2q1q3 = 2f * q1 * q3;
            float p2q3q4 = 2f * q3 * q4;
            float q1q1 = q1 * q1;
            float q1q2 = q1 * q2;
            float q1q3 = q1 * q3;
            float q1q4 = q1 * q4;
            float q2q2 = q2 * q2;
            float q2q3 = q2 * q3;
            float q2q4 = q2 * q4;
            float q3q3 = q3 * q3;
            float q3q4 = q3 * q4;
            float q4q4 = q4 * q4;

            // Normalise accelerometer measurement
            norm = (float)Math.Sqrt(ax * ax + ay * ay + az * az);
            if (norm == 0f)
            {
                return; // handle NaN
            }

            norm = 1 / norm;        // use reciprocal for division
            ax *= norm;
            ay *= norm;
            az *= norm;

            // Normalise magnetometer measurement
            norm = (float)Math.Sqrt(mx * mx + my * my + mz * mz);
            if (norm == 0f)
            {
                return; // handle NaN
            }

            norm = 1 / norm;        // use reciprocal for division
            mx *= norm;
            my *= norm;
            mz *= norm;

            // Reference direction of Earth's magnetic field
            p2q1mx = 2f * q1 * mx;
            p2q1my = 2f * q1 * my;
            p2q1mz = 2f * q1 * mz;
            p2q2mx = 2f * q2 * mx;
            hx = mx * q1q1 - p2q1my * q4 + p2q1mz * q3 + mx * q2q2 + p2q2 * my * q3 + p2q2 * mz * q4 - mx * q3q3 - mx * q4q4;
            hy = p2q1mx * q4 + my * q1q1 - p2q1mz * q2 + p2q2mx * q3 - my * q2q2 + my * q3q3 + p2q3 * mz * q4 - my * q4q4;
            p2bx = (float)Math.Sqrt(hx * hx + hy * hy);
            p2bz = -p2q1mx * q3 + p2q1my * q2 + mz * q1q1 + p2q2mx * q4 - mz * q2q2 + p2q3 * my * q4 - mz * q3q3 + mz * q4q4;
            p4bx = 2f * p2bx;
            p4bz = 2f * p2bz;

            // Gradient decent algorithm corrective step
            s1 = -p2q3 * (2f * q2q4 - p2q1q3 - ax) + p2q2 * (2f * q1q2 + p2q3q4 - ay) - p2bz * q3 * (p2bx * (0.5f - q3q3 - q4q4) + p2bz * (q2q4 - q1q3) - mx) + (-p2bx * q4 + p2bz * q2) * (p2bx * (q2q3 - q1q4) + p2bz * (q1q2 + q3q4) - my) + p2bx * q3 * (p2bx * (q1q3 + q2q4) + p2bz * (0.5f - q2q2 - q3q3) - mz);
            s2 = p2q4 * (2f * q2q4 - p2q1q3 - ax) + p2q1 * (2f * q1q2 + p2q3q4 - ay) - 4f * q2 * (1 - 2f * q2q2 - 2f * q3q3 - az) + p2bz * q4 * (p2bx * (0.5f - q3q3 - q4q4) + p2bz * (q2q4 - q1q3) - mx) + (p2bx * q3 + p2bz * q1) * (p2bx * (q2q3 - q1q4) + p2bz * (q1q2 + q3q4) - my) + (p2bx * q4 - p4bz * q2) * (p2bx * (q1q3 + q2q4) + p2bz * (0.5f - q2q2 - q3q3) - mz);
            s3 = -p2q1 * (2f * q2q4 - p2q1q3 - ax) + p2q4 * (2f * q1q2 + p2q3q4 - ay) - 4f * q3 * (1 - 2f * q2q2 - 2f * q3q3 - az) + (-p4bx * q3 - p2bz * q1) * (p2bx * (0.5f - q3q3 - q4q4) + p2bz * (q2q4 - q1q3) - mx) + (p2bx * q2 + p2bz * q4) * (p2bx * (q2q3 - q1q4) + p2bz * (q1q2 + q3q4) - my) + (p2bx * q1 - p4bz * q3) * (p2bx * (q1q3 + q2q4) + p2bz * (0.5f - q2q2 - q3q3) - mz);
            s4 = p2q2 * (2f * q2q4 - p2q1q3 - ax) + p2q3 * (2f * q1q2 + p2q3q4 - ay) + (-p4bx * q4 + p2bz * q2) * (p2bx * (0.5f - q3q3 - q4q4) + p2bz * (q2q4 - q1q3) - mx) + (-p2bx * q1 + p2bz * q3) * (p2bx * (q2q3 - q1q4) + p2bz * (q1q2 + q3q4) - my) + p2bx * q2 * (p2bx * (q1q3 + q2q4) + p2bz * (0.5f - q2q2 - q3q3) - mz);
            norm = 1f / (float)Math.Sqrt(s1 * s1 + s2 * s2 + s3 * s3 + s4 * s4);    // normalise step magnitude
            s1 *= norm;
            s2 *= norm;
            s3 *= norm;
            s4 *= norm;

            // Compute rate of change of quaternion
            qDot1 = 0.5f * (-q2 * gx - q3 * gy - q4 * gz) - Beta * s1;
            qDot2 = 0.5f * (q1 * gx + q3 * gz - q4 * gy) - Beta * s2;
            qDot3 = 0.5f * (q1 * gy - q2 * gz + q4 * gx) - Beta * s3;
            qDot4 = 0.5f * (q1 * gz + q2 * gy - q3 * gx) - Beta * s4;

            // Integrate to yield quaternion
            q1 += qDot1 * SamplePeriod;
            q2 += qDot2 * SamplePeriod;
            q3 += qDot3 * SamplePeriod;
            q4 += qDot4 * SamplePeriod;
            norm = 1f / (float)Math.Sqrt(q1 * q1 + q2 * q2 + q3 * q3 + q4 * q4);    // normalise quaternion
            Quaternion[0] = q1 * norm;
            Quaternion[1] = q2 * norm;
            Quaternion[2] = q3 * norm;
            Quaternion[3] = q4 * norm;
        }

        /// <summary>
        /// Algorithm IMU update method. Requires only gyroscope and accelerometer data.
        /// </summary>
        /// <param name="gx">
        /// Gyroscope x axis measurement in radians/s.
        /// </param>
        /// <param name="gy">
        /// Gyroscope y axis measurement in radians/s.
        /// </param>
        /// <param name="gz">
        /// Gyroscope z axis measurement in radians/s.
        /// </param>
        /// <param name="ax">
        /// Accelerometer x axis measurement in any calibrated units.
        /// </param>
        /// <param name="ay">
        /// Accelerometer y axis measurement in any calibrated units.
        /// </param>
        /// <param name="az">
        /// Accelerometer z axis measurement in any calibrated units.
        /// </param>
        /// <remarks>
        /// Optimised for minimal arithmetic.
        /// Total ±: 45
        /// Total *: 85
        /// Total /: 3
        /// Total sqrt: 3
        /// </remarks>
        public void Update(float gx, float gy, float gz, float ax, float ay, float az)
        {
            float q1 = Quaternion[0], q2 = Quaternion[1], q3 = Quaternion[2], q4 = Quaternion[3];   // short name local variable for readability
            float norm;
            float s1, s2, s3, s4;
            float qDot1, qDot2, qDot3, qDot4;

            // Auxiliary variables to avoid repeated arithmetic
            float p2q1 = 2f * q1;
            float p2q2 = 2f * q2;
            float p2q3 = 2f * q3;
            float p2q4 = 2f * q4;
            float p4q1 = 4f * q1;
            float p4q2 = 4f * q2;
            float p4q3 = 4f * q3;
            float p8q2 = 8f * q2;
            float p8q3 = 8f * q3;
            float q1q1 = q1 * q1;
            float q2q2 = q2 * q2;
            float q3q3 = q3 * q3;
            float q4q4 = q4 * q4;

            // Normalise accelerometer measurement
            norm = (float)Math.Sqrt(ax * ax + ay * ay + az * az);
            if (norm == 0f)
            {
                return; // handle NaN
            }

            norm = 1 / norm;        // use reciprocal for division
            ax *= norm;
            ay *= norm;
            az *= norm;

            // Gradient decent algorithm corrective step
            s1 = p4q1 * q3q3 + p2q3 * ax + p4q1 * q2q2 - p2q2 * ay;
            s2 = p4q2 * q4q4 - p2q4 * ax + 4f * q1q1 * q2 - p2q1 * ay - p4q2 + p8q2 * q2q2 + p8q2 * q3q3 + p4q2 * az;
            s3 = 4f * q1q1 * q3 + p2q1 * ax + p4q3 * q4q4 - p2q4 * ay - p4q3 + p8q3 * q2q2 + p8q3 * q3q3 + p4q3 * az;
            s4 = 4f * q2q2 * q4 - p2q2 * ax + 4f * q3q3 * q4 - p2q3 * ay;
            norm = 1f / (float)Math.Sqrt(s1 * s1 + s2 * s2 + s3 * s3 + s4 * s4);    // normalise step magnitude
            s1 *= norm;
            s2 *= norm;
            s3 *= norm;
            s4 *= norm;

            // Compute rate of change of quaternion
            qDot1 = 0.5f * (-q2 * gx - q3 * gy - q4 * gz) - Beta * s1;
            qDot2 = 0.5f * (q1 * gx + q3 * gz - q4 * gy) - Beta * s2;
            qDot3 = 0.5f * (q1 * gy - q2 * gz + q4 * gx) - Beta * s3;
            qDot4 = 0.5f * (q1 * gz + q2 * gy - q3 * gx) - Beta * s4;

            // Integrate to yield quaternion
            q1 += qDot1 * SamplePeriod;
            q2 += qDot2 * SamplePeriod;
            q3 += qDot3 * SamplePeriod;
            q4 += qDot4 * SamplePeriod;
            norm = 1f / (float)Math.Sqrt(q1 * q1 + q2 * q2 + q3 * q3 + q4 * q4);    // normalise quaternion
            Quaternion[0] = q1 * norm;
            Quaternion[1] = q2 * norm;
            Quaternion[2] = q3 * norm;
            Quaternion[3] = q4 * norm;
        }

        /// <summary>
        /// From a different source
        /// </summary>
        public void MadgwickQuaternionUpdateWithBias(float ax, float ay, float az, float gx, float gy, float gz)
        {
            float q1 = Quaternion[0], q2 = Quaternion[1], q3 = Quaternion[2], q4 = Quaternion[3];         // short name local variable for readability
            float norm;                                               // vector norm
            float f1, f2, f3;                                         // objetive funcyion elements
            float J_11or24, J_12or23, J_13or22, J_14or21, J_32, J_33; // objective function Jacobian elements
            float qDot1, qDot2, qDot3, qDot4;
            float hatDot1, hatDot2, hatDot3, hatDot4;
            float gerrx, gerry, gerrz, gbiasx = 0, gbiasy = 0, gbiasz = 0;        // gyro bias error

            // Auxiliary variables to avoid repeated arithmetic
            float _halfq1 = 0.5f * q1;
            float _halfq2 = 0.5f * q2;
            float _halfq3 = 0.5f * q3;
            float _halfq4 = 0.5f * q4;
            float _2q1 = 2.0f * q1;
            float _2q2 = 2.0f * q2;
            float _2q3 = 2.0f * q3;
            float _2q4 = 2.0f * q4;
            float _2q1q3 = 2.0f * q1 * q3;
            float _2q3q4 = 2.0f * q3 * q4;

            // Normalise accelerometer measurement
            norm = (float)Math.Sqrt(ax * ax + ay * ay + az * az);
            if (norm == 0.0f)
            {
                return; // handle NaN
            }

            norm = 1.0f / norm;
            ax *= norm;
            ay *= norm;
            az *= norm;

            // Compute the objective function and Jacobian
            f1 = _2q2 * q4 - _2q1 * q3 - ax;
            f2 = _2q1 * q2 + _2q3 * q4 - ay;
            f3 = 1.0f - _2q2 * q2 - _2q3 * q3 - az;
            J_11or24 = _2q3;
            J_12or23 = _2q4;
            J_13or22 = _2q1;
            J_14or21 = _2q2;
            J_32 = 2.0f * J_14or21;
            J_33 = 2.0f * J_11or24;

            // Compute the gradient (matrix multiplication)
            hatDot1 = J_14or21 * f2 - J_11or24 * f1;
            hatDot2 = J_12or23 * f1 + J_13or22 * f2 - J_32 * f3;
            hatDot3 = J_12or23 * f2 - J_33 * f3 - J_13or22 * f1;
            hatDot4 = J_14or21 * f1 + J_11or24 * f2;

            // Normalize the gradient
            norm = (float)Math.Sqrt(hatDot1 * hatDot1 + hatDot2 * hatDot2 + hatDot3 * hatDot3 + hatDot4 * hatDot4);
            hatDot1 /= norm;
            hatDot2 /= norm;
            hatDot3 /= norm;
            hatDot4 /= norm;

            // Compute estimated gyroscope biases
            gerrx = _2q1 * hatDot2 - _2q2 * hatDot1 - _2q3 * hatDot4 + _2q4 * hatDot3;
            gerry = _2q1 * hatDot3 + _2q2 * hatDot4 - _2q3 * hatDot1 - _2q4 * hatDot2;
            gerrz = _2q1 * hatDot4 - _2q2 * hatDot3 + _2q3 * hatDot2 - _2q4 * hatDot1;

            // Compute and remove gyroscope biases
            gbiasx += gerrx * SamplePeriod * _zeta;
            gbiasy += gerry * SamplePeriod * _zeta;
            gbiasz += gerrz * SamplePeriod * _zeta;
            gx -= gbiasx;
            gy -= gbiasy;
            gz -= gbiasz;

            // Compute the quaternion derivative
            qDot1 = -_halfq2 * gx - _halfq3 * gy - _halfq4 * gz;
            qDot2 = _halfq1 * gx + _halfq3 * gz - _halfq4 * gy;
            qDot3 = _halfq1 * gy - _halfq2 * gz + _halfq4 * gx;
            qDot4 = _halfq1 * gz + _halfq2 * gy - _halfq3 * gx;

            // Compute then integrate estimated quaternion derivative
            q1 += (qDot1 - (_beta * hatDot1)) * SamplePeriod;
            q2 += (qDot2 - (_beta * hatDot2)) * SamplePeriod;
            q3 += (qDot3 - (_beta * hatDot3)) * SamplePeriod;
            q4 += (qDot4 - (_beta * hatDot4)) * SamplePeriod;

            // Normalize the quaternion
            norm = (float)Math.Sqrt(q1 * q1 + q2 * q2 + q3 * q3 + q4 * q4);    // normalise quaternion
            norm = 1.0f / norm;
            Quaternion[0] = q1 * norm;
            Quaternion[1] = q2 * norm;
            Quaternion[2] = q3 * norm;
            Quaternion[3] = q4 * norm;
        }
    }
}
