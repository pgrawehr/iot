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
        const int RpmSensorPin = 2;
        const int TankSensorRelaisPin = 7;
        private ArduinoBoard _board;
        private ILogger _logger;
        private FrequencySensor _frequencySensor;
        private SensorMeasurement _frequencyMeasurement;
        private GpioController _gpioController;
        private bool _tankSensorIsOn;

        public ArduinoSensors(MeasurementManager manager) : base(manager,
            TimeSpan.FromSeconds(1))
        {
            _logger = this.GetCurrentClassLogger();
            ForceTankSensorEnable = false;
            _tankSensorIsOn = false;
        }

        public bool ForceTankSensorEnable
        {
            get;
            set;
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
            _gpioController.OpenPin(RpmSensorPin, PinMode.Input);
            _gpioController.ClosePin(RpmSensorPin);

            _gpioController.OpenPin(TankSensorRelaisPin, PinMode.Output);
            _gpioController.Write(TankSensorRelaisPin, _tankSensorIsOn);

            _frequencySensor.EnableFrequencyReporting(RpmSensorPin, FrequencyMode.Falling, 1000);
            _frequencyMeasurement = new SensorMeasurement("Alternate RPM sensor", RotationalSpeed.Zero, SensorSource.Engine, 2,
                TimeSpan.FromSeconds(3));
            Manager.AddMeasurement(_frequencyMeasurement);
            base.Init(gpioController);
        }

        protected override void UpdateSensors()
        {
            var freq = _frequencySensor.GetMeasuredFrequency();
            freq = freq / EngineSurveillance.TicksPerRevolution;
            _frequencyMeasurement.UpdateValue(RotationalSpeed.FromRevolutionsPerMinute(freq.CyclesPerMinute));

            var engOn = (CustomData<bool>)SensorMeasurement.Engine0On;
            bool newSensorValue = ForceTankSensorEnable || engOn.Value;

            if (newSensorValue != _tankSensorIsOn)
            {
                _tankSensorIsOn = newSensorValue;
                _gpioController.Write(TankSensorRelaisPin, _tankSensorIsOn);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopThread();

                _gpioController.Write(TankSensorRelaisPin, PinValue.Low);
                _board?.Dispose();
                _board = null;
            }

            base.Dispose(disposing);
        }
    }
}
