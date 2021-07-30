using Iot.Device.Ads1115;
using Iot.Device.DHTxx;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Text;
using System.Threading;
using Iot.Device.Common;
using UnitsNet;

namespace DisplayControl
{
    public sealed class DhtSensors : PollingSensorBase
    {
        private Dht11 _dht11;

        private SensorMeasurement _engineHumidity;
        
        public DhtSensors(MeasurementManager manager)
        : base(manager, TimeSpan.FromSeconds(5))
        {
            _engineHumidity = new SensorMeasurement("Engine room humidity", RelativeHumidity.Zero, SensorSource.Engine); // Just because we have that sensor - not because it's useful
        }
        
        public override void Init(GpioController gpioController)
        {
            Manager.AddMeasurement(SensorMeasurement.Engine0Temperature);
            Manager.AddMeasurement(_engineHumidity);

            _dht11 = new Dht11(16, PinNumberingScheme.Logical, gpioController, false);

            base.Init(gpioController);
        }

        /// <summary>
        /// Use some polling for the sensor values, as this sensor does not support interrupts.
        /// It needs a low poll rate only, though (can query at most once every 2 secs)
        /// </summary>
        protected override void UpdateSensors()
        {
            if (_dht11.TryReadTemperatureAndHumidity(out var temp, out var humidity))
            {
                SensorMeasurement.Engine0Temperature.UpdateValue(temp);
                _engineHumidity.UpdateValue(_dht11.Humidity);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_dht11 != null)
            {
                _dht11.Dispose();
                _dht11 = null;
            }

            base.Dispose(disposing);
        }
    }
}
