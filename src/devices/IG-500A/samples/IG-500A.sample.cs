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
                using (Ig500Sensor sensor = new Ig500Sensor(port.BaseStream,
                    OutputDataSets.Euler | OutputDataSets.Magnetometers | OutputDataSets.Quaternion |
                    OutputDataSets.Temperatures |
                    OutputDataSets.Accelerometers | OutputDataSets.Gyroscopes))
                {
                    if (!sensor.WaitForSensorReady(out var errorMessage))
                    {
                        Console.WriteLine($"Error initializing device: {errorMessage}");
                        return;
                    }

                    while (!Console.KeyAvailable)
                    {
                        var orien = sensor.Orientation;
                        var magneto = sensor.Magnetometer;
                        var gyro = sensor.Gyroscope;
                        var accele = sensor.Accelerometer;

                        Console.Clear();

                        Console.WriteLine($"Orientation Heading: {orien.X:F2} Roll: {orien.Y:F2} Pitch: {orien.Z:F2}");
                        Console.WriteLine($"Magnetometer X: {magneto.X} Y: {magneto.Y} Z: {magneto.Z}");
                        Console.WriteLine($"Gyroscope X: {gyro.X} Y: {gyro.Y} Z: {gyro.Z}");
                        Console.WriteLine($"Acceleration X: {accele.X} Y: {accele.Y} Z: {accele.Z}");
                        Console.WriteLine($"Temperature {sensor.Temperature.Celsius}°C");
                        Thread.Sleep(100);
                    }
                }
            }
        }
    }
}
