using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Gpio.Drivers;
using System.Device.I2c;
using System.Device.Pwm;
using System.Device.Pwm.Channels;
using System.Device.Spi;
using System.Text;

namespace Iot.Device.Board
{
    /// <summary>
    /// A generic board for Unix platforms
    /// </summary>
    public class UnixBoard : Board
    {
        private GpioDriver _internalDriver;
        private bool _useLibgpiod;

        public UnixBoard(PinNumberingScheme defaultNumberingScheme, bool useLibgpiod = true)
            : base(defaultNumberingScheme)
        {
            _internalDriver = null;
            _useLibgpiod = useLibgpiod;
        }

        /// <summary>
        /// True if the Libgpiod driver is used, false if SysFs is used.
        /// Returns false until <seealso cref="Initialize"/> is called.
        /// </summary>
        public bool LibGpiodDriverUsed
        {
            get
            {
                return _internalDriver is LibGpiodDriver;
            }
        }

        public override GpioController CreateGpioController(int[] pinAssignment = null)
        {
            var driver = CreateGpioDriver();
            return new GpioController(DefaultPinNumberingScheme, new ManagedGpioDriver(this, driver, pinAssignment));
        }

        protected virtual GpioDriver CreateGpioDriver()
        {
            if (_useLibgpiod == false)
            {
                return new SysFsDriver();
            }

            UnixDriver driver = null;
            try
            {
                driver = new LibGpiodDriver();
            }
            catch (PlatformNotSupportedException)
            {
                driver = new SysFsDriver();
            }

            return driver;
        }

        public override int ConvertPinNumberToLogicalNumberingScheme(int pinNumber)
        {
            return pinNumber;
        }

        public override int ConvertLogicalNumberingSchemeToPinNumber(int pinNumber)
        {
            return pinNumber;
        }

        public override void Initialize()
        {
            base.Initialize();

            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                // Something is really wrong here.
                throw new PlatformNotSupportedException("This board type is only supported on linux/unix.");
            }

            // Try to create a GpioController - if that succeeds, we're probably on compatible hardware
            try
            {
                UnixDriver driver = UnixDriver.Create();
                _internalDriver = driver;
            }
            catch (Exception x) when (!(x is NullReferenceException))
            {
                throw new PlatformNotSupportedException($"Unable to open GPIO device: {x.Message}", x);
            }
        }

        public override I2cDevice CreateI2cDevice(I2cConnectionSettings connectionSettings, int[] pinAssignment)
        {
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_internalDriver != null)
                {
                    _internalDriver.Dispose();
                    _internalDriver = null;
                }
            }

            base.Dispose(disposing);
        }
    }
}
