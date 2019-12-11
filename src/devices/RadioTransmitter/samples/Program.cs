// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.I2c;
using Iot.Device.RadioTransmitter;

namespace RadioTransmitter
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
            I2cConnectionSettings settings = new I2cConnectionSettings(1, Kt0803.DefaultI2cAddress);
            I2cDevice device = I2cDevice.Create(settings);

            using (Kt0803 radio = new Kt0803(device, 106.6, Region.China))
            {
                Console.WriteLine($"The radio is running on FM {radio.Frequency.ToString("0.0")}MHz");

                Console.ReadKey();
            }
        }
    }
}
