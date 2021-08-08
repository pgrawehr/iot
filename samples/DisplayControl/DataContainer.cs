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
        I2cDevice _displayDevice = null;
        ICharacterLcd _characterLcd = null;
        LcdConsole _lcdConsole = null;
        AdcSensors _adcSensors = null;
        private SensorFusionEngine _fusionEngine;
        private ExtendedDisplayController _extendedDisplayController;
        private bool _lcdConsoleActive;
        private SensorMeasurement _activeValueSourceUpper;
        private SensorMeasurement _activeValueSourceLower;
        private SensorMeasurement _activeValueSourceSingle;
        private List<SensorMeasurement> _sensorsWithErrors;

        private MeasurementManager _sensorManager;
        private DhtSensors _dhtSensors;
        private SystemSensors _systemSensors;
        private Bmp280Environment _pressureSensor;
        private ImuSensor _imuSensor;
        private NmeaSensor _nmeaSensor;
        private CultureInfo _activeEncoding;
        private LcdValueUnitDisplay _bigValueDisplay;
        private Stopwatch _timer;
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
            _sensorsWithErrors = new List<SensorMeasurement>();
            _activeValueSourceUpper = null;
            _activeValueSourceLower = null;
            _activeValueSourceSingle = null;
            _lcdConsoleActive = true;
            _bigValueDisplay = null;
            _timer = new Stopwatch();
            _activeEncoding = CultureInfo.CreateSpecificCulture("de-CH");
            _menuMode = false;
        }

        public GpioController Controller { get; }

        public List<SensorMeasurement> SensorValueSources => _sensorManager.Measurements();

        public SensorMeasurement ActiveValueSourceUpper
        {
            get
            {
                return _activeValueSourceUpper;
            }
            set
            {
                if (value != _activeValueSourceUpper)
                {
                    _activeValueSourceUpper = value;
                    _activeValueSourceSingle = null;
                    LeaveMenu();
                    // Immediately show the new value
                    OnSensorValueChanged(value);
                }
            }
        }

        public SensorMeasurement ActiveValueSourceLower
        {
            get
            {
                return _activeValueSourceLower;
            }
            set
            {
                if (value != _activeValueSourceLower)
                {
                    _activeValueSourceLower = value;
                    _activeValueSourceSingle = null;
                    LeaveMenu();
                    // Immediately show the new value
                    OnSensorValueChanged(value);
                }
            }
        }

        public SensorMeasurement ActiveValueSourceSingle
        {
            get
            {
                return _activeValueSourceSingle;
            }
            set
            {
                if (value != _activeValueSourceSingle)
                {
                    _activeValueSourceSingle = value;
                    _activeValueSourceUpper = null;
                    _activeValueSourceLower = null;
                    LeaveMenu();
                    _timer.Restart();
                    // Immediately show the new value
                    OnSensorValueChanged(value);
                }
            }
        }

        private void InitializeDisplay()
        {
            _displayDevice = I2cDevice.Create(new I2cConnectionSettings(1, 0x27));
            var lcdInterface = LcdInterface.CreateI2c(_displayDevice, false);
            _characterLcd = new Lcd2004(lcdInterface);

            _characterLcd.UnderlineCursorVisible = false;
            _characterLcd.BacklightOn = true;
            _characterLcd.DisplayOn = true;
            _characterLcd.Clear();
            _lcdConsole = new LcdConsole(_characterLcd, "A00", false);
            LoadEncoding();
            _lcdConsole.LineFeedMode = LineWrapMode.WordWrap;
            _lcdConsole.Clear();
            _lcdConsole.WriteLine("== Startup ==");
            _lcdConsoleActive = true;
            _menuController = new MenuController(this);
            _menuMode = false;
            _bigValueDisplay = new LcdValueUnitDisplay(_characterLcd, _activeEncoding);
        }

        private void InitializeSensors()
        {
            Thread.CurrentThread.Name = "Main Thread";
            WriteLineToConsoleAndDisplay("CPU...");
            _systemSensors = new SystemSensors(_sensorManager);
            _systemSensors.Init(Controller);
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
                _pressureSensor = new Bmp280Environment(_sensorManager);
                _pressureSensor.Init(Controller);
                WriteLineToConsoleAndDisplay("Remote display connected and ready");
            }
            catch (IOException x)
            {
                WriteLineToConsoleAndDisplay("Remote display not connected: " + x.Message);
            }

            WriteLineToConsoleAndDisplay("DHT...");
            _dhtSensors = new DhtSensors(_sensorManager);
            _dhtSensors.Init(Controller);

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
            if (_lcdConsoleActive)
            {
                _lcdConsole?.WriteLine(text);
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
                Dispatcher.UIThread.Post(() => DisplayButtonPressed(button, pressed));
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
                    LeaveMenu();
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

            if (button == DisplayButton.Enter && _lcdConsoleActive && _activeValueSourceLower != null)
            {
                SwitchToBigMode(_activeValueSourceLower);
            }

            if (ActiveValueSourceSingle != null)
            {
                ActiveValueSourceSingle = sourceToChange;
                _timer.Restart();
            }
            else
            {
                ActiveValueSourceLower = sourceToChange;
            }
        }

        private void LeaveMenu()
        {
            _menuMode = false;
            // Make sure the display is blank
            SwitchToConsoleMode();
            _lcdConsole.Clear();
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
            ////if (!Dispatcher.UIThread.CheckAccess())
            ////{
            ////    Dispatcher.UIThread.Post(() => OnSensorValueChanged(newMeasurement));
            ////    return;
            ////}

            CheckForTriggers(newMeasurement);

            if (_menuMode)
            {
                return;
            }

            if (_activeValueSourceUpper == newMeasurement || _activeValueSourceLower == newMeasurement)
            {
                SwitchToConsoleMode();
                Display2Values(_activeValueSourceUpper, _activeValueSourceLower);
                return;
            }
            else if (_activeValueSourceSingle == newMeasurement)
            {
                SwitchToBigMode(_activeValueSourceSingle);
                DisplayBigValue(_activeValueSourceSingle);
            }
        }

        private void SwitchToBigMode(SensorMeasurement withValue)
        {
            _activeValueSourceUpper = _activeValueSourceLower = null;
            if (_activeValueSourceSingle != withValue)
            {
                _activeValueSourceSingle = withValue;
                _timer.Restart();
            }
            
            if (_lcdConsoleActive)
            {
                _bigValueDisplay.InitForRom("A00");
                _bigValueDisplay.Clear();
                _lcdConsoleActive = false;
            }
        }

        private void SwitchToConsoleMode()
        {
            if (!_lcdConsoleActive)
            {
                LoadEncoding();
                _lcdConsole.Clear();
                _lcdConsoleActive = true;
            }
        }

        private void SwitchToConsoleMode(SensorMeasurement upper, SensorMeasurement lower)
        {
            _activeValueSourceSingle = null;
            _activeValueSourceUpper = upper;
            _activeValueSourceLower = lower;
            if (!_lcdConsoleActive)
            {
                LoadEncoding();
                _lcdConsole.Clear();
                _lcdConsoleActive = true;
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

            lock (_sensorsWithErrors)
            {
                if (source.Status.HasFlag(SensorMeasurementStatus.Warning))
                {
                    if (!_sensorsWithErrors.Contains(source))
                    {
                        _sensorsWithErrors.Add(source);
                        _extendedDisplayController.SoundAlarm(true);
                        ActiveValueSourceSingle = source;
                    }
                }
                else if (_sensorsWithErrors.Contains(source))
                {
                    _sensorsWithErrors.RemoveAll(x => x == source);
                    if (_sensorsWithErrors.Count == 0)
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

            if (source == SensorMeasurement.AirHumidityOutside)
            {
                if (source.TryGetAs(out RelativeHumidity humidity))
                {
                    _nmeaSensor.SendHumidity(humidity);
                }
            }

        }

        public void DisplayBigValue(SensorMeasurement valueSource)
        {
            if (valueSource == null)
            {
                _bigValueDisplay.DisplayValue("No data");
                return;
            }

            string text = valueSource.ToString();
            if (string.IsNullOrWhiteSpace(text))
            {
                text = "No data";
            }
            else
            {
                text += valueSource.Value?.ToString("a", CultureInfo.CurrentCulture);
            }

            if (_timer.Elapsed < TimeSpan.FromSeconds(3))
            {
                // Display the value description for 3 seconds after changing
                _bigValueDisplay.DisplayValue(text, valueSource.Name);
            }
            else
            {
                _bigValueDisplay.DisplayValue(text);
            }
        }

        public void Display2Values(SensorMeasurement valueSourceUpper, SensorMeasurement valueSourceLower)
        {
            if (valueSourceUpper == null)
            {
                valueSourceUpper = SensorMeasurement.CpuTemperature;
            }

            _lcdConsole.ReplaceLine(0, valueSourceUpper.Name ?? string.Empty);
            string text = valueSourceUpper.ToString();
            if (text.Contains("\n"))
            {
                _lcdConsole.SetCursorPosition(0, 1);
                _lcdConsole.Write(text);
            }
            else
            {
                _lcdConsole.ReplaceLine(1, String.Format(CultureInfo.CurrentCulture, "{0} {1}", text, valueSourceUpper.Unit));
                // Only if the first entry is a 1-liner
                if (valueSourceLower != null)
                {
                    _lcdConsole.ReplaceLine(2, valueSourceLower.Name);
                    text = valueSourceLower.ToString();
                    if (text.Contains("\n"))
                    {
                        // We can't display a two liner here
                        text = text.Replace("\n", " ", StringComparison.OrdinalIgnoreCase);
                    }

                    _lcdConsole.ReplaceLine(3, String.Format(CultureInfo.CurrentCulture, "{0} {1}", text, valueSourceLower.Unit));
                }
            }
        }

        public void Initialize()
        {
            InitializeDisplay();
            InitializeSensors();
            _lcdConsole.Clear();
            _lcdConsole.ReplaceLine(0, "Startup successful");
        }

        /// <summary>
        /// Loads the de-CH encoding to the display (even after it has been overriden)
        /// </summary>
        public void LoadEncoding()
        {
            _lcdConsole.LoadEncoding(LcdConsole.CreateEncoding(_activeEncoding, "A00"));
        }

        public void ShutDown()
        {
            _lcdConsole.Clear();
            _lcdConsole.BacklightOn = false;
            _lcdConsole.DisplayOn = false;
            _lcdConsole.LineFeedMode = LineWrapMode.WordWrap;
            _sensorManager.AnyMeasurementChanged -= OnSensorValueChanged;

            _adcSensors.Dispose();
            _adcSensors = null;
            _dhtSensors?.Dispose();
            _dhtSensors = null;

            _pressureSensor?.Dispose();
            _pressureSensor = null;

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

            _lcdConsole.Dispose();
            _lcdConsole = null;
            _characterLcd.Dispose();
            _characterLcd = null;
            _displayDevice.Dispose();
            _displayDevice = null;

            _sensorManager.Dispose();
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
                _console = control._lcdConsole;
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
