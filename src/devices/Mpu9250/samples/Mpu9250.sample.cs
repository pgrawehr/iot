// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.I2c;
using System.IO;
using System.Net;
using System.Threading;
using Iot.Device.Imu;

namespace DemoMpu9250
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello MPU9250!");

            if (args.Length != 0)
            {
                MagnetometerCalibrationDeepDive(Convert.ToInt32(args[0]));
            }
            else
            {
                Console.WriteLine("If you want to run a deep dive calibration data export, run this sample with an argument for the number of calibration cycles you want:");
                Console.WriteLine("To run a calibration with 1000 sample and exporting all data: ./Mpu9250.sample 1000");
                MainTest();
            }
        }

        public static void MagnetometerCalibrationDeepDive(int calibrationCount)
        {
            var mpui2CConnectionSettingmpus = new I2cConnectionSettings(1, Mpu9250.DefaultI2cAddress);
            Mpu9250 mpu9250 = new Mpu9250(I2cDevice.Create(mpui2CConnectionSettingmpus));
            mpu9250.MagnetometerOutputBitMode = Iot.Device.Magnetometer.OutputBitMode.Output16bit;
            mpu9250.MagnetometerMeasurementMode = Iot.Device.Magnetometer.MeasurementMode.ContinuousMeasurement100Hz;
            Console.WriteLine("Please move the magnetometer during calibration");
            using (var ioWriter = new StreamWriter("mag.csv"))
            {
                // First we read the data without calibration at all
                Console.WriteLine("Reading magnetometer data without calibration");
                ioWriter.WriteLine($"X;Y;Z");
                for (int i = 0; i < calibrationCount; i++)
                {
                    try
                    {
                        var magne = mpu9250.ReadMagnetometerWithoutCorrection();
                        ioWriter.WriteLine($"{magne.X};{magne.Y};{magne.Z}");
                        // 10 ms = 100Hz, so waiting to make sure we have new data
                        Thread.Sleep(10);
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("Error reading");
                    }
                }

                Console.WriteLine("Performing calibration");
                // then we calibrate
                var magnetoBias = mpu9250.CalibrateMagnetometer(calibrationCount);
                ioWriter.WriteLine();
                ioWriter.WriteLine("Factory calibration data");
                ioWriter.WriteLine($"X;Y;Z");
                ioWriter.WriteLine($"{magnetoBias.X};{magnetoBias.Y};{magnetoBias.Z}");
                ioWriter.WriteLine();
                ioWriter.WriteLine("Magnetometer bias calibration data");
                ioWriter.WriteLine($"X;Y;Z");
                ioWriter.WriteLine($"{mpu9250.MagnometerBias.X};{mpu9250.MagnometerBias.Y};{mpu9250.MagnometerBias.Z}");
                ioWriter.WriteLine();
                // Finally we read the data again
                Console.WriteLine("Reading magnetometer data including calibration");
                ioWriter.WriteLine($"X corr;Y corr;Z corr");
                for (int i = 0; i < calibrationCount; i++)
                {
                    try
                    {
                        var magne = mpu9250.ReadMagnetometer();
                        ioWriter.WriteLine($"{magne.X};{magne.Y};{magne.Z}");
                        // 10 ms = 100Hz, so waiting to make sure we have new data
                        Thread.Sleep(10);
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("Error reading");
                    }
                }
            }

            Console.WriteLine("Calibration deep dive over, file name is mag.csv");
        }

        public static void MainTest()
        {
            var mpui2CConnectionSettingmpus = new I2cConnectionSettings(1, Mpu9250.DefaultI2cAddress);
            var mpu9250 = new Mpu6050(I2cDevice.Create(mpui2CConnectionSettingmpus));
            Console.WriteLine("Attach debugger and press enter");
            Console.ReadLine();
            var resSelfTest = mpu9250.RunGyroscopeAccelerometerSelfTest();
            Console.WriteLine($"Self test:");
            Console.WriteLine($"Gyro X = {resSelfTest.Item1.X}% (should be < +/-14%)");
            Console.WriteLine($"Gyro Y = {resSelfTest.Item1.Y}% (should be < +/-14%)");
            Console.WriteLine($"Gyro Z = {resSelfTest.Item1.Z}% (should be < +/-14%)");
            Console.WriteLine($"Acc X = {resSelfTest.Item2.X}% (should be < +/-14%)");
            Console.WriteLine($"Acc Y = {resSelfTest.Item2.Y}% (should be < +/-14%)");
            Console.WriteLine($"Acc Z = {resSelfTest.Item2.Z}% (should be < +/-14%)");
            if (resSelfTest.pass)
            {
                Console.WriteLine($"Self test PASSED");
            }
            else
            {
                Console.WriteLine($"Self test FAILED");
            }

            Console.WriteLine("Running Gyroscope and Accelerometer calibration");
            // mpu9250.CalibrateGyroscopeAccelerometer();
            Console.WriteLine("Calibration results:");
            Console.WriteLine($"Gyro X bias = {mpu9250.GyroscopeBias.X}");
            Console.WriteLine($"Gyro Y bias = {mpu9250.GyroscopeBias.Y}");
            Console.WriteLine($"Gyro Z bias = {mpu9250.GyroscopeBias.Z}");
            Console.WriteLine($"Acc X bias = {mpu9250.AccelerometerBias.X}");
            Console.WriteLine($"Acc Y bias = {mpu9250.AccelerometerBias.Y}");
            Console.WriteLine($"Acc Z bias = {mpu9250.AccelerometerBias.Z}");
            Console.WriteLine("Press a key to continue");
            var readKey = Console.ReadKey();
            mpu9250.GyroscopeBandwidth = Mpu6050GyroBandwidth.BandWidth188Hz;
            Console.Clear();

            while (!Console.KeyAvailable)
            {
                Console.CursorTop = 0;
                var gyro = mpu9250.GetGyroscopeReading();
                Console.WriteLine($"Gyro X = {gyro.X,15}");
                Console.WriteLine($"Gyro Y = {gyro.Y,15}");
                Console.WriteLine($"Gyro Z = {gyro.Z,15}");
                var acc = mpu9250.GetAccelerometer();
                Console.WriteLine($"Acc X = {acc.X,15}");
                Console.WriteLine($"Acc Y = {acc.Y,15}");
                Console.WriteLine($"Acc Z = {acc.Z,15}");
                Console.WriteLine($"Temp = {mpu9250.GetTemperature().Celsius.ToString("0.00")} °C");
                Thread.Sleep(100);
            }

            readKey = Console.ReadKey();
            // SetWakeOnMotion
            mpu9250.SetWakeOnMotion(300, AccelerometerLowPowerFrequency.Frequency0Dot24Hz);
            // You'll need to attach the INT pin to a GPIO and read the level. Once going up, you have
            // some data and the sensor is awake
            // In order to simulate this without a GPIO pin, you will see that the refresh rate is very low
            // Setup here at 0.24Hz which means, about every 4 seconds
            Console.Clear();

            while (!Console.KeyAvailable)
            {
                Console.CursorTop = 0;
                var acc = mpu9250.GetAccelerometer();
                Console.WriteLine($"Acc X = {acc.X,15}");
                Console.WriteLine($"Acc Y = {acc.Y,15}");
                Console.WriteLine($"Acc Z = {acc.Z,15}");
                Thread.Sleep(100);
            }
        }
    }
}
