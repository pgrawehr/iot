using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Device.Gpio;
using System.Device.I2c;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Threading;
using Iot.Device.CharacterLcd;
using Iot.Device.Nmea0183.Sentences;
using Iot.Device.Common;
using UnitsNet;

namespace DisplayControl
{
    public class DataContainer
    {
        I2cDevice m_displayDevice = null;
        ICharacterLcd m_characterLcd = null;
        LcdConsole m_lcdConsole = null;
        AdcSensors _adcSensors = null;
        private SensorFusionEngine _fusionEngine;
        private ExtendedDisplayController _extendedDisplayController;
        private bool m_lcdConsoleActive;
        private SensorMeasurement m_activeValueSourceUpper;
        private SensorMeasurement m_activeValueSourceLower;
        private SensorMeasurement m_activeValueSourceSingle;
        private List<SensorMeasurement> m_sensorsWithErrors;

        private MeasurementManager _sensorManager;
        private DhtSensors m_dhtSensors;
        private SystemSensors m_systemSensors;
        private Bmp280Environment m_pressureSensor;
        private ImuSensor _imuSensor;
        private NmeaSensor _nmeaSensor;
        private CultureInfo m_activeEncoding;
        private LcdValueUnitDisplay m_bigValueDisplay;
        private Stopwatch m_timer;
        private int _numberOfImuSentencesSent = 0;
        private bool _menuMode;
        private MenuController _menuController;
        private Bmp680Environment _weatherSensor;
        private EngineSurveillance _engine;
        private PersistenceFile _configFile;

        private enum MenuOptionResult
        {
            None,
            Exit,
            SubMenu,
            TextUpdate
        }

        public DataContainer(GpioController controller)
        {
            Controller = controller;
            _extendedDisplayController = null;
            _sensorManager = new MeasurementManager();
            _fusionEngine = new SensorFusionEngine(_sensorManager);
            m_sensorsWithErrors = new List<SensorMeasurement>();
            m_activeValueSourceUpper = null;
            m_activeValueSourceLower = null;
            m_activeValueSourceSingle = null;
            m_lcdConsoleActive = true;
            m_bigValueDisplay = null;
            m_timer = new Stopwatch();
            m_activeEncoding = CultureInfo.CreateSpecificCulture("de-CH");
            _menuMode = false;
        }

        public GpioController Controller { get; }

        public List<SensorMeasurement> SensorValueSources => _sensorManager.Measurements();

        public SensorMeasurement ActiveValueSourceUpper
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
                    OnSensorValueChanged(value);
                }
            }
        }

        public SensorMeasurement ActiveValueSourceLower
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
                    OnSensorValueChanged(value);
                }
            }
        }

        public SensorMeasurement ActiveValueSourceSingle
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
                    OnSensorValueChanged(value);
                }
            }
        }

        private void NewSensorValueSource(SensorMeasurement newValue)
        {
            if (m_lcdConsoleActive)
            {
                m_lcdConsole.Clear();
            }
            else
            {
                m_bigValueDisplay.Clear();
            }

            OnSensorValueChanged(newValue);
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
            m_lcdConsole = new LcdConsole(m_characterLcd, "A00", false);
            LoadEncoding();
            m_lcdConsole.LineFeedMode = LineWrapMode.WordWrap;
            m_lcdConsole.Clear();
            m_lcdConsole.WriteLine("== Startup ==");
            m_lcdConsoleActive = true;
            _menuController = new MenuController(this);
            _menuMode = false;
            m_bigValueDisplay = new LcdValueUnitDisplay(m_characterLcd, m_activeEncoding);
        }

        private void InitializeSensors()
        {
            Thread.CurrentThread.Name = "Main Thread";
            WriteLineToConsoleAndDisplay("CPU...");
            m_systemSensors = new SystemSensors(_sensorManager);
            m_systemSensors.Init(Controller);
            _configFile = new PersistenceFile("/home/pi/projects/ShipLogs/NavigationConfig.txt");

            WriteLineToConsoleAndDisplay("Display controller...");
            try
            {
                var extendedDisplayController = new ExtendedDisplayController();
                extendedDisplayController.Init(Controller);
                extendedDisplayController.SelfTest();
                _extendedDisplayController = extendedDisplayController;

                WriteLineToConsoleAndDisplay("ADC...");
                _adcSensors = new AdcSensors(_sensorManager);
                _adcSensors.Init(Controller, extendedDisplayController);
                _adcSensors.ButtonPressed += DisplayButtonPressed;

                WriteLineToConsoleAndDisplay("Cockpit Environment...");
                m_pressureSensor = new Bmp280Environment(_sensorManager);
                m_pressureSensor.Init(Controller);
                WriteLineToConsoleAndDisplay("Remote display connected and ready");
            }
            catch (IOException x)
            {
                WriteLineToConsoleAndDisplay("Remote display not connected: " + x.Message);
            }

            //WriteLineToConsoleAndDisplay("DHT...");
            //m_dhtSensors = new DhtSensors();
            //m_dhtSensors.Init(Controller);

            //allSources.AddRange(m_dhtSensors.SensorValueSources);

            WriteLineToConsoleAndDisplay("Wetter...");
            _weatherSensor = new Bmp680Environment(_sensorManager);
            _weatherSensor.Init(Controller);

            WriteLineToConsoleAndDisplay("IMU...");
            _imuSensor = new ImuSensor(_sensorManager, _configFile);
            _imuSensor.Init(Controller);
            _imuSensor.OnNewOrientation += ImuSensorOnNewOrientation;

            WriteLineToConsoleAndDisplay("NMEA Source...");
            _nmeaSensor = new NmeaSensor(_sensorManager);
            _nmeaSensor.Initialize(_fusionEngine);

            WriteLineToConsoleAndDisplay("Motor...");
            _engine = new EngineSurveillance(_sensorManager, 9);
            _engine.Init(Controller);
            _engine.DataChanged += NewEngineData;

            _sensorManager.AnyMeasurementChanged += OnSensorValueChanged;
            _fusionEngine.LoadPredefinedOperations();

            foreach (var m in _sensorManager.Measurements())
            {
                m.CustomFormat = "{1:N2}"; // No unit
            }

            WriteLineToConsoleAndDisplay($"Found {_sensorManager.Measurements().Count} sensors.");
        }

        private void NewEngineData(EngineData data)
        {
            _nmeaSensor?.SendEngineData(data);
        }


        /// <summary>
        /// Writes a line to the console and the display (for logging)
        /// </summary>
        private void WriteLineToConsoleAndDisplay(string text)
        {
            Console.WriteLine(text);
            if (m_lcdConsoleActive)
            {
                m_lcdConsole?.WriteLine(text);
            }
        }

        private void ImuSensorOnNewOrientation(Vector3 orientation)
        {
            if (_nmeaSensor != null)
            {
                // Send only every fourth message (we currently get the data at 25Hz, this is a bit much)
                if (_numberOfImuSentencesSent % 4 == 0)
                {
                    _nmeaSensor.SendImuData(orientation);
                }

                _numberOfImuSentencesSent++;
            }
        }

        private void DisplayButtonPressed(DisplayButton button, bool pressed)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.InvokeAsync(() => DisplayButtonPressed(button, pressed));
                return;
            }
            if (pressed == false)
            {
                // React only on press events
                return;
            }

            if (_menuMode)
            {
                if (_menuController.ButtonPressed(button) == false)
                {
                    // The user has left the menu
                    _menuMode = false;
                    // Make sure the display is blank
                    SwitchToConsoleMode();
                    m_lcdConsole.Clear();
                    if (ActiveValueSourceSingle != null)
                    {
                        OnSensorValueChanged(ActiveValueSourceSingle);
                    }
                    else
                    {
                        OnSensorValueChanged(ActiveValueSourceUpper);
                        OnSensorValueChanged(ActiveValueSourceLower);
                    }
                }
                return;
            }

            if (button == DisplayButton.Back)
            {
                _menuMode = true;
                SwitchToConsoleMode();
                _menuController.ShowMainMenu();
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

                // _extendedDisplayController.IncreaseBrightness(10);
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

                // _extendedDisplayController.DecreaseBrightness(10);
            }

            if (button == DisplayButton.Enter && m_lcdConsoleActive && m_activeValueSourceLower != null)
            {
                SwitchToBigMode(m_activeValueSourceLower);
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

        private void OnSensorValueChanged(IList<SensorMeasurement> newMeasurements)
        {
            foreach (var m in newMeasurements)
            {
                OnSensorValueChanged(m);
            }
        }

        private void OnSensorValueChanged(SensorMeasurement newMeasurement)
        {
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.InvokeAsync(() => OnSensorValueChanged(newMeasurement));
                return;
            }

            CheckForTriggers(newMeasurement);

            if (_menuMode)
            {
                return;
            }

            if (m_activeValueSourceUpper == newMeasurement || m_activeValueSourceLower == newMeasurement)
            {
                SwitchToConsoleMode();
                Display2Values(m_activeValueSourceUpper, m_activeValueSourceLower);
                return;
            }
            else if (m_activeValueSourceSingle == newMeasurement)
            {
                SwitchToBigMode(m_activeValueSourceSingle);
                DisplayBigValue(m_activeValueSourceSingle);
            }
        }

        private void SwitchToBigMode(SensorMeasurement withValue)
        {
            m_activeValueSourceUpper = m_activeValueSourceLower = null;
            m_activeValueSourceSingle = withValue;
            if (m_lcdConsoleActive)
            {
                m_bigValueDisplay.InitForRom("A00");
                m_bigValueDisplay.Clear();
                m_lcdConsoleActive = false;
            }
        }

        private void SwitchToConsoleMode()
        {
            if (!m_lcdConsoleActive)
            {
                LoadEncoding();
                m_lcdConsole.Clear();
                m_lcdConsoleActive = true;
            }
        }

        private void SwitchToConsoleMode(SensorMeasurement upper, SensorMeasurement lower)
        {
            m_activeValueSourceSingle = null;
            m_activeValueSourceUpper = upper;
            m_activeValueSourceLower = lower;
            if (!m_lcdConsoleActive)
            {
                LoadEncoding();
                m_lcdConsole.Clear();
                m_lcdConsoleActive = true;
            }
        }

        /// <summary>
        /// Checks for any trigger/error conditions
        /// </summary>
        /// <param name="source">The value that has last changed</param>
        private void CheckForTriggers(SensorMeasurement source)
        {
            if (source == null)
            {
                return;
            }

            lock (m_sensorsWithErrors)
            {
                if (source.Status.HasFlag(SensorMeasurementStatus.Warning))
                {
                    if (!m_sensorsWithErrors.Contains(source))
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

            if (source == SensorMeasurement.AltitudeGeoid)
            {
                if (source.TryGetAs(out Length len))
                {
                    _weatherSensor.Altitude = len;
                }
            }

            if (source == SensorMeasurement.AirTemperatureOutside)
            {
                if (source.TryGetAs(out Temperature temp))
                {
                    _nmeaSensor.SendTemperature(temp);
                }
            }

            if (source == SensorMeasurement.AirPressureBarometricOutside)
            {
                if (source.TryGetAs(out Pressure p))
                {
                    _nmeaSensor.SendPressure(p);
                }
            }

            if (source == SensorMeasurement.AirHumidityInside)
            {
                if (source.TryGetAs(out Ratio humidity))
                {
                    _nmeaSensor.SendHumidity(humidity);
                }
            }

        }

        public void DisplayBigValue(SensorMeasurement valueSource)
        {
            if (valueSource == null)
            {
                m_bigValueDisplay.DisplayValue("N/A");
                return;
            }
            string text = valueSource.ToString() + valueSource.Value.ToString("a", CultureInfo.CurrentCulture);
            if (m_timer.Elapsed < TimeSpan.FromSeconds(3))
            {
                // Display the value description for 3 seconds after changing
                m_bigValueDisplay.DisplayValue(text, valueSource.Name);
            }
            else
            {
                m_bigValueDisplay.DisplayValue(text);
            }
        }

        public void Display2Values(SensorMeasurement valueSourceUpper, SensorMeasurement valueSourceLower)
        {
            if (valueSourceUpper == null)
            {
                valueSourceUpper = SensorMeasurement.CpuTemperature;
            }

            m_lcdConsole.ReplaceLine(0, valueSourceUpper.Name ?? string.Empty);
            string text = valueSourceUpper.ToString();
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
                    m_lcdConsole.ReplaceLine(2, valueSourceLower.Name);
                    text = valueSourceLower.ToString();
                    if (text.Contains("\n"))
                    {
                        // We can't display a two liner here
                        text = text.Replace("\n", " ", StringComparison.OrdinalIgnoreCase);
                    }

                    m_lcdConsole.ReplaceLine(3, String.Format(CultureInfo.CurrentCulture, "{0} {1}", text, valueSourceLower.Unit));
                }
            }
        }

        public void Initialize()
        {
            InitializeDisplay();
            InitializeSensors();
            m_lcdConsole.Clear();
            m_lcdConsole.ReplaceLine(0, "Startup successful");
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
            _sensorManager.AnyMeasurementChanged -= OnSensorValueChanged;

            _adcSensors.Dispose();
            _adcSensors = null;
            m_dhtSensors?.Dispose();
            m_dhtSensors = null;

            m_pressureSensor?.Dispose();
            m_pressureSensor = null;

            _weatherSensor?.Dispose();
            _weatherSensor = null;

            _imuSensor.Dispose();
            _imuSensor = null;

            _nmeaSensor.Dispose();
            _nmeaSensor = null;

            _engine?.Dispose();
            _engine = null;

            _extendedDisplayController.Dispose();
            _extendedDisplayController = null;

            m_lcdConsole.Dispose();
            m_lcdConsole = null;
            m_characterLcd.Dispose();
            m_characterLcd = null;
            m_displayDevice.Dispose();
            m_displayDevice = null;
        }

        private sealed class MenuController
        {
            private readonly DataContainer _control;
            private readonly LcdConsole _console;
            private readonly List<(string, SensorMeasurement, Func<SensorMeasurement, MenuOptionResult>)> _options;

            private string _title;
            private int _activeItem;
            private bool _subMenuActive;

            public MenuController(DataContainer control)
            {
                _control = control;
                _console = control.m_lcdConsole;
                _title = string.Empty;
                _subMenuActive = false;
                _activeItem = 0;
                _options = new List<(string, SensorMeasurement, Func<SensorMeasurement, MenuOptionResult>)>();
            }

            public bool ButtonPressed(DisplayButton button)
            {
                switch (button)
                {
                    case DisplayButton.Next:
                        _activeItem = (_activeItem + 1) % _options.Count;
                        DrawMenuOptions();
                        break;
                    case DisplayButton.Previous:
                        if (_activeItem == 0)
                        {
                            _activeItem = _options.Count - 1;
                        }
                        else
                        {
                            _activeItem = (_activeItem - 1);
                        }
                        DrawMenuOptions();
                        break;
                    case DisplayButton.Back:
                        if (_subMenuActive)
                        {
                            _subMenuActive = false;
                            ShowMainMenu();
                            return true;
                        }
                        return false;
                    case DisplayButton.Enter:
                    {
                        var action = _options[_activeItem].Item3;
                        MenuOptionResult mr = action.Invoke(_options[_activeItem].Item2);
                        if (mr == MenuOptionResult.Exit)
                        {
                            _subMenuActive = false;
                            return false;
                        }
                        else if (mr == MenuOptionResult.TextUpdate)
                        {
                            DrawMenuOptions();
                        }
                        else if (mr == MenuOptionResult.SubMenu)
                        {
                            _subMenuActive = true;
                            _activeItem = 0;
                            DrawMenuOptions();
                        }
                        break;
                    }
                }

                return true;
            }

            private void SetTitle(string title)
            {
                _title = "== " + title + " ==";
                _console.ReplaceLine(0, _title);
            }

            public void ShowMainMenu()
            {
                _console.Clear();
                _options.Clear();
                SetTitle("Main Menu");
                AddOption("Upper value...", null, x => ShowValueSelection(true));
                AddOption("Lower value...", null, x => ShowValueSelection(false));
                AddOption("Big value...", null, x => ShowBigValueSelection());
                AddOption("Display Brightness", null, x => ShowBrightnessMenu());
                AddOption("Silence alarm", null, x => SilenceAlarm());
                _activeItem = 0;
                DrawMenuOptions();
            }

            private void DrawMenuOptions()
            {
                var previousOption = _activeItem == 0 ? (null, null, null) : _options[_activeItem - 1];
                var currentOption = _options[_activeItem];
                var nextOption = _activeItem == _options.Count - 1 ? (null, null, null) : _options[_activeItem + 1];
                if (previousOption.Item1 == null)
                {
                    _console.ReplaceLine(1, string.Empty);
                }
                else
                {
                    _console.ReplaceLine(1, "  " + previousOption.Item1);
                }

                _console.ReplaceLine(2, "> " + currentOption.Item1);

                if (nextOption.Item1 == null)
                {
                    _console.ReplaceLine(3, string.Empty);
                }
                else
                {
                    _console.ReplaceLine(3, "  " + nextOption.Item1);
                }

            }

            private MenuOptionResult ShowValueSelection(bool upper)
            {
                if (upper)
                {
                    SetTitle("Select upper value");
                    _options.Clear();
                    var sources = _control.SensorValueSources;
                    foreach(var source in sources)
                    {
                        AddOption(source.Name, source, (s) =>
                        {
                            _control.SwitchToConsoleMode(s, _control.ActiveValueSourceLower);
                            return MenuOptionResult.Exit;
                        });
                    }
                }
                else
                {
                    SetTitle("Select lower value");
                    _options.Clear();
                    var sources = _control.SensorValueSources;
                    foreach (var source in sources)
                    {
                        AddOption(source.Name, source, (s) =>
                        {
                            _control.SwitchToConsoleMode(_control.ActiveValueSourceUpper, s);
                            return MenuOptionResult.Exit;
                        });
                    }
                }

                return MenuOptionResult.SubMenu;
            }

            private MenuOptionResult ShowBigValueSelection()
            {
                SetTitle("Select big value:");
                _options.Clear();
                var sources = _control.SensorValueSources;
                foreach (var source in sources)
                {
                    AddOption(source.Name, source, (s) =>
                    {
                        _control.SwitchToBigMode(source);
                        return MenuOptionResult.Exit;
                    });
                }

                return MenuOptionResult.SubMenu;
            }

            private MenuOptionResult ShowBrightnessMenu()
            {
                SetTitle("Display Backlight");
                _options.Clear();
                AddOption("Increase Brightness", null, x =>
                {
                    _control._extendedDisplayController.IncreaseBrightness(10);
                    return MenuOptionResult.TextUpdate;
                });

                AddOption("Decrease Brightness", null, x =>
                {
                    _control._extendedDisplayController.DecreaseBrightness(10);
                    return MenuOptionResult.TextUpdate;
                });

                return MenuOptionResult.SubMenu;
            }

            private MenuOptionResult SilenceAlarm()
            {
                _control._extendedDisplayController.SoundAlarm(false);
                return MenuOptionResult.TextUpdate;
            }

            private void AddOption(string subMenuName, SensorMeasurement source, Func<SensorMeasurement, MenuOptionResult> operation)
            {
                _options.Add((subMenuName, source, operation));
            }
        }
    }
}
