﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Device;
using System.Device.I2c;
using System.Device.Model;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;
using Iot.Device.Magnetometer;
using UnitsNet;

namespace Iot.Device.Imu
{
    /// <summary>
    /// MPU6050 - gyroscope, accelerometer and temperature sensor
    /// </summary>
    [Interface("MPU6050 - gyroscope, accelerometer and temperature sensor")]
    public class Mpu6050 : IDisposable
    {
        /// <summary>
        /// Default address for MPU9250
        /// </summary>
        public const byte DefaultI2cAddress = 0x68;

        /// <summary>
        /// Second address for MPU9250
        /// </summary>
        public const byte SecondI2cAddress = 0x69;

        private const float Adc = 0x8000;
        private const float Gravity = 9.807f;
        internal I2cDevice _i2cDevice;
        private AccelerometerRange _accelerometerRange;
        private GyroscopeRange _gyroscopeRange;
        private AccelerometerBandwidth _accelerometerBandwidth;
        private GyroscopeBandwidth _gyroscopeBandwidth;
        internal bool _wakeOnMotion;

        /// <summary>
        /// Initialize the MPU6050
        /// </summary>
        /// <param name="i2cDevice">The I2C device</param>
        public Mpu6050(I2cDevice i2cDevice)
        {
            if (i2cDevice == null)
            {
                throw new ArgumentNullException(nameof(i2cDevice), $"Variable i2cDevice is null");
            }

            _i2cDevice = i2cDevice;
            Reset();
            PowerOn();
            if (!CheckVersion())
            {
                throw new IOException($"This device does not contain the correct signature 0x68 for a MPU6050");
            }

            GyroscopeBandwidth = GyroscopeBandwidth.Bandwidth0250Hz;
            GyroscopeRange = GyroscopeRange.Range0250Dps;
            AccelerometerBandwidth = AccelerometerBandwidth.Bandwidth1130Hz;
            AccelerometerRange = AccelerometerRange.Range02G;
        }

        /// <summary>
        /// Used to create the class for the MPU9250. Initialization is a bit different than for the MPU6050
        /// </summary>
        internal Mpu6050(I2cDevice i2cDevice, bool isInternal)
        {
            _i2cDevice = i2cDevice ?? throw new ArgumentNullException(nameof(i2cDevice));
        }

        #region Accelerometer

        /// <summary>
        /// Get or set the accelerometer range
        /// </summary>
        [Property]
        public AccelerometerRange AccelerometerRange
        {
            get => _accelerometerRange;
            set
            {
                WriteRegister(Register.ACCEL_CONFIG, (byte)((byte)value << 3));
                // We will cache the range to avoid i2c access every time this data is requested
                // This allow as well to make sure the stored data is the same as the one read
                _accelerometerRange = (AccelerometerRange)(ReadByte(Register.ACCEL_CONFIG) >> 3);
                if (_accelerometerRange != value)
                {
                    throw new IOException($"Can set {nameof(AccelerometerRange)}, desired value {value}, stored value {_accelerometerRange}");
                }
            }
        }

        /// <summary>
        /// Get or set the accelerometer bandwidth
        /// </summary>
        [Property]
        public AccelerometerBandwidth AccelerometerBandwidth
        {
            get => _accelerometerBandwidth;
            set
            {
                WriteRegister(Register.ACCEL_CONFIG_2, (byte)value);
                _accelerometerBandwidth = (AccelerometerBandwidth)ReadByte(Register.ACCEL_CONFIG_2);
                if (_accelerometerBandwidth != value)
                {
                    throw new IOException($"Can set {nameof(AccelerometerBandwidth)}, desired value {value}, stored value {_accelerometerBandwidth}");
                }
            }
        }

        /// <summary>
        /// Get the real accelerometer bandwidth. This allows to calculate the real
        /// degree per second
        /// </summary>
        [Property]
        public float AccelerationScale
        {
            get
            {
                float val = AccelerometerRange switch
                {
                    AccelerometerRange.Range02G => 2.0f,
                    AccelerometerRange.Range04G => 4.0f,
                    AccelerometerRange.Range08G => 8.0f,
                    AccelerometerRange.Range16G => 16.0f,
                    _ => 0,
                };

                val = (val * Gravity) / Adc;
                return val / (1 + SampleRateDivider);
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
        [Telemetry("Acceleration")]
        public Vector3 GetAccelerometer() => GetRawAccelerometer() * AccelerationScale;

        /// <summary>
        /// Gets the raw accelerometer data
        /// </summary>
        /// <returns></returns>
        internal Vector3 GetRawAccelerometer()
        {
            Span<byte> rawData = stackalloc byte[6]
            {
                0,
                0,
                0,
                0,
                0,
                0
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
        [Property]
        public AccelerometerLowPowerFrequency AccelerometerLowPowerFrequency
        {
            get => (AccelerometerLowPowerFrequency)ReadByte(Register.LP_ACCEL_ODR);
            set => WriteRegister(Register.LP_ACCEL_ODR, (byte)value);
        }

        #endregion

        #region Gyroscope

        /// <summary>
        /// Get or set the gyroscope range
        /// </summary>
        [Property]
        public GyroscopeRange GyroscopeRange
        {
            get => _gyroscopeRange;
            set
            {
                WriteRegister(Register.GYRO_CONFIG, (byte)((byte)value << 3));
                _gyroscopeRange = (GyroscopeRange)(ReadByte(Register.GYRO_CONFIG) >> 3);
                if (_gyroscopeRange != value)
                {
                    throw new IOException($"Can set {nameof(GyroscopeRange)}, desired value {value}, stored value {_gyroscopeRange}");
                }
            }
        }

        /// <summary>
        /// Get or set the gyroscope bandwidth
        /// </summary>
        [Property]
        public GyroscopeBandwidth GyroscopeBandwidth
        {
            get => _gyroscopeBandwidth;
            set
            {
                if (value == GyroscopeBandwidth.Bandwidth8800HzFS32)
                {
                    WriteRegister(Register.GYRO_CONFIG, (byte)((byte)GyroscopeRange | 0x01));
                }
                else if (value == GyroscopeBandwidth.Bandwidth3600HzFS32)
                {
                    WriteRegister(Register.GYRO_CONFIG, (byte)((byte)GyroscopeRange | 0x02));
                }
                else
                {
                    WriteRegister(Register.GYRO_CONFIG, (byte)GyroscopeRange);
                    WriteRegister(Register.CONFIG, (byte)value);
                }

                var retConf = ReadByte(Register.GYRO_CONFIG);
                if ((retConf & 0x01) == 0x01)
                {
                    _gyroscopeBandwidth = GyroscopeBandwidth.Bandwidth8800HzFS32;
                }
                else if ((retConf & 0x03) == 0x00)
                {
                    _gyroscopeBandwidth = (GyroscopeBandwidth)ReadByte(Register.CONFIG);
                }
                else
                {
                    _gyroscopeBandwidth = GyroscopeBandwidth.Bandwidth3600HzFS32;
                }

                if (_gyroscopeBandwidth != value)
                {
                    throw new IOException($"Can set {nameof(GyroscopeBandwidth)}, desired value {value}, stored value {_gyroscopeBandwidth}");
                }
            }
        }

        /// <summary>
        /// Get the real gyroscope bandwidth. This allows to calculate the real
        /// angular rate in degree per second
        /// </summary>
        [Property]
        public float GyroscopeScale
        {
            get
            {
                float val = GyroscopeRange switch
                {
                    GyroscopeRange.Range0250Dps => 250.0f,
                    GyroscopeRange.Range0500Dps => 500.0f,
                    GyroscopeRange.Range1000Dps => 1000.0f,
                    GyroscopeRange.Range2000Dps => 2000.0f,
                    _ => 0,
                };

                val /= Adc;
                // the sample rate diver only apply for the non FS modes
                if ((GyroscopeBandwidth != GyroscopeBandwidth.Bandwidth3600HzFS32) &&
                    (GyroscopeBandwidth != GyroscopeBandwidth.Bandwidth8800HzFS32))
                {
                    return val / (1 + SampleRateDivider);
                }

                return val;
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
        [Telemetry("AngularRate")]
        public Vector3 GetGyroscopeReading() => GetRawGyroscope() * GyroscopeScale;

        /// <summary>
        /// Gets the raw gyroscope data
        /// </summary>
        /// <returns></returns>
        internal Vector3 GetRawGyroscope()
        {
            Span<byte> rawData = stackalloc byte[6]
            {
                0,
                0,
                0,
                0,
                0,
                0
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
        [Telemetry("Temperature")]
        public Temperature GetTemperature()
        {
            Span<byte> rawData = stackalloc byte[2]
            {
                0,
                0
            };
            ReadBytes(Register.TEMP_OUT_H, rawData);
            // formula from the documentation
            return Temperature.FromDegreesCelsius((BinaryPrimitives.ReadInt16BigEndian(rawData) - 21) / 333.87 + 21);
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
        [Command]
        public void SetWakeOnMotion(uint accelerometerThreshold, AccelerometerLowPowerFrequency acceleratorLowPower)
        {
            // Using documentation page 31 of Product Specification to setup
            _wakeOnMotion = true;
            if (accelerometerThreshold > 1020)
            {
                throw new ArgumentException($"Value has to be between 0mg and 1020mg", nameof(accelerometerThreshold));
            }

            // LSB = 4mg
            accelerometerThreshold /= 4;
            // Make sure we start from a clean soft reset
            PowerOn();
            // PWR_MGMT_1 (0x6B) make CYCLE =0, SLEEP = 0  and STANDBY = 0
            WriteRegister(Register.PWR_MGMT_1, (byte)ClockSource.Internal20MHz);
            // PWR_MGMT_2 (0x6C) set DIS_XA, DIS_YA, DIS_ZA = 0 and DIS_XG, DIS_YG, DIS_ZG = 1
            // Remove the Gyroscope
            WriteRegister(Register.PWR_MGMT_2, (byte)(DisableModes.DisableGyroscopeX | DisableModes.DisableGyroscopeY | DisableModes.DisableGyroscopeZ));
            // ACCEL_CONFIG 2 (0x1D) set ACCEL_FCHOICE_B = 0 and A_DLPFCFG[2:0]=1(b001)
            // Bandwidth for Accelerator to 184Hz
            AccelerometerBandwidth = AccelerometerBandwidth.Bandwidth0184Hz;
            // Enable Motion Interrupt
            //  In INT_ENABLE (0x38), set the whole register to 0x40 to enable motion interrupt only
            WriteRegister(Register.INT_ENABLE, 0x40);
            // Enable AccelHardware Intelligence:
            // In MOT_DETECT_CTRL (0x69), set ACCEL_INTEL_EN = 1 and ACCEL_INTEL_MODE  = 1
            WriteRegister(Register.MOT_DETECT_CTRL, 0b1100_0000);
            // Set Motion Threshold:
            // In WOM_THR (0x1F), set the WOM_Threshold[7:0] to 1~255 LSBs (0~1020mg)
            WriteRegister(Register.WOM_THR, (byte)accelerometerThreshold);
            // Set Frequency of Wake-up:
            // In LP_ACCEL_ODR (0x1E), set Lposc_clksel[3:0] = 0.24Hz ~ 500Hz
            WriteRegister(Register.LP_ACCEL_ODR, (byte)acceleratorLowPower);
            // Enable Cycle Mode (AccelLow Power Mode):
            // In PWR_MGMT_1 (0x6B) make CYCLE =1
            WriteRegister(Register.PWR_MGMT_1, 0b0010_0000);
            // Motion Interrupt Configuration Completed
        }

        internal void Reset()
        {
            WriteRegister(Register.PWR_MGMT_1, 0x80);
            // http://www.invensense.com/wp-content/uploads/2015/02/PS-MPU-9250A-01-v1.1.pdf, section 4.23.
            // Maximum risen time is 100 ms after VDD
            Thread.Sleep(100);
            _wakeOnMotion = false;
        }

        internal void PowerOn()
        {
            // this should be a soft reset
            WriteRegister(Register.PWR_MGMT_1, 0x01);
            // http://www.invensense.com/wp-content/uploads/2015/02/PS-MPU-9250A-01-v1.1.pdf, section 4.23.
            // Maximum risen time is 100 ms after VDD
            Thread.Sleep(100);
            _wakeOnMotion = false;
        }

        /// <summary>
        /// Return true if the version of MPU6050 is the correct one
        /// </summary>
        /// <returns>True if success</returns>
        // Check if the version is thee correct one
        internal bool CheckVersion() => ReadByte(Register.WHO_AM_I) == 0x68;

        /// <summary>
        /// Get or set the sample diver mode
        /// </summary>
        [Property]
        public byte SampleRateDivider
        {
            get => ReadByte(Register.SMPLRT_DIV);
            set => WriteRegister(Register.SMPLRT_DIV, value);
        }

        /// <summary>
        /// Get or set the elements to disable.
        /// It can be any axes of the accelerometer and or the gyroscope
        /// </summary>
        public DisableModes DisableModes
        {
            get => (DisableModes)ReadByte(Register.PWR_MGMT_2);
            set => WriteRegister(Register.PWR_MGMT_2, (byte)value);
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
                    0,
                    0
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
            get => (FifoModes)(ReadByte(Register.FIFO_EN));
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
        public void ReadFifo(Span<byte> readData) => ReadBytes(Register.FIFO_R_W, readData);

        #endregion

        #region I2C

        /// <summary>
        /// Write data on any of the I2C replica attached to the MPU9250
        /// </summary>
        /// <param name="i2cChannel">The replica channel to attached to the I2C device</param>
        /// <param name="address">The I2C address of the replica I2C element</param>
        /// <param name="register">The register to write to the replica I2C element</param>
        /// <param name="data">The byte data to write to the replica I2C element</param>
        public void WriteByteToReplicaDevice(I2cChannel i2cChannel, byte address, byte register, byte data)
        {
            // I2C_SLVx_ADDR += 3 * i2cChannel
            byte slvAddress = (byte)((byte)Register.I2C_SLV0_ADDR + 3 * (byte)i2cChannel);
            Span<byte> dataout = stackalloc byte[2]
            {
                slvAddress,
                address
            };
            _i2cDevice.Write(dataout);
            // I2C_SLVx_REG = I2C_SLVx_ADDR + 1
            dataout[0] = (byte)(slvAddress + 1);
            dataout[1] = register;
            _i2cDevice.Write(dataout);
            // I2C_SLVx_D0 =  I2C_SLV0_DO + i2cChannel
            // Except Channel4
            byte channelData = i2cChannel != I2cChannel.Replica4 ? (byte)((byte)Register.I2C_SLV0_DO + (byte)i2cChannel) : (byte)Register.I2C_SLV4_DO;
            dataout[0] = channelData;
            dataout[1] = data;
            _i2cDevice.Write(dataout);
            // I2C_SLVx_CTRL = I2C_SLVx_ADDR + 2
            dataout[0] = (byte)(slvAddress + 2);
            dataout[1] = 0x81;
            _i2cDevice.Write(dataout);
        }

        /// <summary>
        /// Read data from any of the I2C replica attached to the MPU9250
        /// </summary>
        /// <param name="i2cChannel">The replica channel to attached to the I2C device</param>
        /// <param name="address">The I2C address of the replica I2C element</param>
        /// <param name="register">The register to read from the replica I2C element</param>
        /// <param name="readBytes">The read data</param>
        public void ReadByteFromReplicaDevice(I2cChannel i2cChannel, byte address, byte register, Span<byte> readBytes)
        {
            if (readBytes.Length > 24)
            {
                throw new ArgumentException("Value must be 24 bytes or less.", nameof(readBytes));
            }

            byte slvAddress = (byte)((byte)Register.I2C_SLV0_ADDR + 3 * (byte)i2cChannel);
            Span<byte> dataout = stackalloc byte[2]
            {
                slvAddress,
                (byte)(address | 0x80)
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
                (byte)reg,
                data
            };
            _i2cDevice.Write(dataout);
        }

        internal byte ReadByte(Register reg)
        {
            _i2cDevice.WriteByte((byte)reg);
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
            _i2cDevice = null!;
        }

        #endregion
    }
}
