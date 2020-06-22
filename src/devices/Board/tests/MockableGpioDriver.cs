﻿using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Text;
using System.Threading;

namespace Board.Tests
{
    /// <summary>
    /// A wrapper class that exposes the internal protected methods, so that they can be mocked.
    /// Note: To provide an expectation for this mock, be sure to set the <code>Callbase</code> property to true and then
    /// set expectations on the Ex methods. Only loose behavior works.
    /// </summary>
    public abstract class MockableGpioDriver : GpioDriver
    {
        protected override int PinCount
        {
            get
            {
                return 28;
            }
        }

        public abstract int ConvertPinNumberToLogicalNumberingSchemeEx(int pinNumber);

        protected override int ConvertPinNumberToLogicalNumberingScheme(int pinNumber)
        {
            return ConvertPinNumberToLogicalNumberingSchemeEx(pinNumber);
        }

        public abstract void OpenPinEx(int pinNumber);

        protected override void OpenPin(int pinNumber)
        {
            OpenPinEx(pinNumber);
        }

        public abstract void ClosePinEx(int pinNumber);

        protected override void ClosePin(int pinNumber)
        {
            ClosePinEx(pinNumber);
        }

        public abstract void SetPinModeEx(int pinNumber, PinMode mode);

        protected override void SetPinMode(int pinNumber, PinMode mode)
        {
            SetPinModeEx(pinNumber, mode);
        }

        public abstract PinMode GetPinModeEx(int pinNumber);

        protected override PinMode GetPinMode(int pinNumber)
        {
            return GetPinModeEx(pinNumber);
        }

        public abstract bool IsPinModeSupportedEx(int pinNumber, PinMode mode);

        protected override bool IsPinModeSupported(int pinNumber, PinMode mode)
        {
            return IsPinModeSupportedEx(pinNumber, mode);
        }

        public abstract PinValue ReadEx(int pinNumber);

        protected override PinValue Read(int pinNumber)
        {
            return ReadEx(pinNumber);
        }

        public abstract void WriteEx(int pinNumber, PinValue value);

        protected override void Write(int pinNumber, PinValue value)
        {
            WriteEx(pinNumber, value);
        }

        public abstract WaitForEventResult WaitForEventEx(int pinNumber, PinEventTypes eventTypes, CancellationToken cancellationToken);

        protected override WaitForEventResult WaitForEvent(int pinNumber, PinEventTypes eventTypes, CancellationToken cancellationToken)
        {
            return WaitForEventEx(pinNumber, eventTypes, cancellationToken);
        }

        public abstract void AddCallbackForPinValueChangedEventEx(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback);

        protected override void AddCallbackForPinValueChangedEvent(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback)
        {
            AddCallbackForPinValueChangedEventEx(pinNumber, eventTypes, callback);
        }

        public abstract void RemoveCallbackForPinValueChangedEventEx(int pinNumber, PinChangeEventHandler callback);

        protected override void RemoveCallbackForPinValueChangedEvent(int pinNumber, PinChangeEventHandler callback)
        {
            RemoveCallbackForPinValueChangedEventEx(pinNumber, callback);
        }
    }
}
