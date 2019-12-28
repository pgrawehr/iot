using Iot.Device.Ads1115;
using System;
using System.Collections.Generic;
using System.Device.I2c;
using System.Text;

namespace DisplayControl
{
    public sealed class AdcSensors : IDisposable
    {
        Ads1115 m_cpuAdc;
        Ads1115 m_displayAdc;
        public AdcSensors()
        {
        }

        public void Init()
        {
            var cpuI2c = I2cDevice.Create(new I2cConnectionSettings(1, (int)I2cAddress.GND));
            m_cpuAdc = new Ads1115(cpuI2c, InputMultiplexer.AIN0, MeasuringRange.FS4096, DataRate.SPS128, DeviceMode.Continuous);

            var displayI2c = I2cDevice.Create(new I2cConnectionSettings(1, (int)I2cAddress.VCC));
            m_displayAdc = new Ads1115(displayI2c);
        }

        public void Dispose()
        {
            if (m_cpuAdc != null)
            {
                m_cpuAdc.DeviceMode = DeviceMode.PowerDown;
                m_cpuAdc.Dispose();
            }
            if (m_displayAdc != null)
            {
                m_displayAdc.DeviceMode = DeviceMode.PowerDown;
                m_displayAdc.Dispose();
            }
        }
    }
}
