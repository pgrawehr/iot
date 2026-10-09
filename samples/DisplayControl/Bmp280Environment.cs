using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Text;
using System.Threading;
using Iot.Device.Bmxx80;
using Iot.Device.Bmxx80.PowerMode;
using Iot.Device.Common;
using UnitsNet.Units;

namespace DisplayControl
{
    public class Bmp280Environment : PollingSensorBase
    {
        private Bmp280 _bmp280;

        public Bmp280Environment(MeasurementManager manager) 
            : base(manager, TimeSpan.FromSeconds(5))
        {
        }

        public override void Init(GpioController gpioController)
        {
            _bmp280 = new Bmp280(I2cDevice.Create(new I2cConnectionSettings(1, Bmp280.DefaultI2cAddress)));
            _bmp280.TemperatureSampling = Sampling.Standard;
            
            _bmp280.PressureSampling = Sampling.UltraHighResolution;
            Manager.AddMeasurement(SensorMeasurement.AirPressureRawOutside);
            Manager.AddMeasurement(SensorMeasurement.AirTemperatureOutside);
            
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
                SensorMeasurement.AirTemperatureOutside.UpdateValue(tempValue);
            }

            if (_bmp280.TryReadPressure(out var preValue))
            {
                SensorMeasurement.AirPressureRawOutside.UpdateValue(preValue.ToUnit(PressureUnit.Hectopascal));
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
