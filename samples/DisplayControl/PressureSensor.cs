using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Text;
using System.Threading;
using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.PowerMode;

namespace DisplayControl
{
    public class PressureSensor : PollingSensorBase
    {
        private Bmp280 _bmp280;
        private ObservableValue<double> _outsideTemperature;
        private ObservableValue<double> _pressure;

        public PressureSensor() : base(TimeSpan.FromSeconds(5))
        {
        }

        public override void Init(GpioController gpioController)
        {
            _bmp280 = new Bmp280(I2cDevice.Create(new I2cConnectionSettings(1, Bmp280.DefaultI2cAddress)));
            _bmp280.TemperatureSampling = Sampling.Standard;
            
            _bmp280.PressureSampling = Sampling.UltraHighResolution;
            _outsideTemperature = new ObservableValue<double>("Temperature Outside", "°C", double.NaN);
            _pressure = new ObservableValue<double>("Pressure Outside", "hPa", double.NaN);
            
            _outsideTemperature.ValueFormatter = "{0:F1}";
            _pressure.ValueFormatter = "{0:F1}";
            SensorValueSources.Add(_outsideTemperature);
            SensorValueSources.Add(_pressure);

            base.Init(gpioController);
        }

        protected override void UpdateSensors()
        {
            // Take one measurement, then sleep as long as this takes
            _bmp280.SetPowerMode(Bmx280PowerMode.Forced);
            var measurementTime = _bmp280.GetMeasurementDuration();
            Thread.Sleep(measurementTime);
            if (_bmp280.TryReadTemperature(out var tempValue))
            {
                _outsideTemperature.Value = tempValue.DegreesCelsius;
            }

            if (_bmp280.TryReadPressure(out var preValue))
            {
                _pressure.Value = preValue.Hectopascals;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _bmp280.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
