// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Device;
using System.Device.I2c;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;
using Iot.Device.Magnetometer;
using Iot.Units;

namespace Iot.Device.Imu
{
    /// <summary>
    ///  MPU6050 has an embedded gyroscope, accelerometer and temperature, but by default not a magnetometer (although one can optionally be attached)
    /// </summary>
    public class Mpu6050 : IDisposable
    {
        /// <summary>
        /// Default address for MPU6050
        /// </summary>
        public const byte DefaultI2cAddress = 0x68;

        /// <summary>
        /// Second address for MPU6050
        /// </summary>
        public const byte SecondI2cAddress = 0x69;

        private const float Adc = 0x8000;
        private const float Gravity = 9.807f;
        internal I2cDevice _i2cDevice;
        private Vector3 _accelerometerBias = new Vector3();
        private Vector3 _gyroscopeBias = new Vector3();
        private AccelerometerRange _accelerometerRange;
        private GyroscopeRange _gyroscopeRange;
        private Mpu6050GyroBandwidth _gyroscopeBandwidth;
        internal bool _wakeOnMotion;

        /// <summary>
        /// Initialize the MPU6050
        /// </summary>
        /// <param name="i2cDevice">The I2C device. The default address is 0x68.</param>
        public Mpu6050(I2cDevice i2cDevice)
        {
            _i2cDevice = i2cDevice;
            Reset();
            if (!CheckVersion())
            {
                throw new IOException($"This device does not contain the correct signature 0x68 for a MPU6050");
            }
        }

        #region Accelerometer

        /// <summary>
        /// Accelerometer bias data
        /// </summary>
        public Vector3 AccelerometerBias => _accelerometerBias;

        /// <summary>
        /// Get or set the accelerometer range
        /// </summary>
        public AccelerometerRange AccelerometerRange
        {
            get => _accelerometerRange;

            set
            {
                WriteRegister(Register.ACCEL_CONFIG, (byte)((byte)value << 3));
                // We will cache the range to avoid i2c access every time this data is requested
                // This allow as well to make sure the stored data is the same as the one read
                _accelerometerRange = (AccelerometerRange)((ReadByte(Register.ACCEL_CONFIG) >> 3) & 0x03);
                if (_accelerometerRange != value)
                {
                    throw new IOException($"Can set {nameof(AccelerometerRange)}, desired value {value}, stored value {_accelerometerRange}");
                }
            }
        }

        /// <summary>
        /// Get the real accelerometer range. This allows to calculate the real acceleration
        /// </summary>
        public float AccelerationScale
        {
            get
            {
                float val = 0;
                switch (AccelerometerRange)
                {
                    case AccelerometerRange.Range02G:
                        val = 2.0f;
                        break;
                    case AccelerometerRange.Range04G:
                        val = 4.0f;
                        break;
                    case AccelerometerRange.Range08G:
                        val = 8.0f;
                        break;
                    case AccelerometerRange.Range16G:
                        val = 16.0f;
                        break;
                    default:
                        break;
                }

                return val / 32768;
            }
        }

        /// <summary>
        /// Get the accelerometer in G
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
        public Vector3 GetAccelerometer() => GetRawAccelerometer() * AccelerationScale;

        private Vector3 GetRawAccelerometer()
        {
            Span<byte> rawData = stackalloc byte[6]
            {
                0, 0, 0, 0, 0, 0
            };
            Vector3 ace = new Vector3();
            ReadBytes(Register.ACCEL_XOUT_H, rawData);
            ace.X = BinaryPrimitives.ReadInt16BigEndian(rawData);
            ace.Y = BinaryPrimitives.ReadInt16BigEndian(rawData.Slice(2));
            ace.Z = BinaryPrimitives.ReadInt16BigEndian(rawData.Slice(4));
            return ace;
        }

        /// <summary>
        /// Set or get the accelerometer low power mode
        /// </summary>
        public AccelerometerLowPowerFrequency AccelerometerLowPowerFrequency
        {
            get { return (AccelerometerLowPowerFrequency)ReadByte(Register.LP_ACCEL_ODR); }
            set { WriteRegister(Register.LP_ACCEL_ODR, (byte)value); }
        }

        #endregion

        #region Gyroscope

        /// <summary>
        /// Gyroscope bias data
        /// </summary>
        public Vector3 GyroscopeBias => _gyroscopeBias;

        /// <summary>
        /// Get or set the gyroscope range
        /// </summary>
        public GyroscopeRange GyroscopeRange
        {
            get => _gyroscopeRange;

            set
            {
                WriteRegister(Register.GYRO_CONFIG, (byte)((byte)value << 3));
                _gyroscopeRange = (GyroscopeRange)((ReadByte(Register.GYRO_CONFIG) >> 3) & 0x03);
                if (_gyroscopeRange != value)
                {
                    throw new IOException($"Can set {nameof(GyroscopeRange)}, desired value {value}, stored value {_gyroscopeRange}");
                }
            }
        }

        /// <summary>
        /// Get or set the gyroscope bandwidth
        /// </summary>
        public Mpu6050GyroBandwidth GyroscopeBandwidth
        {
            get => _gyroscopeBandwidth;

            set
            {
                WriteRegister(Register.CONFIG, (byte)value);

                _gyroscopeBandwidth = (Mpu6050GyroBandwidth)ReadByte(Register.CONFIG);

                if (_gyroscopeBandwidth != value)
                {
                    throw new IOException($"Cannot set {nameof(GyroscopeBandwidth)}, desired value {value}, stored value {_gyroscopeBandwidth}. Device still booting?");
                }
            }
        }

        /// <summary>
        /// Get the real gyroscope scale. This allows to calculate the real
        /// angular rate in degree per second
        /// </summary>
        public float GyroscopeScale
        {
            get
            {
                float val = 0;
                switch (GyroscopeRange)
                {
                    case GyroscopeRange.Range0250Dps:
                        val = 131.0f;
                        break;
                    case GyroscopeRange.Range0500Dps:
                        val = 65.5f;
                        break;
                    case GyroscopeRange.Range1000Dps:
                        val = 32.8f;
                        break;
                    case GyroscopeRange.Range2000Dps:
                        val = 16.4f;
                        break;
                    default:
                        break;
                }

                return 1.0f / val;
            }
        }

        /// <summary>
        /// Get the gyroscope in degrees per seconds
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
        public Vector3 GetGyroscopeReading() => GetRawGyroscope() * GyroscopeScale;

        private Vector3 GetRawGyroscope()
        {
            Span<byte> rawData = stackalloc byte[6]
            {
                0, 0, 0, 0, 0, 0
            };
            Vector3 gyro = new Vector3();
            ReadBytes(Register.GYRO_XOUT_H, rawData);
            gyro.X = BinaryPrimitives.ReadInt16BigEndian(rawData);
            gyro.Y = BinaryPrimitives.ReadInt16BigEndian(rawData.Slice(2));
            gyro.Z = BinaryPrimitives.ReadInt16BigEndian(rawData.Slice(4));
            return gyro;
        }

        #endregion

        #region Temperature

        /// <summary>
        /// Get the temperature
        /// </summary>
        public Temperature GetTemperature()
        {
            Span<byte> rawData = stackalloc byte[2]
            {
                0, 0
            };
            ReadBytes(Register.TEMP_OUT_H, rawData);
            // formula from the documentation
            return Temperature.FromCelsius((BinaryPrimitives.ReadInt16BigEndian(rawData) / 340.0) + 36.53);
        }

        #endregion

        #region Modes, constructor, Dispose

        /// <summary>
        /// Setup the Wake On Motion. This mode generate a rising signal on pin INT
        /// You can catch it with a normal GPIO and place an interruption on it if supported
        /// Reading the sensor won't give any value until it wakes up periodically
        /// Only Accelerator data is available in this mode
        /// </summary>
        /// <param name="accelerometerThreshold">Threshold of magnetometer x/y/z axes. LSB = 4mg. Range is 0mg to 1020mg</param>
        /// <param name="acceleratorLowPower">Frequency used to measure data for the low power consumption mode</param>
        public void SetWakeOnMotion(uint accelerometerThreshold, AccelerometerLowPowerFrequency acceleratorLowPower)
        {
            throw new NotImplementedException();
        }

        internal void Reset()
        {
            WriteRegister(Register.PWR_MGMT_1, 0x80);
            // Maximum risen time is 100 ms after VDD
            Thread.Sleep(100);

            // this should be a soft reset, choosing gyroscope 1 as the clock source
            WriteRegister(Register.PWR_MGMT_1, 0x01);
            // Maximum risen time is 100 ms after VDD
            Thread.Sleep(100);
            WriteRegister(Register.PWR_MGMT_1, 0x01);
            int timeout = 1000;
            // It appears that after a reset, we need to wait quite long until the device gets ready
            // If it is not, writting to the config register just doesn't do anything (which makes later
            // setting the Gyroscope bandwith fail) - this behavior is not documented, but easily reproducible
            while (timeout-- > 0)
            {
                WriteRegister(Register.CONFIG, 1);
                byte read = ReadByte(Register.CONFIG);
                if (read == 1)
                {
                    break;
                }

                Thread.Sleep(10);
            }

            // We leave the config register at 0x01, DLPF_CFG = 1, which is suggested
            // Set the sample rate register to 1, which makes it 1khz (equal for all sensors)
            WriteRegister(Register.SMPLRT_DIV, 0);
            _wakeOnMotion = false;

            // Set defaults
            GyroscopeRange = GyroscopeRange.Range0250Dps;
            GyroscopeBandwidth = Mpu6050GyroBandwidth.BandWidth188Hz;
            AccelerometerRange = AccelerometerRange.Range02G;
        }

        /// <summary>
        /// Return true if the version of MPU6500 is the correct one
        /// </summary>
        /// <returns>True if success</returns>
        internal bool CheckVersion()
        {
            // Check if the version is thee correct one
            return ReadByte(Register.WHO_AM_I) == 0x68;
        }

        /// <summary>
        /// Get or set the sample diver mode
        /// </summary>
        public byte SampleRateDivider
        {
            get { return ReadByte(Register.SMPLRT_DIV); }
            set { WriteRegister(Register.SMPLRT_DIV, value); }
        }

        /// <summary>
        /// Get or set the elements to disable.
        /// It can be any axes of the accelerometer and or the gyroscope
        /// </summary>
        public DisableModes DisableModes
        {
            get { return (DisableModes)ReadByte(Register.PWR_MGMT_2); }
            set { WriteRegister(Register.PWR_MGMT_2, (byte)value); }
        }

        #endregion

        #region FIFO

        /// <summary>
        /// Get the number of elements to read from the FIFO (First In First Out) buffer
        /// </summary>
        public uint FifoCount
        {
            get
            {
                Span<byte> rawData = stackalloc byte[2]
                {
                    0, 0
                };
                ReadBytes(Register.FIFO_COUNTH, rawData);
                return BinaryPrimitives.ReadUInt16BigEndian(rawData);
            }
        }

        /// <summary>
        /// Get or set the FIFO (First In First Out) modes
        /// </summary>
        public FifoModes FifoModes
        {
            get
            {
                return (FifoModes)(ReadByte(Register.FIFO_EN));
            }
            set
            {
                if (value != FifoModes.None)
                {
                    // Make sure the FIFO is enabled
                    var usrCtl = (UserControls)ReadByte(Register.USER_CTRL);
                    usrCtl = usrCtl | UserControls.FIFO_RST;
                    WriteRegister(Register.USER_CTRL, (byte)usrCtl);
                }
                else
                {
                    // Deactivate FIFO
                    var usrCtl = (UserControls)ReadByte(Register.USER_CTRL);
                    usrCtl = usrCtl & ~UserControls.FIFO_RST;
                    WriteRegister(Register.USER_CTRL, (byte)usrCtl);
                }

                WriteRegister(Register.FIFO_EN, (byte)value);
            }
        }

        /// <summary>
        /// Read data in the FIFO (First In First Out) buffer, read as many data as the size of readData byte span
        /// You should read the number of data available in the FifoCount property then
        /// read them here.
        /// You will read only data you have selected in FifoModes.
        /// Data are in the order of the Register from 0x3B to 0x60.
        /// ACCEL_XOUT_H and ACCEL_XOUT_L
        /// ACCEL_YOUT_H and ACCEL_YOUT_L
        /// ACCEL_ZOUT_H and ACCEL_ZOUT_L
        /// TEMP_OUT_H and TEMP_OUT_L
        /// GYRO_XOUT_H and GYRO_XOUT_L
        /// GYRO_YOUT_H and GYRO_YOUT_L
        /// GYRO_ZOUT_H and GYRO_ZOUT_L
        /// EXT_SENS_DATA_00 to EXT_SENS_DATA_24
        /// </summary>
        /// <param name="readData">Data which will be read</param>
        public void ReadFifo(Span<byte> readData)
        {
            ReadBytes(Register.FIFO_R_W, readData);
        }

        #endregion

        #region Calibration and tests

        /// <summary>
        /// Runs a self test of the MPU
        /// </summary>
        /// <returns>the gyroscope and accelerometer error values (change from factory trim)</returns>
        public (Vector3 gyroscopeErrorPercentage, Vector3 accelerometerErrorPercentage, bool pass) RunGyroscopeAccelerometerSelfTest()
        {
            // Used for the number of cycles to run the test
            // Value is 200 according to documentation AN-MPU-9250A-03
            const int numCycles = 200;

            Vector3 accAverage = new Vector3();
            Vector3 gyroAverage = new Vector3();
            Vector3 accSelfTestAverage = new Vector3();
            Vector3 gyroSelfTestAverage = new Vector3();

            // Setup the registers for Gyroscope as in documentation
            // DLPF Config | LPF BW | Sampling Rate | Filter Delay
            // 2           | 92Hz   | 1kHz          | 3.9ms
            WriteRegister(Register.SMPLRT_DIV, 0x00);
            WriteRegister(Register.CONFIG, 0x02);
            GyroscopeRange = GyroscopeRange.Range0250Dps;
            // Set full scale range for the gyro to 250 dps
            // Setup the registers for accelerometer as in documentation
            // DLPF Config | LPF BW | Sampling Rate | Filter Delay
            // 2           | 92Hz   | 1kHz          | 7.8ms
            WriteRegister(Register.ACCEL_CONFIG_2, 0x02);
            AccelerometerRange = AccelerometerRange.Range02G;

            // Read the data 200 times as per the documentation page 5
            for (int reading = 0; reading < numCycles; reading++)
            {
                gyroAverage = GetRawGyroscope();
                accAverage = GetRawAccelerometer();
            }

            accAverage /= numCycles;
            gyroAverage /= numCycles;

            // Set USR_Reg: (1Bh) Gyro_Config, gdrive_axisCTST [0-2] to b111 to enable Self-Test.
            WriteRegister(Register.ACCEL_CONFIG, 0xE0);
            // Set USR_Reg: (1Ch) Accel_Config, AX/Y/Z_ST_EN   [0-2] to b111 to enable Self-Test.
            WriteRegister(Register.GYRO_CONFIG, 0xE0);
            // Wait 20ms for oscillations to stabilize
            Thread.Sleep(20);

            // Read the gyroscope and accelerometer output at a 1kHz rate and average 200 readings.
            // The averaged values will be the LSB of GX_ST_OS, GY_ST_OS, GZ_ST_OS, AX_ST_OS, AY_ST_OS and AZ_ST_OS in the software
            for (int reading = 0; reading < numCycles; reading++)
            {
                gyroSelfTestAverage = GetRawGyroscope();
                accSelfTestAverage = GetRawAccelerometer();
            }

            accSelfTestAverage /= numCycles;
            gyroSelfTestAverage /= numCycles;

            // Procedure as outlined in MPU6000 Register Doc, page 9f
            byte selfTestX = ReadByte(0x0D);
            byte selfTestY = ReadByte(0x0E);
            byte selfTestZ = ReadByte(0x0F);
            byte selfTestA = ReadByte(0x10);
            int xgTest = selfTestX & 0x1F; // Lower 5 bits
            int ygTest = selfTestY & 0x1F;
            int zgTest = selfTestZ & 0x1F;

            double ftXg = 0;
            if (xgTest != 0)
            {
                ftXg = 25.0 * 131.0 * Math.Pow(1.046, xgTest - 1.0);
            }

            double ftYg = 0;
            if (ygTest != 0)
            {
                ftYg = -25.0 * 131.0 * Math.Pow(1.046, ygTest - 1.0);
            }

            double ftZg = 0;
            if (zgTest != 0)
            {
                ftZg = 25.0 * 131.0 * Math.Pow(1.046, zgTest - 1.0);
            }

            // Calculate self test response
            var str = gyroSelfTestAverage - gyroAverage;
            // Calculate change of trim value, and convert to a percentage
            double changeOfTrimX = 100 + (100 * (str.X - ftXg) / ftXg);
            double changeOfTrimY = 100 + (100 * (str.Y - ftYg) / ftYg);
            double changeOfTrimZ = 100 + (100 * (str.Z - ftZg) / ftZg);

            // If the gyros are Ok, these values should all be +/- 14% (see Datasheet, p 12)
            Vector3 gyroSelfTestResult = new Vector3((float)changeOfTrimX, (float)changeOfTrimY, (float)changeOfTrimZ);

            int xaTest = (selfTestX >> 3) | (selfTestA & 0x30) >> 4; // XA_TEST result is a five-bit unsigned integer
            int yaTest = (selfTestY >> 3) | (selfTestA & 0x0C) >> 2; // YA_TEST result is a five-bit unsigned integer
            int zaTest = (selfTestZ >> 3) | (selfTestA & 0x03) >> 0; // ZA_TEST result is a five-bit unsigned integer

            // Calculate all factory trim
            double ftXa = 0;
            if (xaTest != 0)
            {
                ftXa = (4096.0 * 0.34) * (Math.Pow((0.92 / 0.34), ((xaTest - 1.0) / 30.0)));
            }

            double ftYa = 0;
            if (yaTest != 0)
            {
                ftYa = (4096.0 * 0.34) * (Math.Pow((0.92 / 0.34), ((yaTest - 1.0) / 30.0)));
            }

            double ftZa = 0;
            if (zaTest != 0)
            {
                ftZa = (4096.0 * 0.34) * (Math.Pow((0.92 / 0.34), ((zaTest - 1.0) / 30.0)));
            }

            // Calculate self test response
            str = accSelfTestAverage - accAverage;
            // Calculate change of trim value, and convert to a percentage
            changeOfTrimX = 100 + (100 * (str.X - ftXa) / ftXa);
            changeOfTrimY = 100 + (100 * (str.Y - ftYa) / ftYa);
            changeOfTrimZ = 100 + (100 * (str.Z - ftZa) / ftZa);

            // If the gyros are Ok, these values should all be +/- 14% (see Datasheet, p 12)
            Vector3 accelSelfTestResult = new Vector3((float)changeOfTrimX, (float)changeOfTrimY, (float)changeOfTrimZ);

            // To cleanup the configuration after the test
            // Set USR_Reg: (1Bh) Gyro_Config, gdrive_axisCTST [0-2] to b000.
            WriteRegister(Register.ACCEL_CONFIG, 0x00);
            // Set USR_Reg: (1Ch) Accel_Config, AX/Y/Z_ST_EN [0-2] to b000.
            WriteRegister(Register.GYRO_CONFIG, 0x00);
            // Wait 20ms for oscillations to stabilize
            Thread.Sleep(20);

            bool pass = Math.Abs(gyroSelfTestResult.X) <= 14.0 && Math.Abs(gyroSelfTestResult.Y) <= 14.0 && Math.Abs(gyroSelfTestResult.Z) <= 14.0 &&
                Math.Abs(accelSelfTestResult.X) <= 14.0 && Math.Abs(accelSelfTestResult.Y) <= 14.0 && Math.Abs(accelSelfTestResult.Z) <= 14.0;

            return (gyroSelfTestResult, accelSelfTestResult, pass);
        }

        #endregion

        #region I2C

        /// <summary>
        /// Write data on any of the I2C slave attached to the MPU9250
        /// </summary>
        /// <param name="i2cChannel">The slave channel to attached to the I2C device</param>
        /// <param name="address">The I2C address of the slave I2C element</param>
        /// <param name="register">The register to write to the slave I2C element</param>
        /// <param name="data">The byte data to write to the slave I2C element</param>
        public void WriteByteToSlaveDevice(I2cChannel i2cChannel, byte address, byte register, byte data)
        {
            // I2C_SLVx_ADDR += 3 * i2cChannel
            byte slvAddress = (byte)((byte)Register.I2C_SLV0_ADDR + 3 * (byte)i2cChannel);
            Span<byte> dataout = stackalloc byte[2]
            {
                slvAddress, address
            };
            _i2cDevice.Write(dataout);
            // I2C_SLVx_REG = I2C_SLVx_ADDR + 1
            dataout[0] = (byte)(slvAddress + 1);
            dataout[1] = register;
            _i2cDevice.Write(dataout);
            // I2C_SLVx_D0 =  I2C_SLV0_DO + i2cChannel
            // Except Channel4
            byte channelData = i2cChannel != I2cChannel.Slave4 ? (byte)((byte)Register.I2C_SLV0_DO + (byte)i2cChannel) : (byte)Register.I2C_SLV4_DO;
            dataout[0] = channelData;
            dataout[1] = data;
            _i2cDevice.Write(dataout);
            // I2C_SLVx_CTRL = I2C_SLVx_ADDR + 2
            dataout[0] = (byte)(slvAddress + 2);
            dataout[1] = 0x81;
            _i2cDevice.Write(dataout);
        }

        /// <summary>
        /// Read data from any of the I2C slave attached to the MPU9250
        /// </summary>
        /// <param name="i2cChannel">The slave channel to attached to the I2C device</param>
        /// <param name="address">The I2C address of the slave I2C element</param>
        /// <param name="register">The register to read from the slave I2C element</param>
        /// <param name="readBytes">The read data</param>
        public void ReadByteFromSlaveDevice(I2cChannel i2cChannel, byte address, byte register, Span<byte> readBytes)
        {
            if (readBytes.Length > 24)
            {
                throw new ArgumentException($"Can't read more than 24 bytes at once");
            }

            byte slvAddress = (byte)((byte)Register.I2C_SLV0_ADDR + 3 * (byte)i2cChannel);
            Span<byte> dataout = stackalloc byte[2]
            {
                slvAddress, (byte)(address | 0x80)
            };
            _i2cDevice.Write(dataout);
            dataout[0] = (byte)(slvAddress + 1);
            dataout[1] = (byte)register;
            _i2cDevice.Write(dataout);
            dataout[0] = (byte)(slvAddress + 2);
            dataout[1] = (byte)(0x80 | readBytes.Length);
            _i2cDevice.Write(dataout);
            // Just need to wait a very little bit
            // For data transfer to happen and process on the MPU9250 side
            DelayHelper.DelayMicroseconds(140 + readBytes.Length * 10, false);
            _i2cDevice.WriteByte((byte)Register.EXT_SENS_DATA_00);
            _i2cDevice.Read(readBytes);
        }

        internal void WriteRegister(Register reg, byte data)
        {
            Span<byte> dataout = stackalloc byte[]
            {
                (byte)reg, data
            };
            _i2cDevice.Write(dataout);
        }

        internal byte ReadByte(Register reg)
        {
            _i2cDevice.WriteByte((byte)reg);
            return _i2cDevice.ReadByte();
        }

        internal byte ReadByte(byte reg)
        {
            _i2cDevice.WriteByte(reg);
            return _i2cDevice.ReadByte();
        }

        internal void ReadBytes(Register reg, Span<byte> readBytes)
        {
            _i2cDevice.WriteByte((byte)reg);
            _i2cDevice.Read(readBytes);
        }

        /// <summary>
        /// Cleanup everything
        /// </summary>
        public void Dispose()
        {
            _i2cDevice?.Dispose();
            _i2cDevice = null;
        }

        #endregion
    }
}
