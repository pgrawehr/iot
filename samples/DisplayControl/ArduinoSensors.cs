using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iot.Device.Arduino;
using Iot.Device.Common;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace DisplayControl
{
    public class ArduinoSensors : PollingSensorBase
    {
        private ArduinoBoard _board;
        private ILogger _logger;
        private FrequencySensor _frequencySensor;
        private SensorMeasurement _frequencyMeasurement;
        private GpioController _gpioController;

        public ArduinoSensors(MeasurementManager manager) : base(manager,
            TimeSpan.FromSeconds(1))
        {
            _logger = this.GetCurrentClassLogger();
        }

        public override void Init(GpioController gpioController)
        {
            if (!ArduinoBoard.TryFindBoard(new string[] { "/dev/ttyACM0", "/dev/ttyUSB0", "/dev/ttyS0" }, new int[] { 115200 }, out _board))
            {
                throw new NotSupportedException("Could not find Arduino");
            }

            _logger.LogInformation($"Connected to Arduino, {_board.FirmwareName} {_board.FirmwareVersion}");
            _frequencySensor = _board.GetCommandHandler<FrequencySensor>();
            if (_frequencySensor == null)
            {
                throw new NotSupportedException("Invalid Arduino binding: No frequency module");
            }

            _gpioController = _board.CreateGpioController();
            _gpioController.OpenPin(2, PinMode.Input);
            _gpioController.ClosePin(2);

            _frequencySensor.EnableFrequencyReporting(2, FrequencyMode.Falling, 1000);
            _frequencyMeasurement = new SensorMeasurement("Alternate RPM sensor", RotationalSpeed.Zero, SensorSource.Engine, 2,
                TimeSpan.FromSeconds(3));
            Manager.AddMeasurement(_frequencyMeasurement);
            base.Init(gpioController);
        }

        protected override void UpdateSensors()
        {
            var freq = _frequencySensor.GetMeasuredFrequency();
            _frequencyMeasurement.UpdateValue(RotationalSpeed.FromRevolutionsPerMinute(freq.CyclesPerMinute));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopThread();

                _board?.Dispose();
                _board = null;
            }

            base.Dispose(disposing);
        }
    }
}
