using Iot.Device.Ads1115;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Text;
using System.Threading;
using Iot.Device.Common;
using Iot.Device.CpuTemperature;

namespace DisplayControl
{
    public sealed class SystemSensors : PollingSensorBase
    {
        private CpuTemperature _cpuTemperature;

        public SystemSensors(MeasurementManager manager)
            : base(manager, TimeSpan.FromSeconds(2))
        {
        }
        
        public override void Init(GpioController gpioController)
        {
            Manager.AddMeasurement(SensorMeasurement.CpuTemperature);
            // This sensor can deliver an accuracy of 0.1° at most
            // _cpuTemperatureValue.ValueFormatter = "{0:F1}";
            // SensorValueSources.Add(_cpuTemperatureValue);
            
            _cpuTemperature = new CpuTemperature();

            base.Init(gpioController);
        }

        /// <summary>
        /// Use some polling for the sensor values, as this sensor does not support interrupts. 
        /// It needs a low poll rate only
        /// </summary>
        protected override void UpdateSensors()
        {
            if (_cpuTemperature.IsAvailable)
            {
                SensorMeasurement.CpuTemperature.UpdateValue(_cpuTemperature.Temperature);
            }
        }

    }
}
