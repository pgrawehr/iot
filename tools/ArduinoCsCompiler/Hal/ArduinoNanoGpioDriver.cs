// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArduinoCsCompiler
{
    /// <summary>
    /// The references in here must refer to nanoframework.System.Device.Gpio.
    /// </summary>
    internal class ArduinoNanoGpioDriver : GpioDriver
    {
        public ArduinoNanoGpioDriver()
        {
            PinCount = 20; // Todo: Get the actual pin count from the board definition
        }

        protected override int PinCount { get; }
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
