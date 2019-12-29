using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Device.Gpio;
using System.Device.I2c;
using System.Globalization;
using System.Linq;
using System.Text;
using Iot.Device.CharacterLcd;

namespace DisplayControl
{
    public class DataContainer
    {
        I2cDevice m_displayDevice = null;
        ICharacterLcd m_characterLcd = null;
        LcdConsole m_lcdConsole = null;
        AdcSensors m_adcSensors = null;
        SensorValueSource m_activeValueSource;

        ObservableCollection<SensorValueSource> m_sensorValueSources;

        public DataContainer(GpioController controller)
        {
            Controller = controller;
            m_sensorValueSources = new ObservableCollection<SensorValueSource>();
            m_activeValueSource = null;
        }

        public GpioController Controller { get; }

        public ObservableCollection<SensorValueSource> SensorValueSources => m_sensorValueSources;

        public SensorValueSource ActiveValueSource
        {
            get
            {
                return m_activeValueSource;
            }
            set
            {
                m_activeValueSource = value;
            }
        }

        private void InitializeDisplay()
        {
            m_displayDevice = I2cDevice.Create(new I2cConnectionSettings(1, 0x27));
            var lcdInterface = LcdInterface.CreateI2c(m_displayDevice, false);
            m_characterLcd = new Lcd2004(lcdInterface);

            m_characterLcd.UnderlineCursorVisible = false;
            m_characterLcd.BacklightOn = true;
            m_characterLcd.DisplayOn = true;
            m_characterLcd.Clear();
            m_characterLcd.Write("== Startup ==");
            m_lcdConsole = new LcdConsole(m_characterLcd, "A02", false);
            LoadEncoding();
            m_lcdConsole.Clear();
            m_lcdConsole.Write("== Ready ==");
        }

        private void InitializeSensors()
        {
            m_adcSensors = new AdcSensors();
            m_adcSensors.Init();
            m_adcSensors.ButtonPressed += DisplayButtonPressed;
            foreach(var sensor in m_adcSensors.SensorValueSources)
            {
                sensor.PropertyChanged += OnSensorValueChanged;
                m_sensorValueSources.Add(sensor);
            }
        }

        private void DisplayButtonPressed(DisplayButton button, bool pressed)
        {
            if (pressed)
            {
                if (ActiveValueSource == null)
                {
                    ActiveValueSource = SensorValueSources.FirstOrDefault();
                }
            }
        }

        public void OnSensorValueChanged(object sender, PropertyChangedEventArgs args)
        {
            if (m_activeValueSource == sender)
            {
                DisplayValue(m_activeValueSource);
            }
        }

        public void DisplayValue(SensorValueSource valueSource)
        {
            m_lcdConsole.ReplaceLine(0, valueSource.ValueDescription);
            m_lcdConsole.ReplaceLine(1, String.Format(CultureInfo.CurrentCulture, "{0} {1}", valueSource.ValueAsString, valueSource.Unit));
        }

        public void Initialize()
        {
            InitializeDisplay();
            InitializeSensors();
        }

        /// <summary>
        /// Loads the de-CH encoding to the display (even after it has been overriden)
        /// </summary>
        public void LoadEncoding()
        {
            m_lcdConsole.LoadEncoding(LcdConsole.CreateEncoding(CultureInfo.CreateSpecificCulture("de-CH"), "A02"));
        }

        public void ShutDown()
        {
            m_lcdConsole.Clear();
            m_lcdConsole.BacklightOn = false;
            m_lcdConsole.DisplayOn = false;
            foreach(var sensor in m_sensorValueSources)
            {
                sensor.PropertyChanged -= OnSensorValueChanged;
            }
            m_adcSensors.Dispose();
            m_adcSensors = null;
            m_lcdConsole.Dispose();
            m_lcdConsole = null;
            m_characterLcd.Dispose();
            m_characterLcd = null;
            m_displayDevice.Dispose();
            m_displayDevice = null;
        }
    }
}
