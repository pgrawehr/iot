﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;

namespace DisplayControl
{
    internal sealed class Program : IDisposable
    {
        internal Program(GpioController controller)
        {
            Controller = controller;
        }

        public GpioController Controller { get; }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace()
                .UseReactiveUI();
        }

        public static void Main(string[] args)
        {
            if (args.Any(x => x == "--debug"))
            {
                Console.WriteLine("Press any key to start or attach debugger now");
                while (!Console.KeyAvailable)
                {
                    if (Debugger.IsAttached)
                    {
                        break;
                    }
                    Thread.Sleep(10);
                }
            }

            Console.WriteLine($"Initializing Hardware...");
            using (GpioController controller = new GpioController(PinNumberingScheme.Logical, new RaspberryPi3Driver()))
            {
                Program prog = new Program(controller);
                try
                {
                    Trace.Listeners.Add(new ConsoleTraceListener());
                    prog.Run(args);
                }
                finally
                {
                    prog.Dispose();
                }
            }
        }

        public void Run(string[] args)
        {
            var builder = BuildAvaloniaApp();
            App.SetGpioController(Controller);
            builder.StartWithClassicDesktopLifetime(args, Avalonia.Controls.ShutdownMode.OnMainWindowClose);
        }

        public void Dispose()
        {
            Controller.Dispose();
        }
    }
}