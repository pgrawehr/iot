// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.Gpio;
using System.Threading;
using Iot.Device.Board;

namespace BoardSample
{
    /// <summary>
    /// Test program main class
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Example program for Windows board class (execute on desktop)
        /// </summary>
        /// <param name="args">Command line arguments</param>
        public static void Main(string[] args)
        {
            const int led0 = 0;
            const int led1 = 1;
            const int led2 = 2;
            using BoardBase b = new WindowsBoard(PinNumberingScheme.Logical);

            using GpioController controller = b.CreateGpioController(PinNumberingScheme.Logical);

            if (controller.PinCount > 0)
            {
                Console.WriteLine("Blinking keyboard test. Press ESC to quit");
                controller.OpenPin(led0);
                controller.OpenPin(led1);
                controller.OpenPin(led2);
                controller.SetPinMode(led0, PinMode.Output);
                controller.SetPinMode(led1, PinMode.Output);
                controller.SetPinMode(led2, PinMode.Output);
                PinValue state = PinValue.Low;

                ConsoleKey key = ConsoleKey.NoName;
                while (key != ConsoleKey.Escape)
                {
                    state = !state;
                    controller.Write(led0, state);
                    controller.Write(led1, state);
                    controller.Write(led2, state);
                    Thread.Sleep(500);
                    if (Console.KeyAvailable)
                    {
                        key = Console.ReadKey().Key;
                    }
                }
            }
        }
    }
}
