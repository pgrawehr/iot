using System;
using System.Collections.Generic;
using System.Device.Analog;
using System.Text;
using System.Device.Gpio;
using System.Device.I2c;
using System.Device.Pwm;
using System.Device.Spi;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Iot.Device.Spi;

#pragma warning disable CS1591

namespace Iot.Device.Arduino
{
    /// <summary>
    /// Implements an interface to an arduino board which is running Firmata.
    /// See documentation on how to prepare your arduino board to work with this.
    /// Note that the program will run on the PC, so you cannot disconnect the
    /// Arduino while this driver is connected.
    /// </summary>
    public class ArduinoBoard : IDisposable
    {
        private Stream _serialPortStream;
        private FirmataDevice _firmata;
        private Version _firmwareVersion;
        private string _firmwareName;
        private List<SupportedPinConfiguration> _supportedPinConfigurations;

        public ArduinoBoard(Stream serialPortStream)
        {
            _serialPortStream = serialPortStream;
        }

        public event Action<string, Exception> LogMessages;

        public virtual void Initialize()
        {
            _firmata = new FirmataDevice();
            _firmata.Open(_serialPortStream);
            _firmata.OnError += FirmataOnError;
            var protocolVersion = _firmata.QueryFirmataVersion();
            if (protocolVersion != _firmata.QuerySupportedFirmataVersion())
            {
                throw new NotSupportedException($"Firmata version on board is {protocolVersion}. Expected {_firmata.QuerySupportedFirmataVersion()}. They must be equal.");
            }

            Log($"Firmata version on board is {protocolVersion}.");

            _firmwareVersion = _firmata.QueryFirmwareVersion(out _firmwareName);

            Log($"Firmware version on board is {_firmwareVersion}");

            _firmata.QueryCapabilities();

            _supportedPinConfigurations = _firmata.PinConfigurations; // Clone reference

            Log("Device capabilities: ");
            foreach (var pin in _supportedPinConfigurations)
            {
                Log(pin.ToString());
            }

            // _firmata.SetSamplingInterval(TimeSpan.FromMilliseconds(100));
            _firmata.EnableDigitalReporting();
        }

        public Version FirmwareVersion
        {
            get
            {
                return _firmwareVersion;
            }
        }

        public string FirmwareName
        {
            get
            {
                return _firmwareName;
            }
        }

        internal FirmataDevice Firmata
        {
            get
            {
                return _firmata;
            }
        }

        internal List<SupportedPinConfiguration> SupportedPinConfigurations
        {
            get
            {
                return _supportedPinConfigurations;
            }
        }

        internal void Log(string message)
        {
            LogMessages?.Invoke(message, null);
        }

        private void FirmataOnError(string message, Exception innerException)
        {
            LogMessages?.Invoke(message, innerException);
        }

        public GpioController CreateGpioController(PinNumberingScheme pinNumberingScheme)
        {
            return new GpioController(pinNumberingScheme, new ArduinoGpioControllerDriver(this, _supportedPinConfigurations));
        }

        public I2cDevice CreateI2cDevice(I2cConnectionSettings connectionSettings)
        {
            return new ArduinoI2cDevice(this, connectionSettings);
        }

        /// <summary>
        /// Firmata has no support for SPI, even though the Arduino basically has an SPI interface.
        /// This therefore returns a Software SPI device for the default Arduino SPI port on pins 11, 12 and 13.
        /// </summary>
        /// <param name="settings">Spi Connection settings</param>
        /// <returns></returns>
        public SpiDevice CreateSpiDevice(SpiConnectionSettings settings)
        {
            int mosi = 11;
            int miso = 12;
            int sck = 13;
            return new SoftwareSpi(sck, miso, mosi, settings.ChipSelectLine, settings,
                CreateGpioController(PinNumberingScheme.Board));
        }

        public PwmChannel CreatePwmChannel(
            int chip,
            int channel,
            int frequency = 400,
            double dutyCyclePercentage = 0.5)
        {
            return new ArduinoPwmChannel(this, chip, channel, frequency, dutyCyclePercentage);
        }

        public AnalogController CreateAnalogController(int chip)
        {
            return new AnalogController(PinNumberingScheme.Logical, new ArduinoAnalogControllerDriver(this, _supportedPinConfigurations));
        }

        protected virtual void Dispose(bool disposing)
        {
            // Do this first, to force any blocking read operations to end
            if (_serialPortStream != null)
            {
                _serialPortStream.Close();
                _serialPortStream.Dispose();
            }

            _serialPortStream = null;
            if (_firmata != null)
            {
                _firmata.OnError -= FirmataOnError;
                _firmata.Close();
                _firmata.Dispose();
            }

            _firmata = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
