// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.I2c;
using System.Device.Spi;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using Iot.Device.Adc;
using Iot.Device.Arduino;
using Iot.Device.Arduino.Sample;
using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.PowerMode;
using Iot.Device.Common;
using Iot.Device.CpuTemperature;
using UnitsNet;

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
            Console.WriteLine(" 8 Read analog channel as fast as possible");
            Console.WriteLine(" 9 Run SPI tests with an MCP3008 (experimental)");
            Console.WriteLine(" 0 Detect all devices on the I2C bus");
            Console.WriteLine(" H Read DHT11 Humidity sensor on GPIO 3 (experimental)");
            Console.WriteLine(" D Use a display");
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
                case '8':
                    TestAnalogCallback(board);
                    break;
                case '9':
                    TestSpi(board);
                    break;
                case '0':
                    ScanDeviceAddressesOnI2cBus(board);
                    break;
                case 'h':
                case 'H':
                    TestDht(board);
                    break;
                case 'x':
                case 'X':
                    return false;
                case 'd':
                case 'D':
                    TestDisplay(board);
                    break;
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
            bmp.StandbyTime = StandbyTime.Ms250;
            bmp.SetPowerMode(Bmx280PowerMode.Normal);
            Console.WriteLine("Device open");
            while (!Console.KeyAvailable)
            {
                bmp.TryReadTemperature(out var temperature);
                bmp.TryReadPressure(out var pressure);
                Console.Write($"\rTemperature: {temperature.DegreesCelsius:F2}°C. Pressure {pressure.Hectopascals:F1} hPa                  ");
                Thread.Sleep(100);
            }

            bmp.Dispose();
            device.Dispose();
            Console.ReadKey();
            Console.WriteLine();
        }

        private static void ScanDeviceAddressesOnI2cBus(ArduinoBoard board)
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("     0  1  2  3  4  5  6  7  8  9  a  b  c  d  e  f");
            stringBuilder.Append(Environment.NewLine);

            for (int startingRowAddress = 0; startingRowAddress < 128; startingRowAddress += 16)
            {
                stringBuilder.Append($"{startingRowAddress:x2}: ");  // Beginning of row.

                for (int rowAddress = 0; rowAddress < 16; rowAddress++)
                {
                    int deviceAddress = startingRowAddress + rowAddress;

                    // Skip the unwanted addresses.
                    if (deviceAddress < 0x3 || deviceAddress > 0x77)
                    {
                        stringBuilder.Append("   ");
                        continue;
                    }

                    var connectionSettings = new I2cConnectionSettings(0, deviceAddress);
                    using (var i2cDevice = board.CreateI2cDevice(connectionSettings))
                    {
                        try
                        {
                            i2cDevice.ReadByte();  // Only checking if device is present.
                            stringBuilder.Append($"{deviceAddress:x2} ");
                        }
                        catch
                        {
                            stringBuilder.Append("-- ");
                        }
                    }
                }

                stringBuilder.Append(Environment.NewLine);
            }

            Console.WriteLine(stringBuilder.ToString());
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

        public static void TestAnalogCallback(ArduinoBoard board)
        {
            const int analogPin = 15;
            var analogController = board.CreateAnalogController(0);

            analogController.OpenPin(analogPin);
            analogController.EnableAnalogValueChangedEvent(analogPin, null, 0);

            analogController.ValueChanged += (sender, args) =>
            {
                if (args.PinNumber == analogPin)
                {
                    Console.WriteLine($"New voltage: {args.Value}.");
                }
            };

            Console.WriteLine("Waiting for changes on the analog input");
            while (!Console.KeyAvailable)
            {
                // Nothing to do
                Thread.Sleep(100);
            }

            Console.ReadKey();
            analogController.DisableAnalogValueChangedEvent(analogPin);
            analogController.Dispose();
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

            Console.ReadKey();
            gpioController.UnregisterCallbackForPinValueChangedEvent(Gpio2, MyCallback);
            gpioController.Dispose();
        }

        private static void MyCallback(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
        {
            Console.WriteLine($"Event on GPIO {pinValueChangedEventArgs.PinNumber}, event type: {pinValueChangedEventArgs.ChangeType}");
        }

        public static void TestSpi(ArduinoBoard board)
        {
            const double vssValue = 5; // Set this to the supply voltage of the arduino. Most boards have 5V, some newer ones run at 3.3V.
            SpiConnectionSettings settings = new SpiConnectionSettings(0, 10);
            using (var spi = board.CreateSpiDevice(settings))
            using (Mcp3008 mcp = new Mcp3008(spi))
            {
                Console.WriteLine("SPI Device open");
                while (!Console.KeyAvailable)
                {
                    double vdd = mcp.Read(5);
                    double vss = mcp.Read(6);
                    double middle = mcp.Read(7);
                    Console.WriteLine($"Raw values: VSS {vss} VDD {vdd} Average {middle}");
                    vdd = vssValue * vdd / 1024;
                    vss = vssValue * vss / 1024;
                    middle = vssValue * middle / 1024;
                    Console.WriteLine($"Converted values: VSS {vss:F2}V, VDD {vdd:F2}V, Average {middle:F2}V");
                    Thread.Sleep(200);
                }
            }

            Console.ReadKey();
        }

        public static void TestDht(ArduinoBoard board)
        {
            Console.WriteLine("Reading DHT11. Any key to quit.");

            while (!Console.KeyAvailable)
            {
                if (board.TryReadDht(3, 11, out var temperature, out var humidity))
                {
                    Console.WriteLine($"Temperature: {temperature}, Humidity {humidity}");
                }
                else
                {
                    Console.WriteLine("Unable to read DHT11");
                }

                Thread.Sleep(2000);
            }

            Console.ReadKey();
        }

        public static void TestDisplay(ArduinoBoard board)
        {
            const int Gpio2 = 2;
            const int MaxMode = 10;
            const double StationAltitude = 650;
            int mode = 0;
            var gpioController = board.CreateGpioController(PinNumberingScheme.Board);
            gpioController.OpenPin(Gpio2);
            gpioController.SetPinMode(Gpio2, PinMode.Input);
            CharacterDisplay disp = new CharacterDisplay(board);
            Console.WriteLine("Display output test");
            Console.WriteLine("The button on GPIO 2 changes modes");
            Console.WriteLine("Press x to exit");
            disp.Output.ScrollUpDelay = TimeSpan.FromMilliseconds(500);
            AutoResetEvent buttonClicked = new AutoResetEvent(false);

            void ChangeMode(object sender, PinValueChangedEventArgs pinValueChangedEventArgs)
            {
                mode++;
                if (mode > MaxMode)
                {
                    // Don't change back to 0
                    mode = 1;
                }

                buttonClicked.Set();
            }

            gpioController.RegisterCallbackForPinValueChangedEvent(Gpio2, PinEventTypes.Falling, ChangeMode);
            var device = board.CreateI2cDevice(new I2cConnectionSettings(0, Bmp280.DefaultI2cAddress));
            Bmp280 bmp;
            try
            {
                bmp = new Bmp280(device);
                bmp.StandbyTime = StandbyTime.Ms250;
                bmp.SetPowerMode(Bmx280PowerMode.Normal);
            }
            catch (IOException)
            {
                bmp = null;
                Console.WriteLine("BMP280 not available");
            }

            OpenHardwareMonitor hardwareMonitor = new OpenHardwareMonitor();
            hardwareMonitor.EnableDerivedSensors();
            TimeSpan sleeptime = TimeSpan.FromMilliseconds(500);
            string modeName = string.Empty;
            string previousModeName = string.Empty;
            int firstCharInText = 0;
            while (true)
            {
                if (Console.KeyAvailable && Console.ReadKey(true).KeyChar == 'x')
                {
                    break;
                }

                // Default
                sleeptime = TimeSpan.FromMilliseconds(500);

                switch (mode)
                {
                    case 0:
                        modeName = "Display ready";
                        disp.Output.ReplaceLine(1, "Button for mode");
                        // Just text
                        break;
                    case 1:
                    {
                        modeName = "Time";
                        disp.Output.ReplaceLine(1, DateTime.Now.ToLongTimeString());
                        sleeptime = TimeSpan.FromMilliseconds(200);
                        break;
                    }

                    case 2:
                    {
                        modeName = "Date";
                        disp.Output.ReplaceLine(1, DateTime.Now.ToShortDateString());
                        break;
                    }

                    case 3:
                        modeName = "Temperature / Barometric Pressure";
                        if (bmp != null && bmp.TryReadTemperature(out Temperature temp) && bmp.TryReadPressure(out Pressure p2))
                        {
                            Pressure p3 = WeatherHelper.CalculateBarometricPressure(p2, temp, StationAltitude);
                            disp.Output.ReplaceLine(1, string.Format(CultureInfo.CurrentCulture, "{0:s1} {1:s1}", temp, p3));
                        }
                        else
                        {
                            disp.Output.ReplaceLine(1, "N/A");
                        }

                        break;
                    case 4:
                        modeName = "Temperature / Humidity";
                        if (board.TryReadDht(3, 11, out temp, out var humidity))
                        {
                            disp.Output.ReplaceLine(1, string.Format(CultureInfo.CurrentCulture, "{0:s1} {1:s0}", temp, humidity));
                        }
                        else
                        {
                            disp.Output.ReplaceLine(1, "N/A");
                        }

                        break;

                    case 5:
                        modeName = "Dew point";
                        if (bmp != null && bmp.TryReadPressure(out p2) && board.TryReadDht(3, 11, out temp, out humidity))
                        {
                            Temperature dewPoint = WeatherHelper.CalculateDewPoint(temp, humidity.Percent);
                            disp.Output.ReplaceLine(1, dewPoint.ToString("s1", CultureInfo.CurrentCulture));
                        }
                        else
                        {
                            disp.Output.ReplaceLine(1, "N/A");
                        }

                        break;
                    case 6:
                        modeName = "CPU Temperature";
                        if (hardwareMonitor.TryGetAverageCpuTemperature(out temp))
                        {
                            disp.Output.ReplaceLine(1, temp.ToString("s1", CultureInfo.CurrentCulture));
                        }
                        else
                        {
                            disp.Output.ReplaceLine(1, "N/A");
                        }

                        break;
                    case 7:
                        modeName = "GPU Temperature";
                        if (hardwareMonitor.TryGetAverageGpuTemperature(out temp))
                        {
                            disp.Output.ReplaceLine(1, temp.ToString("s1", CultureInfo.CurrentCulture));
                        }
                        else
                        {
                            disp.Output.ReplaceLine(1, "N/A");
                        }

                        break;
                    case 8:
                        modeName = "CPU Load";
                        disp.Output.ReplaceLine(1, hardwareMonitor.GetCpuLoad().ToString("s1", CultureInfo.CurrentCulture));
                        break;

                    case 9:
                        modeName = "Total power dissipation";
                        var powerSources = hardwareMonitor.GetSensorList().Where(x => x.SensorType == SensorType.Power);
                        Power totalPower = Power.Zero;
                        foreach (var power in powerSources)
                        {
                            if (power.Name != "CPU Cores" && power.TryGetValue(out Power powerConsumption)) // included in CPU Package
                            {
                                totalPower = totalPower + powerConsumption;
                            }
                        }

                        disp.Output.ReplaceLine(1, totalPower.ToString("s1", CultureInfo.CurrentCulture));
                        break;

                    case 10:
                        modeName = "Energy consumed";
                        var energySources = hardwareMonitor.GetSensorList().Where(x => x.SensorType == SensorType.Energy);
                        Energy totalEnergy = Energy.FromWattHours(0); // Set up the desired output unit
                        foreach (var e in energySources)
                        {
                            if (!e.Name.StartsWith("CPU Cores") && e.TryGetValue(out Energy powerConsumption)) // included in CPU Package
                            {
                                totalEnergy = totalEnergy + powerConsumption;
                            }
                        }

                        disp.Output.ReplaceLine(1, totalEnergy.ToString("s1", CultureInfo.CurrentCulture));
                        break;
                }

                int displayWidth = disp.Output.Size.Width;
                if (modeName.Length > displayWidth)
                {
                    // Add one space at the end, makes it a bit easier to read
                    if (firstCharInText < modeName.Length - displayWidth + 1)
                    {
                        firstCharInText++;
                    }
                    else
                    {
                        firstCharInText = 0;
                    }

                    disp.Output.ReplaceLine(0, modeName.Substring(firstCharInText));
                }

                if (modeName != previousModeName)
                {
                    disp.Output.ReplaceLine(0, modeName);

                    previousModeName = modeName;
                    firstCharInText = 0;
                }

                buttonClicked.WaitOne(sleeptime);
            }

            hardwareMonitor.Dispose();
            disp.Output.Clear();
            disp.Dispose();
            bmp?.Dispose();
            gpioController.Dispose();
        }
    }
}
