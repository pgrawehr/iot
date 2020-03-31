using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Threading;

#pragma warning disable CS1591
namespace Iot.Device.Arduino
{
    public class ArduinoGpioControllerDriver : GpioDriver
    {
        private readonly ArduinoBoard _arduinoBoard;
        private readonly List<SupportedPinConfiguration> _supportedPinConfigurations;

        public ArduinoGpioControllerDriver(ArduinoBoard arduinoBoard, List<SupportedPinConfiguration> supportedPinConfigurations)
        {
            _arduinoBoard = arduinoBoard;
            _supportedPinConfigurations = supportedPinConfigurations;
            PinCount = _supportedPinConfigurations.Count;
        }

        protected override int PinCount { get; }

        /// <summary>
        /// Arduino does not distinguish between logical and physical numbers, so this always returns identity
        /// </summary>
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
            var pinConfig = _supportedPinConfigurations.FirstOrDefault(x => x.Pin == pinNumber);
            if (pinConfig == null || !pinConfig.PinModes.Contains(mode))
            {
                throw new NotSupportedException($"Mode {mode} is not supported on Pin {pinNumber}.");
            }

            _arduinoBoard.Firmata.SetPinMode(pinNumber, mode);
        }

        protected override PinMode GetPinMode(int pinNumber)
        {
            return _arduinoBoard.Firmata.GetPinMode(pinNumber);
        }

        protected override bool IsPinModeSupported(int pinNumber, PinMode mode)
        {
            var pinConfig = _supportedPinConfigurations.FirstOrDefault(x => x.Pin == pinNumber);
            if (pinConfig == null || !pinConfig.PinModes.Contains(mode))
            {
                return false;
            }

            return true;
        }

        protected override PinValue Read(int pinNumber)
        {
            throw new NotImplementedException();
        }

        protected override void Write(int pinNumber, PinValue value)
        {
            _arduinoBoard.Firmata.WriteDigitalPin(pinNumber, value);
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
