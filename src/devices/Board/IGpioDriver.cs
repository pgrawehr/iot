using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Text;
using System.Threading;

namespace Iot.Device.Board
{
    public interface IGpioDriver
    {
        int PinCount
        {
            get;
        }

        void OpenPin(int pinNumber);
        void ClosePin(int pinNumber);

        void SetPinMode(int pinNumber, PinMode mode);

        PinMode GetPinMode(int pinNumber);

        bool IsPinModeSupported(int pinNumber, PinMode mode);

        PinValue Read(int pinNumber);

        void Write(int pinNumber, PinValue value);

        WaitForEventResult WaitForEvent(int pinNumber, PinEventTypes eventTypes, CancellationToken cancellationToken);

        void AddCallbackForPinValueChangedEvent(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback);

        void RemoveCallbackForPinValueChangedEvent(int pinNumber, PinChangeEventHandler callback);
    }
}
