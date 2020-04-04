using System;
using System.Collections.Generic;
using System.Device.Analog;
using System.Device.Gpio;
using System.Linq;
using System.Text;
using System.Threading;

namespace Iot.Device.Arduino
{
    internal class ArduinoAnalogControllerDriver : AnalogControllerDriver
    {
        private readonly ArduinoBoard _board;
        private readonly List<SupportedPinConfiguration> _supportedPinConfigurations;

        public ArduinoAnalogControllerDriver(ArduinoBoard board,
            List<SupportedPinConfiguration> supportedPinConfigurations)
        {
            _board = board ?? throw new ArgumentNullException(nameof(board));
            _supportedPinConfigurations = supportedPinConfigurations ?? throw new ArgumentNullException(nameof(supportedPinConfigurations));
            PinCount = _supportedPinConfigurations.Count;
            VoltageReference = 5.0;
        }

        public override int PinCount
        {
            get;
        }

        protected override int ConvertPinNumberToLogicalNumberingScheme(int pinNumber)
        {
            var firstAnalogPin = _supportedPinConfigurations.FirstOrDefault(x => x.PinModes.Contains(SupportedMode.ANALOG_INPUT));
            if (firstAnalogPin == null)
            {
                return 0;
            }

            return pinNumber - firstAnalogPin.Pin;
        }

        public override bool SupportsAnalogInput(int pinNumber)
        {
            return _supportedPinConfigurations[pinNumber].PinModes.Contains(SupportedMode.ANALOG_INPUT);
        }

        public override void OpenPin(int pinNumber)
        {
            if (!_supportedPinConfigurations[pinNumber].PinModes.Contains(SupportedMode.ANALOG_INPUT))
            {
                throw new NotSupportedException($"Pin {pinNumber} does not support Analog input");
            }

            _board.Firmata.SetPinMode(pinNumber, SupportedMode.ANALOG_INPUT);
            _board.Firmata.EnableAnalogReporting(ConvertPinNumberToLogicalNumberingScheme(pinNumber));
        }

        public override void ClosePin(int pinNumber)
        {
            _board.Firmata.DisableAnalogReporting(ConvertPinNumberToLogicalNumberingScheme(pinNumber));
        }

        /// <summary>
        /// Return the resolution of an analog input pin.
        /// </summary>
        /// <param name="pinNumber">The pin number</param>
        /// <param name="numberOfBits">Returns the resolution of the ADC in number of bits, including the sign bit (if applicable)</param>
        /// <param name="minVoltage">Minimum measurable voltage</param>
        /// <param name="maxVoltage">Maximum measurable voltage</param>
        public override void QueryResolution(int pinNumber, out int numberOfBits, out double minVoltage, out double maxVoltage)
        {
            numberOfBits = _supportedPinConfigurations[pinNumber].AnalogInputResolutionBits;
            minVoltage = 0.0;
            maxVoltage = VoltageReference;
        }

        public override uint ReadRaw(int pinNumber)
        {
            return _board.Firmata.GetAnalogRawValue(ConvertPinNumberToLogicalNumberingScheme(pinNumber));
        }

        public override double ReadVoltage(int pinNumber)
        {
            QueryResolution(pinNumber, out int numberOfBits, out double minVoltage, out double maxVoltage);
            uint raw = ReadRaw(pinNumber);
            if (minVoltage >= 0)
            {
                // The ADC can handle only positive values
                int maxRawValue = (1 << numberOfBits) - 1;
                double voltage = ((double)raw / maxRawValue) * maxVoltage;
                return voltage;
            }
            else
            {
                // The ADC also handles negative values. This means that the number of bits includes the sign.
                uint maxRawValue = (uint)((1 << (numberOfBits - 1)) - 1);
                if (raw < maxRawValue)
                {
                    double voltage = ((double)raw / maxRawValue) * maxVoltage;
                    return voltage;
                }
                else
                {
                    // This is a bitmask which has all the bits 1 that are not used in the data.
                    // We use this to sign-extend our raw value. The mask also includes the sign bit itself,
                    // but we know that this is already 1
                    uint topBits = ~maxRawValue;
                    raw |= topBits;
                    int raw2 = (int)raw;
                    double voltage = ((double)raw2 / maxRawValue) * maxVoltage;
                    return voltage; // This is now negative
                }
            }

        }
    }
}
