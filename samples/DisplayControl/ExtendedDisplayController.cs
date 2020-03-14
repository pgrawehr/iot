using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Text;
using Iot.Device.Mcp23xxx;

namespace DisplayControl
{
    internal sealed class ExtendedDisplayController : IDisposable
    {
        public enum PinUsage
        {
            RedLed = 8,
            Buzzer = 15, // Pin 15 is connected to the alarm buzzer
        }
        private I2cDevice _device;
        private Mcp23017 _mcp23017;
        private GpioController _controllerUsingMcp;

        public ExtendedDisplayController(GpioController gpioController)
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
        }

        public void SoundAlarm(bool enable)
        {
            Write(PinUsage.Buzzer, enable ? PinValue.High : PinValue.Low);
            Write(PinUsage.RedLed, enable ? PinValue.High : PinValue.Low);
        }

        public void Write(PinUsage pin, PinValue pinValue)
        {
            _controllerUsingMcp.Write((int)pin, pinValue);
        }

        public void Dispose()
        {
            if (_mcp23017 != null)
            {
                _controllerUsingMcp.Dispose();
                _device.Dispose();
            }

            _mcp23017 = null;
        }
    }
}
