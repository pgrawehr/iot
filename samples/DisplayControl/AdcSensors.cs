using Iot.Device.Ads1115;
using System;
using System.Collections.Generic;
using System.Device.I2c;
using System.Text;
using System.Threading;

namespace DisplayControl
{
    public sealed class AdcSensors : IDisposable
    {
        private readonly List<SensorValueSource> _sensorValueSources;
        private Ads1115 m_cpuAdc;
        private Ads1115 m_displayAdc;

        private Thread m_pollThread;
        private CancellationTokenSource m_cancellationTokenSource;

        private ObservableValue<double> _voltage3_3V;
        private ObservableValue<double> _currentSunBrightness;

        public AdcSensors()
        {
            _sensorValueSources = new List<SensorValueSource>();
        }

        public IList<SensorValueSource> SensorValueSources => _sensorValueSources;

        public void Init()
        {
            var cpuI2c = I2cDevice.Create(new I2cConnectionSettings(1, (int)I2cAddress.GND));
            m_cpuAdc = new Ads1115(cpuI2c, InputMultiplexer.AIN0, MeasuringRange.FS4096, DataRate.SPS128, DeviceMode.Continuous);

            var displayI2c = I2cDevice.Create(new I2cConnectionSettings(1, (int)I2cAddress.VCC));
            m_displayAdc = new Ads1115(displayI2c);

            _voltage3_3V = new ObservableValue<double>("3.3V Supply Voltage", 0.0);
            _currentSunBrightness = new ObservableValue<double>("Sunlight strength", 0.0);

            _sensorValueSources.Add(_voltage3_3V);
            _sensorValueSources.Add(_currentSunBrightness);

            m_cancellationTokenSource = new CancellationTokenSource();
            m_pollThread = new Thread(PollThread);
            m_pollThread.IsBackground = true;
            m_pollThread.Start();
        }

        /// <summary>
        /// Use some polling for the sensor values now, since we seem to need all channels and we can only enable interrupts usefully if a 
        /// single channel is observed at once. 
        /// </summary>
        public void PollThread()
        {
            while (!m_cancellationTokenSource.IsCancellationRequested)
            {
                _voltage3_3V.Value = m_cpuAdc.ReadVoltage(InputMultiplexer.AIN3);
                _currentSunBrightness.Value = m_cpuAdc.ReadVoltage(InputMultiplexer.AIN0);
                m_cancellationTokenSource.Token.WaitHandle.WaitOne(100);
            }
        }

        public void Dispose()
        {
            if (m_pollThread != null)
            {
                m_cancellationTokenSource.Cancel();
                m_pollThread.Join();
                m_cancellationTokenSource.Dispose();
                m_pollThread = null;
                m_cancellationTokenSource = null;
            }
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
