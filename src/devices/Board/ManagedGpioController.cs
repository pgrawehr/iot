using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Iot.Device.Board
{
    internal class ManagedGpioDriver : GpioDriver
    {
        private BoardBase _board;
        private GpioDriver _driver;
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

        public ManagedGpioDriver(BoardBase board, GpioDriver driver)
        {
            _board = board;
            _driver = driver;
            InitFunctionPointers();
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
        }

        protected override int ConvertPinNumberToLogicalNumberingScheme(int pinNumber)
        {
            return _board.ConvertPinNumberToLogicalNumberingScheme(pinNumber);
        }

        protected override void OpenPin(int pinNumber)
        {
            _openPinMethodInfo.Invoke(_driver, new object[] { pinNumber });
        }

        protected override void ClosePin(int pinNumber)
        {
            _closePinMethodInfo.Invoke(_driver, new object[] { pinNumber });
        }

        protected override void SetPinMode(int pinNumber, PinMode mode)
        {
            _setPinModeMethodInfo.Invoke(_driver, new object[] { pinNumber, mode });
        }

        protected override PinMode GetPinMode(int pinNumber)
        {
            return (PinMode)_getPinModeMethodInfo.Invoke(_driver, new object[] { pinNumber });
        }

        protected override bool IsPinModeSupported(int pinNumber, PinMode mode)
        {
            return (bool)_isPinModeSupportedMethodInfo.Invoke(_driver, new object[] { pinNumber, mode });
        }

        protected override PinValue Read(int pinNumber)
        {
            return (PinValue)_readMethodInfo.Invoke(_driver, new object[] { pinNumber });
        }

        protected override void Write(int pinNumber, PinValue value)
        {
            _writeMethodInfo.Invoke(_driver, new object[] { pinNumber, value });
        }

        protected override WaitForEventResult WaitForEvent(int pinNumber, PinEventTypes eventTypes, CancellationToken cancellationToken)
        {
            return (WaitForEventResult)_waitForEventMethodInfo.Invoke(_driver, new object[] { pinNumber, eventTypes, cancellationToken });
        }

        protected override void AddCallbackForPinValueChangedEvent(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback)
        {
            _addCallbackForPinValueChangedEventMethodInfo.Invoke(_driver, new object[] { pinNumber, eventTypes, callback });
        }

        protected override void RemoveCallbackForPinValueChangedEvent(int pinNumber, PinChangeEventHandler callback)
        {
            _removeCallbackForPinValueChangedEventMethodInfo.Invoke(_driver, new object[] { pinNumber, callback });
        }
    }
}
