using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Text;
using System.Threading;

namespace Iot.Device.Board
{
    public class ManagedRaspberryPi3Driver : RaspberryPi3Driver, IGpioDriver
    {
        private readonly Board _board;
        private IGpioDriver _gpioDriverImplementation;

        public ManagedRaspberryPi3Driver(Board board, IGpioDriver actualDriver)
        {
            _board = board;
            _gpioDriverImplementation = actualDriver;
        }

        int IGpioDriver.PinCount => PinCount;

        protected override int PinCount
        {
            get
            {
                return _gpioDriverImplementation.PinCount;
            }
        }

        protected override int ConvertPinNumberToLogicalNumberingScheme(int pinNumber)
        {
            return _board.ConvertPinNumberToLogicalNumberingScheme(pinNumber);
        }

        void IGpioDriver.OpenPin(int pinNumber)
        {
            OpenPin(pinNumber);
        }

        protected override void OpenPin(int pinNumber)
        {
            _gpioDriverImplementation.OpenPin(pinNumber);
        }

        void IGpioDriver.ClosePin(int pinNumber)
        {
            ClosePin(pinNumber);
        }

        protected override void ClosePin(int pinNumber)
        {
            _gpioDriverImplementation.ClosePin(pinNumber);
        }

        protected override void SetPinMode(int pinNumber, PinMode mode)
        {
            _gpioDriverImplementation.SetPinMode(pinNumber, mode);
        }

        protected override PinMode GetPinMode(int pinNumber)
        {
            return _gpioDriverImplementation.GetPinMode(pinNumber);
        }

        protected override bool IsPinModeSupported(int pinNumber, PinMode mode)
        {
            return _gpioDriverImplementation.IsPinModeSupported(pinNumber, mode);
        }

        protected override PinValue Read(int pinNumber)
        {
            return _gpioDriverImplementation.Read(pinNumber);
        }

        protected override void Write(int pinNumber, PinValue value)
        {
            _gpioDriverImplementation.Write(pinNumber, value);
        }

        protected override WaitForEventResult WaitForEvent(int pinNumber, PinEventTypes eventTypes, CancellationToken cancellationToken)
        {
            return _gpioDriverImplementation.WaitForEvent(pinNumber, eventTypes, cancellationToken);
        }

        protected override void AddCallbackForPinValueChangedEvent(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback)
        {
            _gpioDriverImplementation.AddCallbackForPinValueChangedEvent(pinNumber, eventTypes, callback);
        }

        protected override void RemoveCallbackForPinValueChangedEvent(int pinNumber, PinChangeEventHandler callback)
        {
            _gpioDriverImplementation.RemoveCallbackForPinValueChangedEvent(pinNumber, callback);
        }

        void IGpioDriver.SetPinMode(int pinNumber, PinMode mode)
        {
            SetPinMode(pinNumber, mode);
        }

        PinMode IGpioDriver.GetPinMode(int pinNumber)
        {
            return GetPinMode(pinNumber);
        }

        bool IGpioDriver.IsPinModeSupported(int pinNumber, PinMode mode)
        {
            return IsPinModeSupported(pinNumber, mode);
        }

        PinValue IGpioDriver.Read(int pinNumber)
        {
            return Read(pinNumber);
        }

        void IGpioDriver.Write(int pinNumber, PinValue value)
        {
            Write(pinNumber, value);
        }

        WaitForEventResult IGpioDriver.WaitForEvent(int pinNumber, PinEventTypes eventTypes, CancellationToken cancellationToken)
        {
            return WaitForEvent(pinNumber, eventTypes, cancellationToken);
        }

        void IGpioDriver.AddCallbackForPinValueChangedEvent(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback)
        {
            AddCallbackForPinValueChangedEvent(pinNumber, eventTypes, callback);
        }

        void IGpioDriver.RemoveCallbackForPinValueChangedEvent(int pinNumber, PinChangeEventHandler callback)
        {
            RemoveCallbackForPinValueChangedEvent(pinNumber, callback);
        }
    }
}
