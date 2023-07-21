using System;
using System.Collections.Generic;
using System.Device.Analog;
using System.Device.Gpio;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.Arduino;
using Iot.Device.Common;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace DisplayControl
{
    public class ArduinoSensors : PollingSensorBase
    {
        private readonly EngineSurveillance _engine;
        const int RpmSensorPin = 2;
        const int TankSensorRelaisPin = 7;
        private ArduinoBoard _board;
        private ILogger _logger;
        private FrequencySensor _frequencySensor;
        private SensorMeasurement _frequencyMeasurement;
        private SensorMeasurement _tankFillLevelRaw;
        private GpioController _gpioController;
        private bool _tankSensorIsOn;
        private HysteresisFilter _tankSensorEnableFilter;
        private AnalogController _analogController;
        private AnalogInputPin _tankSensorValuePin;

        public ArduinoSensors(MeasurementManager manager, EngineSurveillance engine) : base(manager,
            TimeSpan.FromSeconds(1))
        {
            _engine = engine;
            _logger = this.GetCurrentClassLogger();
            _tankSensorEnableFilter = new HysteresisFilter(false);
            _tankSensorEnableFilter.RisingDelayTime = TimeSpan.FromSeconds(2);
            _tankSensorEnableFilter.FallingDelayTime = TimeSpan.FromSeconds(30);
            ForceTankSensorEnable = false;
            _tankSensorIsOn = false;
        }

        public bool ForceTankSensorEnable
        {
            get;
            set;
        }

        public DhtSensor DhtInterface { get; set; }

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

            int i = 4;
            while (i-- > 0)
            {
                try
                {
                    _gpioController.OpenPin(RpmSensorPin, PinMode.Input);
                    _gpioController.ClosePin(RpmSensorPin);

                    _gpioController.OpenPin(TankSensorRelaisPin, PinMode.Output);
                    _gpioController.Write(TankSensorRelaisPin, _tankSensorIsOn);

                    _frequencySensor.EnableFrequencyReporting(RpmSensorPin, FrequencyMode.Falling, 1000);
                    break;
                }
                catch (TimeoutException x)
                {
                    if (i <= 0)
                    {
                        throw;
                    }

                    _logger.LogError(x, "Error opening pins. Retrying...");
                    Thread.Sleep(100);
                }
            }

            _frequencyMeasurement = new SensorMeasurement("Alternate RPM sensor", RotationalSpeed.Zero,
                SensorSource.Engine, 2,
                TimeSpan.FromSeconds(3));

            Manager.AddMeasurement(_frequencyMeasurement);
            Manager.AddMeasurement(SensorMeasurement.Engine0Rpm);

            // The value is valid until a new measurement arives (it is kept even if the tank sensor is switched off)
            _tankFillLevelRaw = new SensorMeasurement("Fuel tank raw value", ElectricPotential.Zero, SensorSource.Fuel, 1, TimeSpan.FromDays(100));
            Manager.AddMeasurement(_tankFillLevelRaw);

            Manager.AddMeasurement(SensorMeasurement.FuelTank0Level);

            _analogController = _board.CreateAnalogController(0);
            _tankSensorValuePin = _analogController.OpenPin(18); // A4

            DhtInterface = _board.GetCommandHandler<DhtSensor>();
            _board.SetPinMode(3, SupportedMode.Dht);

            base.Init(gpioController);
        }

        protected override void UpdateSensors()
        {
            var freq = _frequencySensor.GetMeasuredFrequency();
            freq = freq / _engine.EngineArduinoRpmCorrectionFactor;
            SensorMeasurement.Engine0Rpm.UpdateValue(RotationalSpeed.FromRevolutionsPerMinute(freq.CyclesPerMinute));
            _frequencyMeasurement.UpdateValue(RotationalSpeed.FromRevolutionsPerMinute(freq.CyclesPerMinute));

            var engOnV = (CustomData<bool>)SensorMeasurement.Engine0On;
            bool engOn = engOnV.Value;
            _tankSensorEnableFilter.Update(engOn);
            bool newSensorValue = ForceTankSensorEnable || _tankSensorEnableFilter.Output;

            if (newSensorValue != _tankSensorIsOn)
            {
                _tankSensorIsOn = newSensorValue;
                _gpioController.Write(TankSensorRelaisPin, _tankSensorIsOn);
            }

            
            var voltage = _tankSensorValuePin.ReadVoltage();
            if (_tankSensorIsOn)
            {
                _tankFillLevelRaw.UpdateValue(voltage, SensorMeasurementStatus.None, false);
                double v = voltage.Volts;
                double percentage = 0;
                if (v < 4.9) // If above, the value is invalid (sensor off, broken connection etc)
                {
                    // ~2.25V Full, 2.0V 75% 1.3V 20% 0.21V leer
                    // We use a stepwise linear function, as below. The tank size is not linear from top to bottom
                    // a and b are the constants for the linear equation between that point and the point above.
                    // Input   Expected result	a             b
                    // 2.25    100
                    // 2       75               100           -125
                    // 1.3     20               78.57142857   -82.14285714
                    // 0.21    0                18.34862385   -3.853211009
                    if (v >= 2.0)
                    {
                        percentage = 100 * v + -125;
                    }
                    else if (v >= 1.3)
                    {
                        percentage = 78.57142857 * v + -82.14285714;
                    }
                    else
                    {
                        percentage = 18.34862385 + v + -3.853211009;
                    }

                    // no clamping of percentage, so we would see out-of-bounds values.
                    SensorMeasurement.FuelTank0Level.UpdateValue(Ratio.FromPercent(percentage), SensorMeasurementStatus.None, false);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopThread();

                _gpioController.Write(TankSensorRelaisPin, PinValue.Low);
                _tankSensorValuePin.Dispose();
                _analogController.Dispose();
                _tankSensorValuePin = null;
                _analogController = null;
                _board?.Dispose();
                _board = null;
            }

            base.Dispose(disposing);
        }
    }
}
