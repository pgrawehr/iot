// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.I2c;
using System.Device.Spi;
using System.IO.Ports;
using System.Threading;
using Iot.Device.Arduino;

namespace Ft4222.Samples
{
    /// <summary>
    /// Sample application for Ft4222
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args">Unused</param>
        public static void Main(string[] args)
        {
            string portName = "COM4";
            if (args.Length > 1)
            {
                portName = args[1];
            }

            using (var port = new SerialPort(portName, 57600))
            {
                Console.WriteLine($"Connecting to Arduino on {portName}");
                port.Open();
                ArduinoBoard board = new ArduinoBoard(port.BaseStream);
                Console.WriteLine($"Connection successful. Firmware version: {board.FirmwareVersion}, Builder: {board.FirmwareName}");
                while (Menu(board))
                {
                }

                board.Dispose();
            }
        }

        private static bool Menu(ArduinoBoard board)
        {
            Console.WriteLine("Hello I2C, SPI and GPIO FTFI! FT4222");
            Console.WriteLine("Select the test you want to run:");
            Console.WriteLine(" 1 Run I2C tests with a BNO055");
            Console.WriteLine(" 2 Run SPI tests with a simple HC595 with led blinking on all ports");
            Console.WriteLine(" 3 Run GPIO tests with a simple led blinking on GPIO6 port");
            Console.WriteLine(" 4 Run polling button test on GPIO2");
            Console.WriteLine(" 5 Run event wait test event on GPIO2 on Falling and Rising");
            Console.WriteLine(" 6 Run callback event test on GPIO2");
            Console.WriteLine(" X Exit");
            var key = Console.ReadKey();
            Console.WriteLine();

            ////if (key.KeyChar == '1')
            ////{
            ////    TestI2c();
            ////}

            ////if (key.KeyChar == '2')
            ////{
            ////    TestSpi();
            ////}

            if (key.KeyChar == '3')
            {
                TestGpio(board);
            }

            if (key.KeyChar == '4')
            {
                TestInput(board);
            }

            if (key.KeyChar == '5')
            {
                TestEventsDirectWait(board);
            }

            if (key.KeyChar == '6')
            {
                TestEventsCallback(board);
            }

            if (key.KeyChar == 'x' || key.KeyChar == 'X')
            {
                return false;
            }

            return true;
        }

        ////private static void TestI2c()
        ////{
        ////    var ftI2c = new Ft4222I2c(new I2cConnectionSettings(0, Bno055Sensor.DefaultI2cAddress));

        ////    var bno055Sensor = new Bno055Sensor(ftI2c);

        ////    Console.WriteLine($"Id: {bno055Sensor.Info.ChipId}, AccId: {bno055Sensor.Info.AcceleratorId}, GyroId: {bno055Sensor.Info.GyroscopeId}, MagId: {bno055Sensor.Info.MagnetometerId}");
        ////    Console.WriteLine($"Firmware version: {bno055Sensor.Info.FirmwareVersion}, Bootloader: {bno055Sensor.Info.BootloaderVersion}");
        ////    Console.WriteLine($"Temperature source: {bno055Sensor.TemperatureSource}, Operation mode: {bno055Sensor.OperationMode}, Units: {bno055Sensor.Units}");
        ////    Console.WriteLine($"Powermode: {bno055Sensor.PowerMode}");
        ////}

        ////private static void TestSpi()
        ////{
        ////    var ftSpi = new Ft4222Spi(new SpiConnectionSettings(0, 1) { ClockFrequency = 1_000_000, Mode = SpiMode.Mode0 });

        ////    while (!Console.KeyAvailable)
        ////    {
        ////        ftSpi.WriteByte(0xFF);
        ////        Thread.Sleep(500);
        ////        ftSpi.WriteByte(0x00);
        ////        Thread.Sleep(500);
        ////    }
        ////}

        public static void TestGpio(ArduinoBoard board)
        {
            // Use Pin 6
            const int gpio = 6;
            var gpioController = board.GetGpioController(PinNumberingScheme.Board);

            // Opening GPIO2
            gpioController.OpenPin(gpio);
            gpioController.SetPinMode(gpio, PinMode.Output);

            Console.WriteLine("Blinking GPIO6");
            while (!Console.KeyAvailable)
            {
                gpioController.Write(gpio, PinValue.High);
                Thread.Sleep(500);
                gpioController.Write(gpio, PinValue.Low);
                Thread.Sleep(500);
            }

            Console.ReadKey();
        }

        public static void TestInput(ArduinoBoard board)
        {
            const int gpio = 2;
            var gpioController = board.GetGpioController(PinNumberingScheme.Board);

            // Opening GPIO2
            gpioController.OpenPin(gpio);
            gpioController.SetPinMode(gpio, PinMode.Input);

            if (gpioController.GetPinMode(gpio) != PinMode.Input)
            {
                throw new InvalidOperationException("Couldn't set pin mode");
            }

            Console.WriteLine("Polling input pin 2");
            var lastState = gpioController.Read(gpio);
            while (!Console.KeyAvailable)
            {
                var newState = gpioController.Read(gpio);
                if (newState != lastState)
                {
                    if (newState == PinValue.High)
                    {
                        Console.WriteLine("Button pressed");
                    }
                    else
                    {
                        Console.WriteLine("Button released");
                    }
                }

                lastState = newState;
                Thread.Sleep(10);
            }

            Console.ReadKey();
        }

        public static void TestEventsDirectWait(ArduinoBoard board)
        {
            const int Gpio2 = 2;
            var gpioController = board.GetGpioController(PinNumberingScheme.Board);

            // Opening GPIO2
            gpioController.OpenPin(Gpio2);
            gpioController.SetPinMode(Gpio2, PinMode.Input);

            Console.WriteLine("Waiting for both falling and rising events");
            while (!Console.KeyAvailable)
            {
                var res = gpioController.WaitForEvent(Gpio2, PinEventTypes.Falling | PinEventTypes.Rising, new TimeSpan(0, 0, 0, 0, 50));
                if ((!res.TimedOut) && (res.EventTypes != PinEventTypes.None))
                {
                    Console.WriteLine($"Event on GPIO {Gpio2}, event type: {res.EventTypes}");
                }
            }

            Console.ReadKey();
            Console.WriteLine("Waiting for only rising events");
            while (!Console.KeyAvailable)
            {
                var res = gpioController.WaitForEvent(Gpio2, PinEventTypes.Rising, new TimeSpan(0, 0, 0, 0, 50));
                if ((!res.TimedOut) && (res.EventTypes != PinEventTypes.None))
                {
                    MyCallback(gpioController, new PinValueChangedEventArgs(res.EventTypes, Gpio2));
                }
            }
        }

        public static void TestEventsCallback(ArduinoBoard board)
        {
            const int Gpio2 = 2;
            var gpioController = board.GetGpioController(PinNumberingScheme.Board);

            // Opening GPIO2
            gpioController.OpenPin(Gpio2);
            gpioController.SetPinMode(Gpio2, PinMode.Input);

            Console.WriteLine("Setting up events on GPIO2 for rising and falling");

            gpioController.RegisterCallbackForPinValueChangedEvent(Gpio2, PinEventTypes.Falling | PinEventTypes.Rising, MyCallback);
            Console.WriteLine("Event setup, press a key to remove the falling event");
            while (!Console.KeyAvailable)
            {
                // Nothing to do
                Thread.Sleep(100);
            }

            Console.ReadKey();
            gpioController.UnregisterCallbackForPinValueChangedEvent(Gpio2, MyCallback);
            gpioController.RegisterCallbackForPinValueChangedEvent(Gpio2, PinEventTypes.Rising, MyCallback);
            Console.WriteLine("Now only waiting for rising events, press a key to remove all events and quit");
            while (!Console.KeyAvailable)
            {
                // Nothing to do
                Thread.Sleep(100);
            }

            gpioController.UnregisterCallbackForPinValueChangedEvent(Gpio2, MyCallback);
        }

        private static void MyCallback(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            Console.WriteLine($"Event on GPIO {pinValueChangedEventArgs.PinNumber}, event type: {pinValueChangedEventArgs.ChangeType}");
        }
    }
}
