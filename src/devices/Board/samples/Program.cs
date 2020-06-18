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
            var os = Environment.OSVersion;
            if (os.Platform == PlatformID.Win32NT)
            {
                // TODO: Add wrapper for CPU type property (see https://stackoverflow.com/questions/6944779/determine-operating-system-and-processor-type-in-c-sharp)
                // We will eventually need to run different code based on whether this is an X86/X64 CPU or an ARM
                WindowsDesktop();
            }
            else if (os.Platform == PlatformID.Unix)
            {
                RaspberryPiTest();
            }
        }

        private static void WindowsDesktop()
        {
            const int led0 = 0;
            const int led1 = 1;
            const int led2 = 2;
            using Board b = new GenericBoard(PinNumberingScheme.Logical);

            using GpioController controller = b.CreateGpioController(null, PinNumberingScheme.Logical);

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
                        key = Console.ReadKey(true).Key;
                    }
                }

                while (Console.KeyAvailable)
                {
                    Console.ReadKey(true);
                }

                key = ConsoleKey.NoName;
                Console.WriteLine($"Input test: This polls the 'A' key, ESC to quit");
                int pinNumber = (int)ConsoleKey.A;
                controller.OpenPin(pinNumber);
                controller.SetPinMode(pinNumber, PinMode.Input);
                // Note that the Console access is independent of what we actually demonstrate here: The use of the keyboard as "input pins"
                while (key != ConsoleKey.Escape)
                {
                    if (controller.Read(pinNumber) == PinValue.High)
                    {
                        Console.WriteLine("Key is pressed");
                    }
                    else
                    {
                        Console.WriteLine("Key is not pressed");
                    }

                    Thread.Sleep(100);
                    if (Console.KeyAvailable)
                    {
                        key = Console.ReadKey(true).Key;
                    }
                }

                key = ConsoleKey.NoName;
                Console.WriteLine($"Interrupt test: This listens on the 'A' key, ESC to quit");
                controller.SetPinMode(pinNumber, PinMode.Input);
                controller.RegisterCallbackForPinValueChangedEvent(pinNumber, PinEventTypes.Falling | PinEventTypes.Rising, Callback);
                // Note that the Console access is independent of what we actually demonstrate here: The use of the keyboard as "input pins"
                while (key != ConsoleKey.Escape)
                {
                    // Now we can do a blocking read here
                    key = Console.ReadKey(true).Key;
                }

                controller.UnregisterCallbackForPinValueChangedEvent(pinNumber, Callback);

                controller.ClosePin(pinNumber);
            }
        }

        private static void Callback(object sender, PinValueChangedEventArgs e)
        {
            if (e.ChangeType == PinEventTypes.Rising)
            {
                Console.WriteLine("Key pressed");
            }
            else
            {
                Console.WriteLine("Key released");
            }
        }

        private static void RaspberryPiTest()
        {
        }
    }
}
