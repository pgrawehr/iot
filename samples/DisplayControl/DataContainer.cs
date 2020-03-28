using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Device.Gpio;
using System.Device.I2c;
using System.Diagnostics;
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
        private ExtendedDisplayController _extendedDisplayController;
        private bool m_lcdConsoleActive;
        private SensorValueSource m_activeValueSourceUpper;
        private SensorValueSource m_activeValueSourceLower;
        private SensorValueSource m_activeValueSourceSingle;
        private List<SensorValueSource> m_sensorsWithErrors;

        private List<SensorValueSource> m_sensorValueSources;
        private DhtSensors m_dhtSensors;
        private SystemSensors m_systemSensors;
        private PressureSensor m_pressureSensor;
        private ImuSensor _imuSensor;
        private NmeaSensor _nmeaSensor;
        private CultureInfo m_activeEncoding;
        private LcdValueUnitDisplay m_bigValueDisplay;
        private Stopwatch m_timer;

        public DataContainer(GpioController controller)
        {
            Controller = controller;
            _extendedDisplayController = null;
            m_sensorValueSources = new List<SensorValueSource>();
            m_sensorsWithErrors = new List<SensorValueSource>();
            m_activeValueSourceUpper = null;
            m_activeValueSourceLower = null;
            m_activeValueSourceSingle = null;
            m_lcdConsoleActive = true;
            m_bigValueDisplay = null;
            m_timer = new Stopwatch();
            m_activeEncoding = CultureInfo.CreateSpecificCulture("de-CH");
        }

        public GpioController Controller { get; }

        public List<SensorValueSource> SensorValueSources => m_sensorValueSources;

        public SensorValueSource ActiveValueSourceUpper
        {
            get
            {
                return m_activeValueSourceUpper;
            }
            set
            {
                if (value != m_activeValueSourceUpper)
                {
                    m_activeValueSourceUpper = value;
                    m_activeValueSourceSingle = null;
                    // Immediately show the new value
                    OnSensorValueChanged(value, null);
                }
            }
        }

        public SensorValueSource ActiveValueSourceLower
        {
            get
            {
                return m_activeValueSourceLower;
            }
            set
            {
                if (value != m_activeValueSourceLower)
                {
                    m_activeValueSourceLower = value;
                    m_activeValueSourceSingle = null;
                    // Immediately show the new value
                    OnSensorValueChanged(value, null);
                }
            }
        }

        public SensorValueSource ActiveValueSourceSingle
        {
            get
            {
                return m_activeValueSourceSingle;
            }
            set
            {
                if (value != m_activeValueSourceSingle)
                {
                    m_activeValueSourceSingle = value;
                    m_activeValueSourceUpper = null;
                    m_activeValueSourceLower = null;
                    m_timer.Restart();
                    // Immediately show the new value
                    OnSensorValueChanged(value, null);
                }
            }
        }

        private void NewSensorValueSource(SensorValueSource newValue)
        {
            if (m_lcdConsoleActive)
            {
                m_lcdConsole.Clear();
            }
            else
            {
                m_bigValueDisplay.Clear();
            }

            OnSensorValueChanged(newValue, null);
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
            m_lcdConsole = new LcdConsole(m_characterLcd, "A00", false);
            LoadEncoding();
            m_lcdConsole.Clear();
            m_lcdConsole.Write("== Ready ==");
            m_lcdConsoleActive = true;
            m_bigValueDisplay = new LcdValueUnitDisplay(m_characterLcd, m_activeEncoding);
        }

        private void InitializeSensors()
        {
            m_adcSensors = new AdcSensors();
            m_adcSensors.Init();
            m_adcSensors.ButtonPressed += DisplayButtonPressed;

            List<SensorValueSource> allSources = new List<SensorValueSource>();
            allSources.AddRange(m_adcSensors.SensorValueSources);

            m_dhtSensors = new DhtSensors();
            m_dhtSensors.Init(Controller);

            allSources.AddRange(m_dhtSensors.SensorValueSources);

            m_systemSensors = new SystemSensors();
            m_systemSensors.Init(Controller);
            allSources.AddRange(m_systemSensors.SensorValueSources);

            m_pressureSensor = new PressureSensor();
            m_pressureSensor.Init(Controller);
            allSources.AddRange(m_pressureSensor.SensorValueSources);

            _imuSensor = new ImuSensor();
            _imuSensor.Init(Controller);
            allSources.AddRange(_imuSensor.SensorValueSources);

            _nmeaSensor = new NmeaSensor();
            _nmeaSensor.Initialize();
            allSources.AddRange(_nmeaSensor.SensorValueSources);

            foreach (var sensor in allSources)
            {
                sensor.PropertyChanged += OnSensorValueChanged;
                m_sensorValueSources.Add(sensor);
            }

            _extendedDisplayController = new ExtendedDisplayController(Controller);
        }

        private void DisplayButtonPressed(DisplayButton button, bool pressed)
        {
            if (pressed == false)
            {
                // React only on press events
                return;
            }

            var sourceToChange = ActiveValueSourceSingle;
            if (sourceToChange == null)
            {
                sourceToChange = ActiveValueSourceLower;
            }
            if (button == DisplayButton.Next)
            {
                if (sourceToChange == null)
                {
                    sourceToChange = SensorValueSources.FirstOrDefault();
                }
                else
                {
                    int idx = SensorValueSources.IndexOf(sourceToChange);
                    if (idx < 0)
                    {
                        idx = 0;
                    }
                    idx = (idx + 1) % SensorValueSources.Count;
                    sourceToChange = SensorValueSources[idx];
                }
            }
            if (button == DisplayButton.Previous)
            {
                if (sourceToChange == null)
                {
                    sourceToChange = SensorValueSources.FirstOrDefault();
                }
                else
                {
                    int idx = SensorValueSources.IndexOf(sourceToChange);
                    idx = (idx - 1);
                    if (idx < 0)
                    {
                        idx = SensorValueSources.Count - 1;
                    }
                    sourceToChange = SensorValueSources[idx];
                }
            }

            if (ActiveValueSourceSingle != null)
            {
                ActiveValueSourceSingle = sourceToChange;
                m_timer.Restart();
            }
            else
            {
                ActiveValueSourceLower = sourceToChange;
            }
        }

        public void OnSensorValueChanged(object sender, PropertyChangedEventArgs args)
        {
            CheckForTriggers(sender as SensorValueSource);
        
            if (m_activeValueSourceUpper == sender || m_activeValueSourceLower == sender)
            {
                if (!m_lcdConsoleActive)
                {
                    LoadEncoding();
                    m_lcdConsole.Clear();
                    m_lcdConsoleActive = true;
                }
                Display2Values(m_activeValueSourceUpper, m_activeValueSourceLower);
                return;
            }
            else if (m_activeValueSourceSingle == sender)
            {
                if (m_lcdConsoleActive)
                {
                    m_bigValueDisplay.InitForRom("A00");
                    m_bigValueDisplay.Clear();
                    m_lcdConsoleActive = false;
                }

                DisplayBigValue(m_activeValueSourceSingle);
            }
        }

        /// <summary>
        /// Checks for any trigger/error conditions
        /// </summary>
        /// <param name="source">The value that has last changed</param>
        private void CheckForTriggers(SensorValueSource source)
        {
            if (source == null)
            {
                return;
            }

            lock (m_sensorsWithErrors)
            {
                if (source.WarningLevel != WarningLevel.None)
                {
                    if (!m_sensorsWithErrors.Contains(source) && !source.SuppressWarnings)
                    {
                        m_sensorsWithErrors.Add(source);
                        _extendedDisplayController.SoundAlarm(true);
                        ActiveValueSourceSingle = source;
                    }
                }
                else if (m_sensorsWithErrors.Contains(source))
                {
                    m_sensorsWithErrors.RemoveAll(x => x == source);
                    if (m_sensorsWithErrors.Count == 0)
                    {
                        _extendedDisplayController.SoundAlarm(false);
                    }
                }
            }
        }

        public void DisplayBigValue(SensorValueSource valueSource)
        {
            if (valueSource == null)
            {
                m_bigValueDisplay.DisplayValue("N/A");
                return;
            }
            string text = valueSource.ValueAsString + " " + valueSource.Unit;
            if (m_timer.Elapsed < TimeSpan.FromSeconds(3))
            {
                // Display the value description for 3 seconds after changing
                m_bigValueDisplay.DisplayValue(text, valueSource.ValueDescription);
            }
            else
            {
                m_bigValueDisplay.DisplayValue(text);
            }
        }

        public void Display2Values(SensorValueSource valueSourceUpper, SensorValueSource valueSourceLower)
        {
            if (valueSourceUpper == null)
            {
                valueSourceUpper = new ObservableValue<string>(string.Empty, string.Empty, string.Empty);
            }
            m_lcdConsole.ReplaceLine(0, valueSourceUpper.ValueDescription);
            string text = valueSourceUpper.ValueAsString;
            if (text.Contains("\n"))
            {
                m_lcdConsole.SetCursorPosition(0, 1);
                m_lcdConsole.Write(text);
            }
            else
            {
                m_lcdConsole.ReplaceLine(1, String.Format(CultureInfo.CurrentCulture, "{0} {1}", text, valueSourceUpper.Unit));
                // Only if the first entry is a 1-liner
                if (valueSourceLower != null)
                {
                    m_lcdConsole.ReplaceLine(2, valueSourceLower.ValueDescription);
                    text = valueSourceLower.ValueAsString;
                    m_lcdConsole.ReplaceLine(3, String.Format(CultureInfo.CurrentCulture, "{0} {1}", text, valueSourceLower.Unit));
                }
            }
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
            m_lcdConsole.LoadEncoding(LcdConsole.CreateEncoding(m_activeEncoding, "A00"));
        }

        public void ShutDown()
        {
            m_lcdConsole.Clear();
            m_lcdConsole.BacklightOn = false;
            m_lcdConsole.DisplayOn = false;
            m_lcdConsole.LineFeedMode = LineWrapMode.WordWrap;
            foreach(var sensor in m_sensorValueSources)
            {
                sensor.PropertyChanged -= OnSensorValueChanged;
            }
            m_adcSensors.Dispose();
            m_adcSensors = null;
            m_dhtSensors.Dispose();
            m_dhtSensors = null;

            _nmeaSensor.Dispose();
            _nmeaSensor = null;

            m_pressureSensor.Dispose();
            m_pressureSensor = null;

            _imuSensor.Dispose();
            _imuSensor = null;

            _extendedDisplayController.Dispose();
            _extendedDisplayController = null;

            m_lcdConsole.Dispose();
            m_lcdConsole = null;
            m_characterLcd.Dispose();
            m_characterLcd = null;
            m_displayDevice.Dispose();
            m_displayDevice = null;
        }
    }
}
