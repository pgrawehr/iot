using Iot.Device.Ads1115;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.IO;
using System.Text;
using System.Threading;
using Iot.Device.Common;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace DisplayControl
{
    public class AdcSensorWithoutDisplay : PollingSensorBase
    {
        private Ads1115 m_cpuAdc;

        private SensorMeasurement _currentSunBrightness;
        private int _count;
        private ILogger _logger;
        private SensorMeasurement _voltage3_3V;

        public AdcSensorWithoutDisplay(MeasurementManager manager)
            : this(manager, TimeSpan.FromMilliseconds(200))
        {
        }

        public AdcSensorWithoutDisplay(MeasurementManager manager, TimeSpan pollingInterval)
        : base(manager, pollingInterval)
        {
            _count = 0;
            _logger = this.GetCurrentClassLogger();
        }

        public override void Init(GpioController gpioController)
        {
            var cpuI2c = I2cDevice.Create(new I2cConnectionSettings(1, 0x48));
            m_cpuAdc = new Ads1115(cpuI2c, InputMultiplexer.AIN0, MeasuringRange.FS4096, DataRate.SPS128, DeviceMode.PowerDown);

            _voltage3_3V = new SensorMeasurement("3.3V supply voltage", ElectricPotential.Zero, SensorSource.MainPower);
            _currentSunBrightness = new SensorMeasurement("Sunlight strength", ElectricPotential.Zero, SensorSource.Air);
            Manager.AddRange(new[]
            {
                _voltage3_3V, 
                _currentSunBrightness, 
            });

            base.Init(gpioController);
        }


        /// <summary>
        /// Use some polling for the sensor values now, since we seem to need all channels and we can only enable interrupts usefully if a 
        /// single channel is observed at once. 
        /// </summary>
        protected override void UpdateSensors()
        {
            if (_count % 10 == 0)
            {
                // Do this only every once in a while
                try
                {
                    _voltage3_3V.UpdateValue(m_cpuAdc.ReadVoltage(InputMultiplexer.AIN3));
                    // Todo: Voltage is not really the correct unit for this.
                    _currentSunBrightness.UpdateValue((m_cpuAdc.MaxVoltageFromMeasuringRange(MeasuringRange.FS4096) -
                                                       m_cpuAdc.ReadVoltage(InputMultiplexer.AIN2)));
                }
                catch (IOException x)
                {
                    _logger.LogError(x, $"Local ADC communication error: {x.Message}");
                }
            }

            _count++;
        }

        protected override void Dispose(bool disposing)
        {
            StopThread();
            if (m_cpuAdc != null)
            {
                m_cpuAdc.DeviceMode = DeviceMode.PowerDown;
                m_cpuAdc.Dispose();
            }
            
            base.Dispose(disposing);
        }
    }
}
