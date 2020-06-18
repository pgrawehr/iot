using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Iot.Device.Board
{
    internal class ManagedGpioDriver : GpioDriver
    {
        private readonly int[] _pinAssignment;
        private readonly Board _board;
        private readonly GpioDriver _driver;
        private int _pinCount;
        private MethodInfo _openPinMethodInfo;
        private MethodInfo _closePinMethodInfo;
        private MethodInfo _setPinModeMethodInfo;
        private MethodInfo _getPinModeMethodInfo;
        private MethodInfo _isPinModeSupportedMethodInfo;
        private MethodInfo _readMethodInfo;
        private MethodInfo _writeMethodInfo;
        private MethodInfo _waitForEventMethodInfo;
        private MethodInfo _addCallbackForPinValueChangedEventMethodInfo;
        private MethodInfo _removeCallbackForPinValueChangedEventMethodInfo;
        private MethodInfo _getAlternatePinModeMethodInfo;
        private MethodInfo _setAlternatePinModeMethodInfo;

        public ManagedGpioDriver(Board board, GpioDriver driver, int[] pinAssignment)
        {
            _board = board;
            _driver = driver;
            _pinAssignment = pinAssignment;
            InitFunctionPointers();
            if (pinAssignment != null)
            {
                foreach (var pin in pinAssignment)
                {
                    if (_board.GetHardwareModeForPinUsage(pin, PinUsage.Gpio) != AlternatePinMode.Gpio)
                    {
                        throw new NotSupportedException($"Logical pin {pin} does not support Gpio");
                    }
                }
            }
        }

        protected override int PinCount
        {
            get
            {
                return _pinCount;
            }
        }

        private void InitFunctionPointers()
        {
            var property = typeof(GpioDriver).GetProperty("PinCount",
                BindingFlags.NonPublic | BindingFlags.Instance);
            _pinCount = (int)property.GetValue(_driver); // this value is constant, so we just cache the value.
            _openPinMethodInfo = typeof(GpioDriver).GetMethod("OpenPin", BindingFlags.NonPublic | BindingFlags.Instance);
            _closePinMethodInfo = typeof(GpioDriver).GetMethod("ClosePin", BindingFlags.NonPublic | BindingFlags.Instance);
            _setPinModeMethodInfo = typeof(GpioDriver).GetMethod("SetPinMode", BindingFlags.NonPublic | BindingFlags.Instance);
            _getPinModeMethodInfo = typeof(GpioDriver).GetMethod("GetPinMode", BindingFlags.NonPublic | BindingFlags.Instance);
            _isPinModeSupportedMethodInfo = typeof(GpioDriver).GetMethod("IsPinModeSupported", BindingFlags.NonPublic | BindingFlags.Instance);
            _readMethodInfo = typeof(GpioDriver).GetMethod("Read", BindingFlags.NonPublic | BindingFlags.Instance);
            _writeMethodInfo = typeof(GpioDriver).GetMethod("Write", BindingFlags.NonPublic | BindingFlags.Instance);
            _waitForEventMethodInfo = typeof(GpioDriver).GetMethod("WaitForEvent", BindingFlags.NonPublic | BindingFlags.Instance);
            _addCallbackForPinValueChangedEventMethodInfo = typeof(GpioDriver).GetMethod("AddCallbackForPinValueChangedEvent", BindingFlags.NonPublic | BindingFlags.Instance);
            _removeCallbackForPinValueChangedEventMethodInfo = typeof(GpioDriver).GetMethod("RemoveCallbackForPinValueChangedEvent", BindingFlags.NonPublic | BindingFlags.Instance);
            _getAlternatePinModeMethodInfo = typeof(RaspberryPi3Driver).GetMethod("GetAlternatePinMode", BindingFlags.NonPublic | BindingFlags.Instance);
            _setAlternatePinModeMethodInfo = typeof(RaspberryPi3Driver).GetMethod("SetAlternatePinMode", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        internal static GpioDriver GetBestDriverForBoard()
        {
            var methodInfo = typeof(GpioController).GetMethod("GetBestDriverForBoard", BindingFlags.NonPublic | BindingFlags.Static);
            if (methodInfo == null)
            {
                throw new InvalidOperationException($"{nameof(GpioController)} is missing an implementation for GetBestDriverForBoard");
            }

            GpioDriver driver = null;
            try
            {
                driver = RethrowInnerException(() => (GpioDriver)methodInfo.Invoke(null, new object[0]));
            }
            catch (Exception x) when (x is PlatformNotSupportedException || x is NotSupportedException) // That would be serious
            {
            }

            if (driver == null)
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    driver = new KeyboardGpioDriver();
                }
                else
                {
                    driver = new DummyGpioDriver();
                }
            }

            return driver;
        }

        protected override int ConvertPinNumberToLogicalNumberingScheme(int pinNumber)
        {
            return RethrowInnerException(() => _board.ConvertPinNumberToLogicalNumberingScheme(pinNumber));
        }

        protected override void OpenPin(int pinNumber)
        {
            _board.ReservePin(pinNumber, PinUsage.Gpio, this);
            RethrowInnerException(() => _openPinMethodInfo.Invoke(_driver, new object[] { pinNumber }));
        }

        protected override void ClosePin(int pinNumber)
        {
            RethrowInnerException(() => _closePinMethodInfo.Invoke(_driver, new object[] { pinNumber }));
            _board.ReleasePin(pinNumber, PinUsage.Gpio, this);
        }

        protected override void SetPinMode(int pinNumber, PinMode mode)
        {
            RethrowInnerException(() => _setPinModeMethodInfo.Invoke(_driver, new object[] { pinNumber, mode }));
        }

        protected override PinMode GetPinMode(int pinNumber)
        {
            return RethrowInnerException(() => (PinMode)_getPinModeMethodInfo.Invoke(_driver, new object[] { pinNumber }));
        }

        protected override bool IsPinModeSupported(int pinNumber, PinMode mode)
        {
            return RethrowInnerException(() => (bool)_isPinModeSupportedMethodInfo.Invoke(_driver, new object[] { pinNumber, mode }));
        }

        protected override PinValue Read(int pinNumber)
        {
            return RethrowInnerException(() => (PinValue)_readMethodInfo.Invoke(_driver, new object[] { pinNumber }));
        }

        protected override void Write(int pinNumber, PinValue value)
        {
            RethrowInnerException(() => _writeMethodInfo.Invoke(_driver, new object[] { pinNumber, value }));
        }

        protected override WaitForEventResult WaitForEvent(int pinNumber, PinEventTypes eventTypes, CancellationToken cancellationToken)
        {
            return RethrowInnerException(() => (WaitForEventResult)_waitForEventMethodInfo.Invoke(_driver, new object[] { pinNumber, eventTypes, cancellationToken }));
        }

        protected override void AddCallbackForPinValueChangedEvent(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback)
        {
            RethrowInnerException(() => _addCallbackForPinValueChangedEventMethodInfo.Invoke(_driver, new object[] { pinNumber, eventTypes, callback }));
        }

        protected override void RemoveCallbackForPinValueChangedEvent(int pinNumber, PinChangeEventHandler callback)
        {
            RethrowInnerException(() => _removeCallbackForPinValueChangedEventMethodInfo.Invoke(_driver, new object[] { pinNumber, callback }));
        }

        /// <summary>
        /// Returns the currently set "alternate" pin mode, for pins which are multiplexed between different functions
        /// </summary>
        /// <param name="pinNumber">The pin number</param>
        /// <returns>The returned pin mode</returns>
        internal AlternatePinMode GetAlternatePinMode(int pinNumber)
        {
            if (_getAlternatePinModeMethodInfo == null)
            {
                return AlternatePinMode.NotSupported;
            }

            int mode = RethrowInnerException(() => (int)_getAlternatePinModeMethodInfo.Invoke(_driver, new object[] { pinNumber }));
            if (mode < 0)
            {
                return AlternatePinMode.Gpio;
            }
            else
            {
                return (AlternatePinMode)(mode + 1);
            }
        }

        internal void SetAlternatePinMode(int pinNumber, AlternatePinMode altMode)
        {
            int mode = 0;
            if (altMode < 0)
            {
                throw new ArgumentException("Invalid mode requested", nameof(altMode));
            }

            if (altMode == AlternatePinMode.Gpio)
            {
                // When going back to Gpio, default to input
                mode = -1;
            }
            else
            {
                mode = (int)altMode - 1;
            }

            RethrowInnerException(() => _setAlternatePinModeMethodInfo.Invoke(_driver, new object[] { pinNumber, mode }));
        }

        private static T RethrowInnerException<T>(Func<T> operation)
        {
            try
            {
                return operation();
            }
            catch (TargetInvocationException x)
            {
                // methodInfo.Invoke() returns a TargetInvocationException wrapping the original exception - so unpack and
                // throw the original.
                if (x.InnerException != null)
                {
                    throw x.InnerException;
                }

                throw;
            }
        }
    }
}
