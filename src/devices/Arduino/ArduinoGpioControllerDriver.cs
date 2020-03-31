using System;
using System.Device.Gpio;
using System.Threading;

#pragma warning disable CS1591
namespace Iot.Device.Arduino
{
    public class ArduinoGpioControllerDriver : GpioDriver
    {
        public ArduinoGpioControllerDriver(ArduinoBoard arduinoBoard)
        {
            throw new NotImplementedException();
        }

        protected override int PinCount { get; }
        protected override int ConvertPinNumberToLogicalNumberingScheme(int pinNumber)
        {
            throw new NotImplementedException();
        }

        protected override void OpenPin(int pinNumber)
        {
            throw new NotImplementedException();
        }

        protected override void ClosePin(int pinNumber)
        {
            throw new NotImplementedException();
        }

        protected override void SetPinMode(int pinNumber, PinMode mode)
        {
            throw new NotImplementedException();
        }

        protected override PinMode GetPinMode(int pinNumber)
        {
            throw new NotImplementedException();
        }

        protected override bool IsPinModeSupported(int pinNumber, PinMode mode)
        {
            throw new NotImplementedException();
        }

        protected override PinValue Read(int pinNumber)
        {
            throw new NotImplementedException();
        }

        protected override void Write(int pinNumber, PinValue value)
        {
            throw new NotImplementedException();
        }

        protected override WaitForEventResult WaitForEvent(int pinNumber, PinEventTypes eventTypes, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override void AddCallbackForPinValueChangedEvent(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback)
        {
            throw new NotImplementedException();
        }

        protected override void RemoveCallbackForPinValueChangedEvent(int pinNumber, PinChangeEventHandler callback)
        {
            throw new NotImplementedException();
        }
    }
}
