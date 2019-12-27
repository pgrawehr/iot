// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.Gpio;
using System.Threading;

namespace DisplayControl
{
    internal sealed class Program : IDisposable
    {
        const int LedPin = 17;

        internal Program(GpioController controller)
        {
            Controller = controller;
        }

        public GpioController Controller { get; }

        public static void Main(string[] args)
        {
            Console.WriteLine($"Initializing Hardware...");
            using (GpioController controller = new GpioController())
            {
                controller.OpenPin(LedPin, PinMode.Output);
                Program prog = new Program(controller);
                try
                {
                    controller.Write(LedPin, PinValue.High);
                    prog.Initialize();
                    prog.Run();
                }
                finally
                {
                    controller.Write(LedPin, PinValue.Low);
                    prog.Dispose();
                }
            }
        }

        public void Initialize()
        {

        }

        public void Dispose()
        {
            Controller.Dispose();
        }
    }
}
