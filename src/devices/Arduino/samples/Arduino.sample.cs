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
using Iot.Device.Bmxx80;

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

            using (var port = new SerialPort(portName, 115200))
            {
                Console.WriteLine($"Connecting to Arduino on {portName}");
                try
                {
                    port.Open();
                }
                catch (UnauthorizedAccessException x)
                {
                    Console.WriteLine($"Could not open COM port: {x.Message} Possible reason: Arduino IDE connected or serial console open");
                    return;
                }

                ArduinoBoard board = new ArduinoBoard(port.BaseStream);
                try
                {
                    board.LogMessages += BoardOnLogMessages;
                    board.Initialize();
                    Console.WriteLine($"Connection successful. Firmware version: {board.FirmwareVersion}, Builder: {board.FirmwareName}");
                    while (Menu(board))
                    {
                    }
                }
                catch (TimeoutException x)
                {
                    Console.WriteLine($"No answer from board: {x.Message} ");
                }
                finally
                {
                    port.Close();
                    board?.Dispose();
                }
            }
        }

        private static void BoardOnLogMessages(string message, Exception exception)
        {
            Console.WriteLine("Log message: " + message);
            if (exception != null)
            {
                Console.WriteLine(exception);
            }
        }

        private static bool Menu(ArduinoBoard board)
        {
            Console.WriteLine("Hello I2C and GPIO on Arduino!");
            Console.WriteLine("Select the test you want to run:");
            Console.WriteLine(" 1 Run I2C tests with a BMP280");
            Console.WriteLine(" 2 Run GPIO tests with a simple led blinking on GPIO6 port");
            Console.WriteLine(" 3 Run polling button test on GPIO2");
            Console.WriteLine(" 4 Run event wait test event on GPIO2 on Falling and Rising");
            Console.WriteLine(" 5 Run callback event test on GPIO2");
            Console.WriteLine(" 6 Run PWM test with a simple led dimming on GPIO6 port");
            Console.WriteLine(" 7 Dim the LED according to the input on A1");
            Console.WriteLine(" X Exit");
            var key = Console.ReadKey();
            Console.WriteLine();

            switch (key.KeyChar)
            {
                case '1':
                    TestI2c(board);
                    break;
                case '2':
                    TestGpio(board);
                    break;
                case '3':
                    TestInput(board);
                    break;
                case '4':
                    TestEventsDirectWait(board);
                    break;
                case '5':
                    TestEventsCallback(board);
                    break;
                case '6':
                    TestPwm(board);
                    break;
                case '7':
                    TestAnalogIn(board);
                    break;
                case 'x':
                case 'X':
                    return false;
            }

            return true;
        }

        private static void TestPwm(ArduinoBoard board)
        {
            int pin = 6;
            using (var pwm = board.CreatePwmChannel(0, pin, 100, 0))
            {
                Console.WriteLine("Now dimming LED. Press any key to exit");
                while (!Console.KeyAvailable)
                {
                    pwm.Start();
                    for (double fadeValue = 0; fadeValue <= 1.0; fadeValue += 0.05)
                    {
                        // sets the value (range from 0 to 255):
                        pwm.DutyCycle = fadeValue;
                        // wait for 30 milliseconds to see the dimming effect
                        Thread.Sleep(30);
                    }

                    // fade out from max to min in increments of 5 points:
                    for (double fadeValue = 1.0; fadeValue >= 0; fadeValue -= 0.05)
                    {
                        // sets the value (range from 0 to 255):
                        pwm.DutyCycle = fadeValue;
                        // wait for 30 milliseconds to see the dimming effect
                        Thread.Sleep(30);
                    }

                }

                Console.ReadKey();
                pwm.Stop();
            }
        }

        private static void TestI2c(ArduinoBoard board)
        {
            var device = board.CreateI2cDevice(new I2cConnectionSettings(0, Bmp280.DefaultI2cAddress));

            var bmp = new Bmp280(device);
            Console.WriteLine("Device open");
            while (!Console.KeyAvailable)
            {
                bmp.TryReadTemperature(out var temperature);
                bmp.TryReadPressure(out var pressure);
                Console.Write($"\rTemperature: {temperature.Celsius:F2}°C. Pressure {pressure.Hectopascal:F1} hPa                  ");
                Thread.Sleep(100);
            }

            bmp.Dispose();
            device.Dispose();
            Console.ReadKey();
            Console.WriteLine();
        }

        public static void TestGpio(ArduinoBoard board)
        {
            // Use Pin 6
            const int gpio = 6;
            var gpioController = board.CreateGpioController(PinNumberingScheme.Board);

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
            gpioController.Dispose();
        }

        public static void TestAnalogIn(ArduinoBoard board)
        {
            // Use Pin 6
            const int gpio = 6;
            const int analogPin = 15;
            var gpioController = board.CreateGpioController(PinNumberingScheme.Board);
            var analogController = board.CreateAnalogController(0);

            analogController.OpenPin(analogPin);
            gpioController.OpenPin(gpio);
            gpioController.SetPinMode(gpio, PinMode.Output);

            Console.WriteLine("Blinking GPIO6, based on analog input.");
            while (!Console.KeyAvailable)
            {
                double voltage = analogController.ReadVoltage(analogPin);
                gpioController.Write(gpio, PinValue.High);
                Thread.Sleep((int)voltage * 100);
                voltage = analogController.ReadVoltage(analogPin);
                gpioController.Write(gpio, PinValue.Low);
                Thread.Sleep((int)voltage * 100);
            }

            Console.ReadKey();
            analogController.Dispose();
            gpioController.Dispose();
        }

        public static void TestInput(ArduinoBoard board)
        {
            const int gpio = 2;
            var gpioController = board.CreateGpioController(PinNumberingScheme.Board);

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
            gpioController.Dispose();
        }

        public static void TestEventsDirectWait(ArduinoBoard board)
        {
            const int Gpio2 = 2;
            var gpioController = board.CreateGpioController(PinNumberingScheme.Board);

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

            gpioController.Dispose();
        }

        public static void TestEventsCallback(ArduinoBoard board)
        {
            const int Gpio2 = 2;
            var gpioController = board.CreateGpioController(PinNumberingScheme.Board);

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
            gpioController.Dispose();
        }

        private static void MyCallback(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            Console.WriteLine($"Event on GPIO {pinValueChangedEventArgs.PinNumber}, event type: {pinValueChangedEventArgs.ChangeType}");
        }
    }
}
