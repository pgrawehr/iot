using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Text;
using System.Threading;
using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.PowerMode;
using Iot.Device.Common;

namespace DisplayControl
{
    public class Bmp680Environment : PollingSensorBase
    {
        public const string MAIN_TEMP_SENSOR = "Temperature Inside";
        public const string MAIN_PRESSURE_SENSOR = "Barometric Pressure";
        public const string MAIN_HUMIDITY_SENSOR = "Relative Humidity";
        private Bme680 _bme680;
        private ObservableValue<double> _temperature;
        private ObservableValue<double> _pressure;
        private ObservableValue<double> _gasValue;
        private ObservableValue<double> _humidity;
        private ObservableValue<double> _barometric;
        private ObservableValue<double> _dewPoint;

        public Bmp680Environment() : base(TimeSpan.FromSeconds(5))
        {
            Altitude = 0;
        }

        /// <summary>
        /// Set this regularly from the GPS receiver
        /// </summary>
        public double Altitude
        {
            get;
            set;
        }

        public override void Init(GpioController gpioController)
        {
            _bme680 = new Bme680(I2cDevice.Create(new I2cConnectionSettings(1, 0x76)));
            _bme680.TemperatureSampling = Sampling.Standard;

            _bme680.PressureSampling = Sampling.HighResolution;

            // Disable gas conversion - the value doesn't say much yet and it just heats up the sensor
            _bme680.GasConversionIsEnabled = false;
            _bme680.HeaterIsEnabled = false;

            _temperature = new ObservableValue<double>(MAIN_TEMP_SENSOR, "°C", double.NaN);
            _pressure = new ObservableValue<double>("Raw Pressure", "hPa", double.NaN);
            _barometric = new ObservableValue<double>(MAIN_PRESSURE_SENSOR, "hPa", double.NaN);
            _humidity = new ObservableValue<double>(MAIN_HUMIDITY_SENSOR, "%", Double.NaN);
            _gasValue = new ObservableValue<double>("Gas Resistance", "Ω", double.NaN);
            _dewPoint = new ObservableValue<double>("Dew Point", "°C", double.NaN);
            
            _temperature.ValueFormatter = "{0:F1}";
            _pressure.ValueFormatter = "{0:F1}";
            _humidity.ValueFormatter = "{0:F1}";
            _gasValue.ValueFormatter = "{0:F0}";
            _dewPoint.ValueFormatter = "{0:F1}";
            SensorValueSources.Add(_temperature);
            SensorValueSources.Add(_pressure);
            SensorValueSources.Add(_humidity);
            // SensorValueSources.Add(_gasValue);
            SensorValueSources.Add(_barometric);
            SensorValueSources.Add(_dewPoint);

            base.Init(gpioController);
        }

        protected override void UpdateSensors()
        {
            // Take one measurement, then sleep as long as this takes
            _bme680.SetPowerMode(Bme680PowerMode.Forced);
            var measurementTime = _bme680.GetMeasurementDuration(_bme680.HeaterProfile);
            Thread.Sleep(measurementTime);
            bool temp = _bme680.TryReadTemperature(out var tempValue);
            if (temp)
            {
                _temperature.Value = tempValue.DegreesCelsius;
            }

            bool press = _bme680.TryReadPressure(out var preValue);
            if (press)
            {
                _pressure.Value = preValue.Hectopascals;
            }

            bool hum = _bme680.TryReadHumidity(out var humidity);
            if (hum)
            {
                _humidity.Value = humidity;
            }

            ////if (_bme680.TryReadGasResistance(out var gasResistance))
            ////{
            ////    _gasValue.Value = gasResistance;
            ////}

            if (temp && press && hum)
            {
                _barometric.Value = WeatherHelper.CalculateBarometricPressure(preValue, tempValue, Altitude, humidity).Hectopascals;
                _dewPoint.Value = WeatherHelper.CalculateDewPoint(tempValue, humidity).DegreesCelsius;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopThread();
                _bme680.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
