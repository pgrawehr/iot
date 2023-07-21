using Iot.Device.Ads1115;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Text;
using System.Threading;
using Iot.Device.Arduino;
using Iot.Device.Common;
using UnitsNet;

namespace DisplayControl
{
    public sealed class DhtSensors : PollingSensorBase
    {
        private readonly DhtSensor _dhtSensorConnection;

        private SensorMeasurement _engineHumidity;
        
        public DhtSensors(MeasurementManager manager, DhtSensor dhtSensorConnection)
        : base(manager, TimeSpan.FromSeconds(5))
        {
            _dhtSensorConnection = dhtSensorConnection ?? throw new ArgumentNullException(nameof(dhtSensorConnection));
            _engineHumidity = new SensorMeasurement("Engine room humidity", RelativeHumidity.Zero, SensorSource.Engine, 1, TimeSpan.FromMinutes(2)); // Just because we have that sensor - not because it's useful
            // A DHT is very slow and often has read errors. So keep the last value longer.
            SensorMeasurement.Engine0Temperature.MaxMeasurementAge = TimeSpan.FromMinutes(2);
        }
        
        public override void Init(GpioController gpioController)
        {
            Manager.AddMeasurement(SensorMeasurement.Engine0Temperature);
            Manager.AddMeasurement(_engineHumidity);

            base.Init(gpioController);
        }

        /// <summary>
        /// Use some polling for the sensor values, as this sensor does not support interrupts.
        /// It needs a low poll rate only, though (can query at most once every 2 secs)
        /// </summary>
        protected override void UpdateSensors()
        {
            if (_dhtSensorConnection.TryReadDht(3, 11, out var temperature, out var humidity))
            {
                SensorMeasurement.Engine0Temperature.UpdateValue(temperature);
                _engineHumidity.UpdateValue(humidity);
            }
        }
    }
}
