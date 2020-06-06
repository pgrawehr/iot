using Iot.Device.Ads1115;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.IO;
using System.Text;
using System.Threading;

namespace DisplayControl
{
    public sealed class AdcSensors : IDisposable
    {
        private readonly List<SensorValueSource> _sensorValueSources;
        private Ads1115 m_cpuAdc;
        private Ads1115 m_displayAdc;
        private GpioController _ledController;
        private int _ledPin;

        private Thread m_pollThread;
        private CancellationTokenSource m_cancellationTokenSource;

        private ObservableValue<double> _voltage3_3V;
        private ObservableValue<double> _currentSunBrightness;
        private VoltageWithLimits _button1;
        private VoltageWithLimits _button2;
        private VoltageWithLimits _button3;
        private VoltageWithLimits _button4;
        private VoltageWithLimits _voltage5V;
        private bool _atLeastOneButtonPressed = false;

        public AdcSensors()
        {
            _sensorValueSources = new List<SensorValueSource>();
        }

        public event Action<DisplayButton, bool> ButtonPressed;

        public IList<SensorValueSource> SensorValueSources => _sensorValueSources;

        public void Init(GpioController ledController, int ledPin)
        {
            _ledController = ledController ?? throw new ArgumentNullException(nameof(ledController));
            _ledPin = ledPin;
            var cpuI2c = I2cDevice.Create(new I2cConnectionSettings(1, (int)I2cAddress.GND));
            m_cpuAdc = new Ads1115(cpuI2c, InputMultiplexer.AIN0, MeasuringRange.FS4096, DataRate.SPS128, DeviceMode.PowerDown);

            var displayI2c = I2cDevice.Create(new I2cConnectionSettings(1, (int)I2cAddress.VCC));
            m_displayAdc = new Ads1115(displayI2c, InputMultiplexer.AIN0, MeasuringRange.FS4096, DataRate.SPS064, DeviceMode.PowerDown);

            _voltage3_3V = new VoltageWithLimits("3.3V Supply Voltage", 3.2, 3.4);
            _currentSunBrightness = new ObservableValue<double>("Sunlight strength", "V", 0.0);
            _voltage5V = new VoltageWithLimits("5V Supply Voltage", 4.8, 5.2);
            _button1 = new VoltageWithLimits("Button 1 voltage", -0.1, 2.55);
            _button1.SuppressWarnings = true;
            _button2 = new VoltageWithLimits("Button 2 voltage", -0.1, 2.7);
            _button2.SuppressWarnings = true;
            _button3 = new VoltageWithLimits("Button 3 voltage", -0.1, 2.7);
            _button3.SuppressWarnings = true;
            _button4 = new VoltageWithLimits("Button 4 voltage", -0.1, 2.7);
            _button4.SuppressWarnings = true;
            // The led controller has the pin already opened, so only set the mode
            _ledController.SetPinMode(_ledPin, PinMode.Output);
            _ledController.Write(_ledPin, PinValue.Low);

            _sensorValueSources.Add(_voltage3_3V);
            _sensorValueSources.Add(_currentSunBrightness);
            _sensorValueSources.Add(_voltage5V);
            _sensorValueSources.Add(_button1);
            _sensorValueSources.Add(_button2);
            _sensorValueSources.Add(_button3);
            _sensorValueSources.Add(_button4);

            m_cancellationTokenSource = new CancellationTokenSource();
            m_pollThread = new Thread(PollThread);
            m_pollThread.Name = "AdcSensors";
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
                if (_button1.WarningLevel == WarningLevel.Warning) // Button pressed
                {
                    cb.Invoke(DisplayButton.Back, true);
                }
                else
                {
                    cb.Invoke(DisplayButton.Back, false);
                }
            }
            if (sender == _button2)
            {
                if (_button2.WarningLevel == WarningLevel.Warning) // Button pressed
                {
                    cb.Invoke(DisplayButton.Previous, true);
                }
                else
                {
                    cb.Invoke(DisplayButton.Previous, false);
                }
            }
            if (sender == _button3)
            {
                if (_button3.WarningLevel == WarningLevel.Warning) // Button pressed
                {
                    cb.Invoke(DisplayButton.Next, true);
                }
                else
                {
                    cb.Invoke(DisplayButton.Next, false);
                }
            }
            if (sender == _button4)
            {
                if (_button4.WarningLevel == WarningLevel.Warning) // Button pressed
                {
                    cb.Invoke(DisplayButton.Enter, true);
                }
                else
                {
                    cb.Invoke(DisplayButton.Enter, false);
                }
            }
        }

        /// <summary>
        /// Use some polling for the sensor values now, since we seem to need all channels and we can only enable interrupts usefully if a 
        /// single channel is observed at once. 
        /// </summary>
        public void PollThread()
        {
            int count = 0;
            while (!m_cancellationTokenSource.IsCancellationRequested)
            {
                if (count % 10 == 0)
                {
                    // Do this only every once in a while
                    try
                    {
                        _voltage3_3V.Value = m_cpuAdc.ReadVoltage(InputMultiplexer.AIN3);
                        // Todo: Voltage is not really the correct unit for this.
                        _currentSunBrightness.Value = m_cpuAdc.MaxVoltageFromMeasuringRange(MeasuringRange.FS4096) -
                                                      m_cpuAdc.ReadVoltage(InputMultiplexer.AIN2);
                    }
                    catch (IOException x)
                    {
                        Console.WriteLine($"Local ADC communication error: {x.Message}");
                    }
                }

                try
                {
                    // First, read all four inputs with the led off (default)
                    double b1Low = m_displayAdc.ReadVoltage(InputMultiplexer.AIN0);
                    double b2Low = m_displayAdc.ReadVoltage(InputMultiplexer.AIN1);
                    double b3Low = m_displayAdc.ReadVoltage(InputMultiplexer.AIN2);
                    double b4Low = m_displayAdc.ReadVoltage(InputMultiplexer.AIN3);
                    _ledController.Write(_ledPin, PinValue.High);
                    // Then read them again, now with the reflection led on
                    double b1High = m_displayAdc.ReadVoltage(InputMultiplexer.AIN0);
                    double b2High = m_displayAdc.ReadVoltage(InputMultiplexer.AIN1);
                    double b3High = m_displayAdc.ReadVoltage(InputMultiplexer.AIN2);
                    double b4High = m_displayAdc.ReadVoltage(InputMultiplexer.AIN3);
                    _ledController.Write(_ledPin, PinValue.Low);
                    double averageLow = (b1Low + b2Low + b3Low + b4Low) / 4;
                    double averageHigh = (b1High + b2High + b3High + b4High) / 4;
                    bool sunIsShining = averageLow > 1.5;
                    // Todo: find out which button might be pressed (obstructed)
                    // if the low average is high the LED might not have an effect at all. 
                    // if the low average equals the high average, the led has no effect (or is broken)
                    if (!sunIsShining)
                    {
                        _button1.Value = b1High;
                        _button2.Value = b2High;
                        _button3.Value = b3High;
                        _button4.Value = b4High;

                        // The "normal" button mode: buttons are pressed when the voltage rises
                        double triggerValue = averageHigh + 0.3;
                        if (b1High > triggerValue)
                        {
                            SendButtonPressedIfOk(DisplayButton.Back);
                        }
                        else if (b2High > triggerValue)
                        {
                            SendButtonPressedIfOk(DisplayButton.Previous);
                        }
                        else if (b3High > triggerValue)
                        {
                            SendButtonPressedIfOk(DisplayButton.Next);
                        }
                        else if (b4High > triggerValue)
                        {
                            SendButtonPressedIfOk(DisplayButton.Enter);
                        }
                        else
                        {
                            // Reset once no buttons are pressed any more
                            _atLeastOneButtonPressed = false;
                        }
                    }
                    else
                    {
                        // the environmental light is so high, that the LED has no effect and the input
                        // is more or less saturated regardless of whether buttons are pressed or not
                        _button1.Value = -b1Low; // Negative, so we can distinguish the case
                        _button2.Value = -b2Low;
                        _button3.Value = -b3Low;
                        _button4.Value = -b4Low;
                        const double lowThreshold = 0.5;
                        if (b1Low < lowThreshold && Math.Abs(b1Low - averageLow) > 0.3)
                        {
                            SendButtonPressedIfOk(DisplayButton.Back);
                        }
                        else if (b2Low < lowThreshold && Math.Abs(b2Low - averageLow) > 0.3)
                        {
                            SendButtonPressedIfOk(DisplayButton.Previous);
                        }
                        else if (b3Low < lowThreshold && Math.Abs(b3Low - averageLow) > 0.3)
                        {
                            SendButtonPressedIfOk(DisplayButton.Next);
                        }
                        else if (b4Low < lowThreshold && Math.Abs(b4Low - averageLow) > 0.3)
                        {
                            SendButtonPressedIfOk(DisplayButton.Enter);
                        }
                        else
                        {
                            // Reset once no buttons are pressed any more
                            _atLeastOneButtonPressed = false;
                        }
                    }
                }
                catch (IOException x)
                {
                    Console.WriteLine($"Remote ADC communication error: {x.Message}");
                }

                m_cancellationTokenSource.Token.WaitHandle.WaitOne(200);
                count++;
            }
        }

        private void SendButtonPressedIfOk(DisplayButton button)
        {
            if (_atLeastOneButtonPressed)
            {
                // Already one button pressed. Wait until released
                return;
            }

            ButtonPressed?.Invoke(button, true);
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
