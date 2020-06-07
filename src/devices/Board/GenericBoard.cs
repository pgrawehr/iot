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

        public override I2cDevice CreateI2cDevice(I2cConnectionSettings connectionSettings, int[] pinAssignment)
        {
            if (pinAssignment == null || pinAssignment.Length != 2)
            {
                throw new ArgumentException($"Invalid argument. Must provide exactly two pins for I2C", nameof(pinAssignment));
            }

            return new I2cDeviceManager(this, connectionSettings, pinAssignment);
        }

        public override SpiDevice CreateSpiDevice(SpiConnectionSettings settings)
        {
            return SpiDevice.Create(settings);
        }

        public override PwmChannel CreatePwmChannel(int chip, int channel, int frequency = 400, double dutyCyclePercentage = 0.5)
        {
            return PwmChannel.Create(chip, channel, frequency, dutyCyclePercentage);
        }

        /// <summary>
        /// Check whether the given pin is usable for the given purpose.
        /// This implementation always returns true, since the generic board requires the user to know what he's doing.
        /// </summary>
        public override bool IsPinUsableFor(int pinNumber, PinUsage usage, PinNumberingScheme pinNumberingScheme = PinNumberingScheme.Logical, int bus = 0)
        {
            return true;
        }

        protected override int[] GetPinAssignmentForI2c(I2cConnectionSettings connectionSettings, int[] logicalPinAssignment)
        {
            if (logicalPinAssignment == null || logicalPinAssignment.Length != 2)
            {
                throw new NotSupportedException("For the generic board, exactly two pins need to be assigned to an I2C bus");
            }

            return logicalPinAssignment;
        }
    }
}
