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
        private VoltageWithLimits _button1;
        private VoltageWithLimits _button2;

        public AdcSensors()
        {
            _sensorValueSources = new List<SensorValueSource>();
        }

        public event Action<DisplayButton, bool> ButtonPressed;

        public IList<SensorValueSource> SensorValueSources => _sensorValueSources;

        public void Init()
        {
            var cpuI2c = I2cDevice.Create(new I2cConnectionSettings(1, (int)I2cAddress.GND));
            m_cpuAdc = new Ads1115(cpuI2c, InputMultiplexer.AIN0, MeasuringRange.FS4096, DataRate.SPS128, DeviceMode.PowerDown);

            var displayI2c = I2cDevice.Create(new I2cConnectionSettings(1, (int)I2cAddress.VCC));
            m_displayAdc = new Ads1115(displayI2c, InputMultiplexer.AIN0, MeasuringRange.FS4096, DataRate.SPS064, DeviceMode.PowerDown);

            _voltage3_3V = new VoltageWithLimits("3.3V Supply Voltage", 3.2, 3.4);
            _currentSunBrightness = new ObservableValue<double>("Sunlight strength", "V", 0.0);
            _button1 = new VoltageWithLimits("Button 1 voltage", 0, 2.0);
            _button1.LimitTriggered += LimitTriggered;
            _button2 = new VoltageWithLimits("Button 2 voltage", 0, 2.0);
            _button2.LimitTriggered += LimitTriggered;

            _sensorValueSources.Add(_voltage3_3V);
            _sensorValueSources.Add(_currentSunBrightness);
            _sensorValueSources.Add(_button1);
            _sensorValueSources.Add(_button2);

            m_cancellationTokenSource = new CancellationTokenSource();
            m_pollThread = new Thread(PollThread);
            m_pollThread.IsBackground = true;
            m_pollThread.Start();
        }

        private void LimitTriggered(object sender, EventArgs e)
        {
            var cb = ButtonPressed;
            if (cb == null)
            {
                // No callbacks registered -> Nothing to do
                return;
            }
            if (sender == _button1)
            {
                if (_button1.WarningLevel == WarningLevel.Error) // Button pressed
                {
                    cb.Invoke(DisplayButton.Previous, true);
                }
                else
                {
                    cb.Invoke(DisplayButton.Previous, false);
                }
            }
            if (sender == _button2)
            {
                if (_button2.WarningLevel == WarningLevel.Error) // Button pressed
                {
                    cb.Invoke(DisplayButton.Next, true);
                }
                else
                {
                    cb.Invoke(DisplayButton.Next, false);
                }
            }
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
                // Todo: Voltage is not really the correct unit for this.
                _currentSunBrightness.Value = m_cpuAdc.MaxVoltageFromMeasuringRange(MeasuringRange.FS4096) - m_cpuAdc.ReadVoltage(InputMultiplexer.AIN0);

                _button1.Value = m_displayAdc.ReadVoltage(InputMultiplexer.AIN0);
                _button2.Value = m_displayAdc.ReadVoltage(InputMultiplexer.AIN1);
                m_cancellationTokenSource.Token.WaitHandle.WaitOne(500);
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
