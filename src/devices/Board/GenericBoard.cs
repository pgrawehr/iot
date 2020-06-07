using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.I2c;
using System.Device.Pwm;
using System.Device.Spi;
using System.Text;

namespace Iot.Device.Board
{
    public class GenericBoard : Board
    {
        public GenericBoard(PinNumberingScheme defaultNumberingScheme)
            : base(defaultNumberingScheme)
        {
            if (defaultNumberingScheme != PinNumberingScheme.Logical)
            {
                throw new NotSupportedException("This board only supports logical pin numbering");
            }
        }

        public override int ConvertPinNumberToLogicalNumberingScheme(int pinNumber)
        {
            return pinNumber;
        }

        public override int ConvertLogicalNumberingSchemeToPinNumber(int pinNumber)
        {
            return pinNumber;
        }

        private GpioDriver CreateDriver(int[] pinAssignment)
        {
            GpioDriver driver = ManagedGpioDriver.GetBestDriverForBoard();

            return new ManagedGpioDriver(this, driver, pinAssignment);
        }

        public override GpioController CreateGpioController(int[] pinAssignment = null)
        {
            return new GpioController(PinNumberingScheme.Logical, CreateDriver(pinAssignment));
        }

        public override I2cDevice CreateI2cDevice(I2cConnectionSettings connectionSettings, int[] pinAssignment, PinNumberingScheme pinNumberingScheme)
        {
            if (pinAssignment == null || pinAssignment.Length != 2)
            {
                throw new ArgumentException($"Invalid argument. Must provide exactly two pins for I2C", nameof(pinAssignment));
            }

            return new I2cDeviceManager(this, connectionSettings, RemapPins(pinAssignment, pinNumberingScheme));
        }

        public override SpiDevice CreateSpiDevice(SpiConnectionSettings settings)
        {
            return SpiDevice.Create(settings);
        }

        public override PwmChannel CreatePwmChannel(int chip, int channel, int frequency, double dutyCyclePercentage,
            int pin, PinNumberingScheme pinNumberingScheme)
        {
            return new PwmChannelManager(this, RemapPin(pin, pinNumberingScheme), chip, channel, frequency, dutyCyclePercentage);
        }

        /// <summary>
        /// Check whether the given pin is usable for the given purpose.
        /// This implementation always returns unknown, since the generic board requires the user to know what he's doing.
        /// </summary>
        public override AlternatePinMode GetHardwareModeForPinUsage(int pinNumber, PinUsage usage, PinNumberingScheme pinNumberingScheme = PinNumberingScheme.Logical, int bus = 0)
        {
            return AlternatePinMode.Unknown;
        }

        protected override int[] GetDefaultPinAssignmentForI2c(I2cConnectionSettings connectionSettings)
        {
            throw new NotSupportedException("For the generic board, you need to specify the pin to use for I2C");
        }

        protected override int GetDefaultPinAssignmentForPwm(int chip, int channel)
        {
            throw new NotSupportedException("For the generic board, you need to specify the pin to use for pwm");
        }
    }
}
