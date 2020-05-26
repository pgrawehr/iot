using Iot.Device.Ads1115;
using Iot.Device.DHTxx;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.Text;
using System.Threading;
using Iot.Device.CpuTemperature;

namespace DisplayControl
{
    public sealed class SystemSensors : PollingSensorBase
    {
        private CpuTemperature _cpuTemperature;

        private ObservableValue<double> _cpuTemperatureValue;

        public SystemSensors() 
            : base(TimeSpan.FromSeconds(2))
        {
        }
        
        public override void Init(GpioController gpioController)
        {
            _cpuTemperatureValue = new ObservableValue<double>("CPU temperature", "°C", double.NaN);
            // This sensor can deliver an accuracy of 0.1° at most
            _cpuTemperatureValue.ValueFormatter = "{0:F1}";
            SensorValueSources.Add(_cpuTemperatureValue);
            
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
                _cpuTemperatureValue.Value = _cpuTemperature.Temperature.DegreesCelsius;
            }
        }

    }
}
