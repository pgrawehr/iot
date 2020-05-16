// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using CpuTemperature;
using Iot.Device.CpuTemperature;
using Iot.Units;

namespace Iot.Device.CpuTemperature.Samples
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            CpuTemperature cpuTemperature = new CpuTemperature();
            Console.WriteLine("Press any key to quit");

            while (!Console.KeyAvailable)
            {
                if (cpuTemperature.IsAvailable)
                {
                    var temperature = cpuTemperature.ReadTemperatures();
                    foreach (var entry in temperature)
                    {
                        if (!double.IsNaN(entry.Item2.Celsius))
                        {
                            Console.WriteLine($"Temperature from {entry.Item1.ToString()}: {entry.Item2.Celsius} °C");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"CPU temperature is not available");
                }

                Thread.Sleep(1000);
            }

            Console.ReadKey(true);
            OpenHardwareMonitor hw = new OpenHardwareMonitor();
            if (!hw.IsAvailable)
            {
                Console.WriteLine("OpenHardwareMonitor is not running");
                return;
            }

            while (!Console.KeyAvailable)
            {
                Console.Clear();
                Console.WriteLine("Showing all available sensors (press any key to quit)");
                var components = hw.GetHardwareComponents();
                foreach (var component in components)
                {
                    Console.WriteLine("--------------------------------------------------------------------");
                    Console.WriteLine($"{component.Name} Type {component.Type}, Path {component.Identifier}");
                    Console.WriteLine("--------------------------------------------------------------------");
                    foreach (var sensor in hw.GetSensorList(component))
                    {
                        Console.Write($"{sensor.Name}: Path {sensor.Identifier}, Parent {sensor.Parent} ");
                        // TODO: Extend this tree once 1072 is merged
                        if (sensor.TryGetValue(out Temperature temp))
                        {
                            Console.WriteLine($"Temperature: {temp.Celsius:F1} °C");
                        }
                        else if (sensor.TryGetValue(out float dbl))
                        {
                            Console.WriteLine($"Value {dbl:F2}");
                        }
                        else
                        {
                            Console.WriteLine($"No data");
                        }
                    }
                }

                Thread.Sleep(1000);
            }
        }
    }
}
