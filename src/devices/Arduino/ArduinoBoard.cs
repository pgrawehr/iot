using System;
using System.Collections.Generic;
using System.Text;
using System.Device.Gpio;
using System.Device.I2c;
using System.Diagnostics;
using System.IO;
using System.Threading;

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

        public virtual void Initialize()
        {
            _firmata = new FirmataDevice();
            _firmata.Open(new FirmataStream(_serialPortStream));
            var protocolVersion = _firmata.QueryFirmataVersion();
            if (protocolVersion != _firmata.QuerySupportedFirmataVersion())
            {
                throw new NotSupportedException($"Firmata version on board is {protocolVersion}. Expected {_firmata.QuerySupportedFirmataVersion()}. They must be equal.");
            }

            _firmwareVersion = _firmata.QueryFirmwareVersion(out _firmwareName);

            _firmata.QueryCapabilities();
            _supportedPinConfigurations = _firmata.PinConfigurations; // Clone reference

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

        public GpioController CreateGpioController(PinNumberingScheme pinNumberingScheme)
        {
            return new GpioController(pinNumberingScheme, new ArduinoGpioControllerDriver(this, _supportedPinConfigurations));
        }

        public I2cDevice CreateI2cDevice(I2cConnectionSettings connectionSettings)
        {
            return new ArduinoI2cDevice(this, connectionSettings);
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
