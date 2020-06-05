using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Iot.Device.Board
{
    /// <summary>
    /// A GPIO Driver for testing on Windows
    /// </summary>
    internal class KeyboardGpioDriver : GpioDriver
    {
        private enum LedKey
        {
            NumLock,
            CapsLock,
            ScrollLock,
        }

        private const int SupportedPinCount = 256;

        private KeyState[] _state;

        public KeyboardGpioDriver()
        {
            _state = new KeyState[SupportedPinCount];
            for (int i = 0; i < SupportedPinCount; i++)
            {
                _state[i] = new KeyState((ConsoleKey)i);
            }
        }

        protected override int PinCount
        {
            get
            {
                // The ConsoleKey enum is used to index into our pins, if needed. This one does not use values below 8, so
                // we'll use 3 for the LEDs.
                return SupportedPinCount;
            }
        }

        protected override int ConvertPinNumberToLogicalNumberingScheme(int pinNumber)
        {
            return pinNumber;
        }

        protected override void OpenPin(int pinNumber)
        {
        }

        protected override void ClosePin(int pinNumber)
        {
        }

        protected override void SetPinMode(int pinNumber, PinMode mode)
        {
            if (IsPinModeSupported(pinNumber, mode))
            {
                _state[pinNumber].Mode = mode;
            }
        }

        protected override PinMode GetPinMode(int pinNumber)
        {
            return _state[pinNumber].Mode;
        }

        protected override bool IsPinModeSupported(int pinNumber, PinMode mode)
        {
            if (pinNumber < 3)
            {
                // Output-only pins (the three LEDs on the keyboard)
                if (mode == PinMode.Output)
                {
                    return true;
                }

                return false;
            }

            if (pinNumber >= 8)
            {
                if (mode == PinMode.Input || mode == PinMode.InputPullDown || mode == PinMode.InputPullUp)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsKeyPressed(ConsoleKey key)
        {
            short state = Interop.GetKeyState((int)key);
            return (state & 0xFFFE) != 0; // any bits except the lowest
        }

        private void SetLedState(LedKey key, PinValue state)
        {
            int virtualKey = 0;
            if (key == LedKey.NumLock)
            {
                virtualKey = Interop.VK_NUMLOCK;
            }
            else if (key == LedKey.CapsLock)
            {
                virtualKey = Interop.VK_CAPITAL;
            }
            else if (key == LedKey.ScrollLock)
            {
                virtualKey = Interop.VK_SCROLL;
            }
            else
            {
                throw new NotSupportedException("No such key");
            }

            // Bit 1 indicates whether the LED is currently on or off (or, whether Scroll lock, num lock, caps lock is on)
            int currentKeyState = Interop.GetKeyState(virtualKey) & 1;
            if ((state == PinValue.High && currentKeyState == 0) ||
                (state == PinValue.Low && currentKeyState != 0))
            {
                // Simulate a key press
                Interop.keybd_event((byte)virtualKey,
                    0x45,
                    Interop.KEYEVENTF_EXTENDEDKEY | 0,
                    IntPtr.Zero);

                // Simulate a key release
                Interop.keybd_event((byte)virtualKey,
                    0x45,
                    Interop.KEYEVENTF_EXTENDEDKEY | Interop.KEYEVENTF_KEYUP,
                    IntPtr.Zero);
            }
        }

        protected override PinValue Read(int pinNumber)
        {
            short currentKeyState = Interop.GetAsyncKeyState(pinNumber);
            if ((currentKeyState & 0xFFFE) != 0)
            {
                return PinValue.High;
            }
            else
            {
                return PinValue.Low;
            }
        }

        protected override void Write(int pinNumber, PinValue value)
        {
            if (pinNumber == 0)
            {
                SetLedState(LedKey.NumLock, value);
            }

            if (pinNumber == 1)
            {
                SetLedState(LedKey.ScrollLock, value);
            }

            if (pinNumber == 2)
            {
                SetLedState(LedKey.CapsLock, value);
            }
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

        private sealed class KeyState
        {
            public KeyState(ConsoleKey key)
            {
                Key = key;
            }

            public ConsoleKey Key
            {
                get;
            }

            public PinMode Mode
            {
                get;
                set;
            }
        }
    }
}
