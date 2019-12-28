// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.Gpio;
using System.Diagnostics;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;

namespace DisplayControl
{
    internal sealed class Program : IDisposable
    {
        const int LedPin = 17;

        internal Program(GpioController controller)
        {
            Controller = controller;
            MainAppBuilder = AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
        }

        public GpioController Controller { get; }

        public AppBuilder MainAppBuilder { get; }

        public static void Main(string[] args)
        {
            Console.WriteLine($"Initializing Hardware...");
            using (GpioController controller = new GpioController())
            {
                controller.OpenPin(LedPin, PinMode.Output);
                Program prog = new Program(controller);
                try
                {
                    Trace.Listeners.Add(new ConsoleTraceListener());
                    controller.Write(LedPin, PinValue.High);
                    prog.Initialize();
                    prog.Run(args);
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

        public void Run(string[] args)
        {
            MainAppBuilder.StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnMainWindowClose);
        }

        public void Dispose()
        {
            Controller.Dispose();
        }
    }
}
