// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Device;
using System.Device.I2c;
using System.Device.Model;
using System.IO;
using System.Net.Http.Headers;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Iot.Device.Magnetometer;
using UnitsNet;

namespace Iot.Device.Imu
{
    /// <summary>
    /// MPU9250 - gyroscope, accelerometer, temperature and magnetometer (thru an embedded AK8963).
    /// </summary>
    [Interface("MPU9250 - gyroscope, accelerometer, temperature and magnetometer (thru an embedded AK8963)")]
    public class Mpu9250 : Mpu6500
    {
        private Ak8963 _ak8963;
        private bool _shouldDispose;
        // Use for the first magnetometer read when switch to continuous 100 Hz
        private bool _firstContinuousRead = true;

        private Vector3 _accelerometerBias = new Vector3();
        private Vector3 _gyroscopeBias = new Vector3();

        #region Magnetometer

        /// <summary>
        /// Get the magnetometer bias
        /// </summary>
        /// <remarks>
        /// Vector axes are the following:
        ///    +Z   +Y
        ///  \  |  /
        ///   \ | /
        ///    \|/
        ///    /|\
        ///   / | \
        ///  /  |  \
        ///         +X
        /// </remarks>
        [Property]
        public Vector3 MagnometerBias => new Vector3(_ak8963.MagnetometerBias.Y, _ak8963.MagnetometerBias.X, -_ak8963.MagnetometerBias.Z);

        /// <summary>
        /// Calibrate the magnetometer. Make sure your sensor is as far as possible of magnet.
        /// Move your sensor in all direction to make sure it will get enough data in all points of space
        /// Calculate as well the magnetometer bias
        /// </summary>
        /// <param name="calibrationCounts">number of points to read during calibration, default is 1000</param>
        /// <returns>Returns the factory calibration data</returns>
        [Command]
        public Vector3 CalibrateMagnetometer(int calibrationCounts = 1000)
        {
            if (_wakeOnMotion)
            {
                return Vector3.Zero;
            }

            // Run the calibration
            var calib = _ak8963.CalibrateMagnetometer(calibrationCounts);
            // Invert X and Y, don't change Z, this is a multiplication factor only
            // it should stay positive
            return new Vector3(calib.Y, calib.X, calib.Z);
        }

        /// <summary>
        /// True if there is a data to read
        /// </summary>
        public bool HasDataToRead => !(_wakeOnMotion && _ak8963.HasDataToRead);

        /// <summary>
        /// Check if the magnetometer version is the correct one (0x48)
        /// </summary>
        /// <returns>Returns the Magnetometer version number</returns>
        /// <remarks>When the wake on motion is on, you can't read the magnetometer, so this function returns 0</remarks>
        [Property("MagnetometerVersion")]
        public byte GetMagnetometerVersion() => _wakeOnMotion ? (byte)0 : _ak8963.GetDeviceInfo();

        /// <summary>
        /// Read the magnetometer without bias correction and can wait for new data to be present
        /// </summary>
        /// <remarks>
        /// Vector axes are the following:
        ///    +Z   +Y
        ///  \  |  /
        ///   \ | /
        ///    \|/
        ///    /|\
        ///   / | \
        ///  /  |  \
        ///         +X
        /// </remarks>
        /// <param name="waitForData">true to wait for new data</param>
        /// <returns>The data from the magnetometer</returns>
        public Vector3 ReadMagnetometerWithoutCorrection(bool waitForData = true)
        {
            var readMag = _ak8963.ReadMagnetometerWithoutCorrection(waitForData, GetTimeout());
            _firstContinuousRead = false;
            return _wakeOnMotion ? Vector3.Zero : new Vector3(readMag.Y, readMag.X, -readMag.Z);
        }

        /// <summary>
        /// Read the magnetometer with bias correction and can wait for new data to be present
        /// </summary>
        /// <remarks>
        /// Vector axes are the following:
        ///    +Z   +Y
        ///  \  |  /
        ///   \ | /
        ///    \|/
        ///    /|\
        ///   / | \
        ///  /  |  \
        ///         +X
        /// </remarks>
        /// <param name="waitForData">true to wait for new data</param>
        /// <returns>The data from the magnetometer</returns>
        [Telemetry("MagneticInduction")]
        public Vector3 ReadMagnetometer(bool waitForData = true)
        {
            Vector3 magn = _ak8963.ReadMagnetometer(waitForData, GetTimeout());
            return new Vector3(magn.Y, magn.X, -magn.Z);
        }

        // TODO: find what is the value in the documentation, it should be pretty fast
        // But taking the same value as for the slowest one so th 8Hz one
        private TimeSpan GetTimeout() => _ak8963.MeasurementMode switch
        {
            // 8Hz measurement period plus 2 milliseconds
            MeasurementMode.SingleMeasurement or MeasurementMode.ExternalTriggedMeasurement or MeasurementMode.SelfTest or MeasurementMode.ContinuousMeasurement8Hz
                => TimeSpan.FromMilliseconds(127),
            // 100Hz measurement period plus 2 milliseconds
            // When switching to this mode, the first read can be longer than 10 ms. Tests shows up to 100 ms
            MeasurementMode.ContinuousMeasurement100Hz => _firstContinuousRead ? TimeSpan.FromMilliseconds(100) : TimeSpan.FromMilliseconds(12),
            _ => TimeSpan.Zero,
        };

        /// <summary>
        /// Select the magnetometer measurement mode
        /// </summary>
        public MeasurementMode MagnetometerMeasurementMode
        {
            get => _ak8963.MeasurementMode;
            set
            {
                _ak8963.MeasurementMode = value;
                if (value == MeasurementMode.ContinuousMeasurement100Hz)
                {
                    _firstContinuousRead = true;
                }
            }
        }

        /// <summary>
        /// Select the magnetometer output bit rate
        /// </summary>
        [Property]
        public OutputBitMode MagnetometerOutputBitMode
        {
            get => _ak8963.OutputBitMode;
            set => _ak8963.OutputBitMode = value;
        }

        /// <summary>
        /// Get the magnetometer hardware adjustment bias
        /// </summary>
        [Property]
        public Vector3 MagnetometerAdjustment => _ak8963.MagnetometerAdjustment;

        #endregion

        /// <summary>
        /// Initialize the MPU9250
        /// </summary>
        /// <param name="i2cDevice">The I2C device</param>
        /// <param name="shouldDispose">Will automatically dispose the I2C device if true</param>
        /// <param name="i2CDeviceAk8963">An I2C Device for the AK8963 when exposed and not behind the MPU9250</param>
        public Mpu9250(I2cDevice i2cDevice, bool shouldDispose = true, I2cDevice? i2CDeviceAk8963 = null)
            : base(i2cDevice, true)
        {
            Reset();
            PowerOn();
            if (!CheckVersion())
            {
                throw new IOException($"This device does not contain the correct signature 0x71 for a MPU9250");
            }

            GyroscopeBandwidth = GyroscopeBandwidth.Bandwidth0250Hz;
            GyroscopeRange = GyroscopeRange.Range0250Dps;
            AccelerometerBandwidth = AccelerometerBandwidth.Bandwidth1130Hz;
            AccelerometerRange = AccelerometerRange.Range02G;
            // Setup I2C for the AK8963
            WriteRegister(Register.USER_CTRL, (byte)UserControls.I2C_MST_EN);
            // Speed of 400 kHz
            WriteRegister(Register.I2C_MST_CTRL, (byte)I2cBusFrequency.Frequency400kHz);
            _shouldDispose = shouldDispose;
            // There are 2 options to setup the Ak8963. Either the I2C address is exposed, either not.
            // Trying both and pick one of them
            if (i2CDeviceAk8963 == null)
            {
                try
                {
                    _ak8963 = new Ak8963(i2cDevice, new Ak8963Attached(), false);
                }
                catch (IOException ex)
                {
                    throw new IOException($"Please try to create an I2cDevice for the AK8963, it may be exposed", ex);
                }
            }
            else
            {
                _ak8963 = new Ak8963(i2CDeviceAk8963);
            }

            if (!_ak8963.IsVersionCorrect())
            {
                // Try to reset the device first
                _ak8963.Reset();
                // Wait a bit
                if (!_ak8963.IsVersionCorrect())
                {
                    // Try to reset the I2C Bus
                    WriteRegister(Register.USER_CTRL, (byte)UserControls.I2C_MST_RST);
                    Thread.Sleep(100);
                    // Resetup again
                    WriteRegister(Register.USER_CTRL, (byte)UserControls.I2C_MST_EN);
                    WriteRegister(Register.I2C_MST_CTRL, (byte)I2cBusFrequency.Frequency400kHz);
                    // Poorly documented time to wait after the I2C bus reset
                    // Found out that waiting a little bit is needed. Exact time may be lower
                    Thread.Sleep(100);
                    // Try one more time
                    if (!_ak8963.IsVersionCorrect())
                    {
                        throw new IOException($"This device does not contain the correct signature 0x48 for a AK8963 embedded into the MPU9250");
                    }
                }
            }

            _ak8963.MeasurementMode = MeasurementMode.SingleMeasurement;
        }

        /// <summary>
        /// Accelerometer bias data
        /// </summary>
        [Property]
        public Vector3 AccelerometerBias => _accelerometerBias;

        /// <summary>
        /// Gyroscope bias data
        /// </summary>
        [Property]
        public Vector3 GyroscopeBias => _gyroscopeBias;

        /// <summary>
        /// Return true if the version of MPU9250 is the correct one
        /// </summary>
        /// <returns>True if success</returns>
        internal new bool CheckVersion()
        {
            // Check if the version is thee correct one
            return ReadByte(Register.WHO_AM_I) == 0x71;
        }

        /// <summary>
        /// Setup the Wake On Motion. This mode generate a rising signal on pin INT
        /// You can catch it with a normal GPIO and place an interruption on it if supported
        /// Reading the sensor won't give any value until it wakes up periodically
        /// Only Accelerator data is available in this mode
        /// </summary>
        /// <param name="accelerometerThreshold">Threshold of magnetometer x/y/z axes. LSB = 4mg. Range is 0mg to 1020mg</param>
        /// <param name="acceleratorLowPower">Frequency used to measure data for the low power consumption mode</param>
        public new void SetWakeOnMotion(uint accelerometerThreshold, AccelerometerLowPowerFrequency acceleratorLowPower)
        {
            // We can't use the magnetometer, only Accelerometer will be measured
            _ak8963.MeasurementMode = MeasurementMode.PowerDown;
            base.SetWakeOnMotion(accelerometerThreshold, acceleratorLowPower);
        }

        #region Calibration and tests

        /// <summary>
        /// Perform full calibration the gyroscope and the accelerometer
        /// It will automatically adjust as well the offset stored in the device
        /// The result bias will be stored in the AcceloremeterBias and GyroscopeBias
        /// </summary>
        /// <returns>Gyroscope and accelerometer bias</returns>
        [Command]
        public (Vector3 GyroscopeBias, Vector3 AccelerometerBias) CalibrateGyroscopeAccelerometer()
        {
            // = 131 LSB/degrees/sec
            const int GyroSensitivity = 131;
            // = 16384 LSB/g
            const int AccSensitivity = 16384;
            byte i2cMaster;
            byte userControls;

            Span<byte> rawData = stackalloc byte[12];

            Vector3 gyroBias = new Vector3();
            Vector3 acceBias = new Vector3();

            Reset();
            // Enable accelerator and gyroscope
            DisableModes = DisableModes.DisableNone;
            Thread.Sleep(200);
            // Disable all interrupts
            WriteRegister(Register.INT_ENABLE, 0x00);
            // Disable FIFO
            FifoModes = FifoModes.None;
            // Disable I2C master
            i2cMaster = ReadByte(Register.I2C_MST_CTRL);
            WriteRegister(Register.I2C_MST_CTRL, 0x00);
            // Disable FIFO and I2C master modes
            userControls = ReadByte(Register.USER_CTRL);
            WriteRegister(Register.USER_CTRL, (byte)UserControls.None);
            // Reset FIFO and DMP
            WriteRegister(Register.USER_CTRL, (byte)UserControls.FIFO_RST);
            DelayHelper.DelayMilliseconds(15, false);

            // Configure MPU6050 gyro and accelerometer for bias calculation
            // Set low-pass filter to 184 Hz
            GyroscopeBandwidth = GyroscopeBandwidth.Bandwidth0184Hz;
            AccelerometerBandwidth = AccelerometerBandwidth.Bandwidth0184Hz;
            // Set sample rate to 1 kHz
            SampleRateDivider = 0;
            // Set gyro to maximum sensitivity
            GyroscopeRange = GyroscopeRange.Range0250Dps;
            AccelerometerRange = AccelerometerRange.Range02G;

            // Configure FIFO will be needed for bias calculation
            FifoModes = FifoModes.GyroscopeX | FifoModes.GyroscopeY | FifoModes.GyroscopeZ | FifoModes.Accelerometer;
            // accumulate 40 samples in 40 milliseconds = 480 bytes
            // Do not exceed 512 bytes max buffer
            DelayHelper.DelayMilliseconds(40, false);
            // We have our data, deactivate FIFO
            FifoModes = FifoModes.None;

            // How many sets of full gyro and accelerometer data for averaging
            var packetCount = FifoCount / 12;

            for (uint reading = 0; reading < packetCount; reading++)
            {
                Vector3 accel_temp = new Vector3();
                Vector3 gyro_temp = new Vector3();

                // Read data
                ReadBytes(Register.FIFO_R_W, rawData);

                // Form signed 16-bit integer for each sample in FIFO
                accel_temp.X = BinaryPrimitives.ReadInt16BigEndian(rawData);
                accel_temp.Y = BinaryPrimitives.ReadInt16BigEndian(rawData.Slice(2));
                accel_temp.Z = BinaryPrimitives.ReadInt16BigEndian(rawData.Slice(4));
                gyro_temp.X = BinaryPrimitives.ReadInt16BigEndian(rawData.Slice(6));
                gyro_temp.Y = BinaryPrimitives.ReadInt16BigEndian(rawData.Slice(8));
                gyro_temp.Z = BinaryPrimitives.ReadInt16BigEndian(rawData.Slice(10));

                acceBias += accel_temp;
                gyroBias += gyro_temp;
            }

            // Make the average
            acceBias /= packetCount;
            gyroBias /= packetCount;

            // bias on Z is cumulative
            acceBias.Z += acceBias.Z > 0 ? -AccSensitivity : AccSensitivity;

            // Divide by 4 to get 32.9 LSB per deg/s
            // Biases are additive, so change sign on calculated average gyro biases
            rawData[0] = (byte)(((int)(-gyroBias.X / 4) >> 8) & 0xFF);
            rawData[1] = (byte)((int)(-gyroBias.X / 4) & 0xFF);
            rawData[2] = (byte)(((int)(-gyroBias.Y / 4) >> 8) & 0xFF);
            rawData[3] = (byte)((int)(-gyroBias.Y / 4) & 0xFF);
            rawData[4] = (byte)(((int)(-gyroBias.Z / 4) >> 8) & 0xFF);
            rawData[5] = (byte)((int)(-gyroBias.Z / 4) & 0xFF);

            // Changes all Gyroscope offsets
            WriteRegister(Register.XG_OFFSET_H, rawData[0]);
            WriteRegister(Register.XG_OFFSET_L, rawData[1]);
            WriteRegister(Register.YG_OFFSET_H, rawData[2]);
            WriteRegister(Register.YG_OFFSET_L, rawData[3]);
            WriteRegister(Register.ZG_OFFSET_H, rawData[4]);
            WriteRegister(Register.ZG_OFFSET_L, rawData[5]);

            // Output scaled gyro biases for display in the main program
            _gyroscopeBias = gyroBias / GyroSensitivity;

            // Construct the accelerometer biases for push to the hardware accelerometer
            // bias registers. These registers contain factory trim values which must be
            // added to the calculated accelerometer biases; on boot up these registers
            // will hold non-zero values. In addition, bit 0 of the lower byte must be
            // preserved since it is used for temperature compensation calculations.
            // Accelerometer bias registers expect bias input as 2048 LSB per g, so that
            // the accelerometer biases calculated above must be divided by 8.

            // A place to hold the factory accelerometer trim biases
            Vector3 accel_bias_reg = new Vector3();
            Span<byte> accData = stackalloc byte[2];
            // Read factory accelerometer trim values
            ReadBytes(Register.XA_OFFSET_H, accData);
            accel_bias_reg.X = BinaryPrimitives.ReadInt16BigEndian(accData);
            ReadBytes(Register.YA_OFFSET_H, accData);
            accel_bias_reg.Y = BinaryPrimitives.ReadInt16BigEndian(accData);
            ReadBytes(Register.ZA_OFFSET_H, accData);
            accel_bias_reg.Z = BinaryPrimitives.ReadInt16BigEndian(accData);

            // Define mask for temperature compensation bit 0 of lower byte of
            // accelerometer bias registers
            uint mask = 0x01;
            // Define array to hold mask bit for each accelerometer bias axis
            Span<byte> mask_bit = stackalloc byte[3];

            // If temperature compensation bit is set, record that fact in mask_bit
            mask_bit[0] = (((uint)accel_bias_reg.X & mask) == mask) ? (byte)0x01 : (byte)0x00;
            mask_bit[1] = (((uint)accel_bias_reg.Y & mask) == mask) ? (byte)0x01 : (byte)0x00;
            mask_bit[2] = (((uint)accel_bias_reg.Z & mask) == mask) ? (byte)0x01 : (byte)0x00;

            // Construct total accelerometer bias, including calculated average
            // accelerometer bias from above
            // Subtract calculated averaged accelerometer bias scaled to 2048 LSB/g
            // (16 g full scale) and keep the mask
            accel_bias_reg -= acceBias / 8;
            // Add the "reserved" mask as it was
            rawData[0] = (byte)(((int)accel_bias_reg.X >> 8) & 0xFF);
            rawData[1] = (byte)(((int)accel_bias_reg.X & 0xFF) | mask_bit[0]);
            rawData[2] = (byte)(((int)accel_bias_reg.Y >> 8) & 0xFF);
            rawData[3] = (byte)(((int)accel_bias_reg.Y & 0xFF) | mask_bit[1]);
            rawData[4] = (byte)(((int)accel_bias_reg.Z >> 8) & 0xFF);
            rawData[5] = (byte)(((int)accel_bias_reg.Z & 0xFF) | mask_bit[2]);
            // Push accelerometer biases to hardware registers
            WriteRegister(Register.XA_OFFSET_H, rawData[0]);
            WriteRegister(Register.XA_OFFSET_L, rawData[1]);
            WriteRegister(Register.YA_OFFSET_H, rawData[2]);
            WriteRegister(Register.YA_OFFSET_L, rawData[3]);
            WriteRegister(Register.ZA_OFFSET_H, rawData[4]);
            WriteRegister(Register.ZA_OFFSET_L, rawData[5]);

            // Restore the previous modes
            WriteRegister(Register.USER_CTRL, (byte)(userControls | (byte)UserControls.I2C_MST_EN));
            i2cMaster = (byte)(i2cMaster & (~(byte)(I2cBusFrequency.Frequency348kHz) | (byte)I2cBusFrequency.Frequency400kHz));
            WriteRegister(Register.I2C_MST_CTRL, i2cMaster);
            DelayHelper.DelayMilliseconds(10, false);

            // Finally store the acceleration bias
            _accelerometerBias = acceBias / AccSensitivity;

            return (_gyroscopeBias, _accelerometerBias);
        }

        #endregion

        /// <summary>
        /// Cleanup everything
        /// </summary>
        public new void Dispose()
        {
            if (_shouldDispose)
            {
                _i2cDevice?.Dispose();
                _i2cDevice = null!;
            }
        }

    }
}
