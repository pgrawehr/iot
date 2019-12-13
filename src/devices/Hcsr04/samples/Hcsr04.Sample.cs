﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Spi;
using System.Threading;
using Iot.Device.Hcsr04;

namespace Iot.Device.Hcsr04.Samples
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
            Console.WriteLine("Hello Hcsr04 Sample!");

            using (var sonar = new Hcsr04(4, 17))
            {
                while (true)
                {
                    Console.WriteLine($"Distance: {sonar.Distance} cm");
                    System.Threading.Thread.Sleep(1000);
                }
            }
        }
    }
}
