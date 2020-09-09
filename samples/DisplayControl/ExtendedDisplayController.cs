using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Text;
using System.Threading;
using Iot.Device.Mcp23xxx;

namespace DisplayControl
{
    internal sealed class ExtendedDisplayController : IDisposable
    {
        internal enum PinUsage
        {
            Led1Green = 0,
            Led1Red = 3,
            Led2Green = 2,
            Led2Red = 1,
            Led3Green = 6,
            Led3Red = 5,
            Led4Green = 4,
            Led4Red = 7,
            Led5Green = 8,
            Led5Red = 9,
            KeyPadLeds = 10,
            DisplayBrightnessStep = 12,
            DisplayBrightnessDirection = 11,
            DisplayBrightnessChipSelect = 13,
            RedLed = 14,
            Buzzer = 15, // Pin 15 is connected to the alarm buzzer
        }


        private I2cDevice _device;
        private Mcp23017 _mcp23017;
        private GpioController _controllerUsingMcp;

        public void Init(GpioController gpioController)
        {
            _device = I2cDevice.Create(new I2cConnectionSettings(1, 0x20));
            _mcp23017 = new Mcp23017(_device, -1, -1, -1, gpioController);
            _controllerUsingMcp = new GpioController(PinNumberingScheme.Logical, _mcp23017);

            // Just open all the pins
            for (int i = 0; i < _controllerUsingMcp.PinCount; i++)
            {
                _controllerUsingMcp.OpenPin(i);
            }

            // Hardware implementation specific code (depends on actually attached hardware)
            _controllerUsingMcp.SetPinMode((int)PinUsage.Buzzer, PinMode.Output);
            _controllerUsingMcp.SetPinMode((int)PinUsage.RedLed, PinMode.Output);
            SoundAlarm(false);

            _controllerUsingMcp.SetPinMode((int)PinUsage.DisplayBrightnessChipSelect, PinMode.Output);
            _controllerUsingMcp.SetPinMode((int)PinUsage.DisplayBrightnessDirection, PinMode.Output);
            _controllerUsingMcp.SetPinMode((int)PinUsage.DisplayBrightnessStep, PinMode.Output);
            _controllerUsingMcp.SetPinMode((int)PinUsage.KeyPadLeds, PinMode.Output);
            _controllerUsingMcp.Write((int)PinUsage.KeyPadLeds, PinValue.Low);
            Write(PinUsage.DisplayBrightnessDirection, PinValue.Low);
            // CS is low active, so set it high
            Write(PinUsage.DisplayBrightnessChipSelect, PinValue.High);
            Write(PinUsage.DisplayBrightnessStep, PinValue.High);

            ClearLedDisplay();
        }

        public GpioController McpController
        {
            get
            {
                return _controllerUsingMcp;
            }
        }

        public void SoundAlarm(bool enable)
        {
            Write(PinUsage.Buzzer, enable ? PinValue.High : PinValue.Low);
            Write(PinUsage.RedLed, enable ? PinValue.High : PinValue.Low);
        }

        public void WriteLed(PinUsage pin, PinValue value)
        {
            Write(pin, value);
        }

        public void IncreaseBrightness(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                Write(PinUsage.DisplayBrightnessChipSelect, PinValue.Low);
                Write(PinUsage.DisplayBrightnessDirection, PinValue.Low);
                Write(PinUsage.DisplayBrightnessStep, PinValue.Low);
                Thread.SpinWait(100); // at least 1us
                Write(PinUsage.DisplayBrightnessStep, PinValue.High);
                Write(PinUsage.DisplayBrightnessChipSelect, PinValue.High);
            }
        }

        public void DecreaseBrightness(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                Write(PinUsage.DisplayBrightnessChipSelect, PinValue.Low);
                Write(PinUsage.DisplayBrightnessDirection, PinValue.High);
                Write(PinUsage.DisplayBrightnessStep, PinValue.Low);
                Thread.SpinWait(100); // at least 1us
                Write(PinUsage.DisplayBrightnessStep, PinValue.High);
                Write(PinUsage.DisplayBrightnessChipSelect, PinValue.High);
            }
        }

        public void ClearLedDisplay()
        {
            // Disable all color leds
            for (int i = 0; i < 10; i++)
            {
                _controllerUsingMcp.SetPinMode(i, PinMode.Output);
                _controllerUsingMcp.Write(i, PinValue.Low);
            }
        }

        public void SelfTest()
        {
            ClearLedDisplay();
            SoundAlarm(true);
            Thread.Sleep(200);
            SoundAlarm(false);
            for (int i = 0; i < 5; i++)
            {
                Write(Led(i, true), PinValue.High);
                Thread.Sleep(200);
                Write(Led(i, true), PinValue.Low);
                Thread.Sleep(200);
            }

            for (int i = 0; i < 5; i++)
            {
                Write(Led(i, false), PinValue.High);
                Thread.Sleep(200);
                Write(Led(i, false), PinValue.Low);
                Thread.Sleep(200);
            }
        }

        private PinUsage Led(int index, bool green)
        {
            if (green)
            {
                switch (index)
                {
                    case 0:
                        return PinUsage.Led1Green;
                    case 1:
                        return PinUsage.Led2Green;
                    case 2:
                        return PinUsage.Led3Green;
                    case 3:
                        return PinUsage.Led4Green;
                    case 4:
                        return PinUsage.Led5Green;
                    default:
                        throw new InvalidOperationException("No such LED");
                }
            }
            else
            {
                switch (index)
                {
                    case 0:
                        return PinUsage.Led1Red;
                    case 1:
                        return PinUsage.Led2Red;
                    case 2:
                        return PinUsage.Led3Red;
                    case 3:
                        return PinUsage.Led4Red;
                    case 4:
                        return PinUsage.Led5Red;
                    default:
                        throw new InvalidOperationException("No such LED");
                }
            }
        }

        private void Write(PinUsage pin, PinValue pinValue)
        {
            _controllerUsingMcp.Write((int)pin, pinValue);
        }

        public void Dispose()
        {
            if (_mcp23017 != null)
            {
                ClearLedDisplay(); // Switch them all off
                SoundAlarm(false);
                _controllerUsingMcp.Dispose();
                _device.Dispose();
            }

            _mcp23017 = null;
        }
    }
}
