// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.I2c;
using System.IO.Ports;
using System.Numerics;
using System.Threading;

using Iot.Device.Imu;

namespace Ig500.Sample
{
    /// <summary>
    /// Test program main class
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Entry point for example program
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static void Main(string[] args)
        {
            Console.WriteLine("Hello IG-500A!");
            Console.WriteLine();

            using (SerialPort port = new SerialPort("COM3", 115200, Parity.None))
            {
                port.Open();
                using (Ig500Sensor sensor = new Ig500Sensor(port.BaseStream))
                {
                    if (!sensor.WaitForSensorReady(out var errorMessage))
                    {
                        Console.WriteLine($"Error initializing device: {errorMessage}");
                        return;
                    }

                    while (!Console.KeyAvailable)
                    {
                        var orien = sensor.Orientation;
                        Console.WriteLine($"Orientation Heading: {orien.X:F2} Roll: {orien.Y:F2} Pitch: {orien.Z:F2}");
                        Console.WriteLine($"Temperature {sensor.Temperature.Celsius}°C");
                        Thread.Sleep(100);
                        ////var magneto = bno055Sensor.Magnetometer;
                        ////Console.WriteLine($"Magnetomer X: {magneto.X} Y: {magneto.Y} Z: {magneto.Z}");
                        ////var gyro = bno055Sensor.Gyroscope;
                        ////Console.WriteLine($"Gyroscope X: {gyro.X} Y: {gyro.Y} Z: {gyro.Z}");
                        ////var accele = bno055Sensor.Accelerometer;
                        ////Console.WriteLine($"Acceleration X: {accele.X} Y: {accele.Y} Z: {accele.Z}");
                        ////var orien = bno055Sensor.Orientation;
                        ////Console.WriteLine($"Orientation Heading: {orien.X} Roll: {orien.Y} Pitch: {orien.Z}");
                        ////var line = bno055Sensor.LinearAcceleration;
                        ////Console.WriteLine($"Linear acceleration X: {line.X} Y: {line.Y} Z: {line.Z}");
                        ////var gravity = bno055Sensor.Gravity;
                        ////Console.WriteLine($"Gravity X: {gravity.X} Y: {gravity.Y} Z: {gravity.Z}");
                        ////var qua = bno055Sensor.Quaternion;
                        ////Console.WriteLine($"Quaternion X: {qua.X} Y: {qua.Y} Z: {qua.Z} W: {qua.W}");
                        ////var temp = bno055Sensor.Temperature.Celsius;
                        ////Console.WriteLine($"Temperature: {temp} °C");
                        ////Thread.Sleep(100);
                    }
                }
            }
        }
    }
}
