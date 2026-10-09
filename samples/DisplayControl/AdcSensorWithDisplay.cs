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
    public sealed class AdcSensorWithDisplay : AdcSensorWithoutDisplay
    {
        private Ads1115 m_displayAdc;
        private ExtendedDisplayController _ledController;

        private SensorMeasurement _button1;
        private SensorMeasurement _button2;
        private SensorMeasurement _button3;
        private SensorMeasurement _button4;
        private HysteresisFilter _buttonEnableFilter;
        private HysteresisFilter _button1Filter;
        private HysteresisFilter _button2Filter;
        private HysteresisFilter _button3Filter;
        private HysteresisFilter _button4Filter;
        private ILogger _logger;

        public AdcSensorWithDisplay(MeasurementManager manager)
        : base(manager, TimeSpan.FromMilliseconds(200))
        {
            _logger = this.GetCurrentClassLogger();
            _buttonEnableFilter = new HysteresisFilter(false);
            _buttonEnableFilter.FallingDelayTime = TimeSpan.FromMilliseconds(500);
            _buttonEnableFilter.RisingDelayTime = TimeSpan.FromMilliseconds(500);

            _button1Filter = new HysteresisFilter(false);
            _button1Filter.RisingDelayTime = TimeSpan.FromMilliseconds(1000);
            _button2Filter = new HysteresisFilter(false);
            _button2Filter.RisingDelayTime = TimeSpan.FromMilliseconds(1000);
            _button3Filter = new HysteresisFilter(false);
            _button3Filter.RisingDelayTime = TimeSpan.FromMilliseconds(1000);
            _button4Filter = new HysteresisFilter(false);
            _button4Filter.RisingDelayTime = TimeSpan.FromMilliseconds(1000);
        }

        public event Action<DisplayButton, bool> ButtonPressed;

        public override void Init(GpioController gpioController)
        {
            throw new InvalidOperationException("Use another overload");
        }

        public bool ButtonsEnabled
        {
            get;
            set;
        }

        internal void Init(GpioController controller, ExtendedDisplayController ledController)
        {
            ButtonsEnabled = true;
            _ledController = ledController ?? throw new ArgumentNullException(nameof(ledController));
            var displayI2c = I2cDevice.Create(new I2cConnectionSettings(1, (int)I2cAddress.VCC));
            m_displayAdc = new Ads1115(displayI2c, InputMultiplexer.AIN0, MeasuringRange.FS4096, DataRate.SPS064, DeviceMode.PowerDown);

            _button1 = new SensorMeasurement("Button 1 voltage", ElectricPotential.Zero, SensorSource.UserInput);
            _button2 = new SensorMeasurement("Button 2 voltage", ElectricPotential.Zero, SensorSource.UserInput);
            _button3 = new SensorMeasurement("Button 3 voltage", ElectricPotential.Zero, SensorSource.UserInput);
            _button4 = new SensorMeasurement("Button 4 voltage", ElectricPotential.Zero, SensorSource.UserInput);
            _ledController.WriteLed(ExtendedDisplayController.PinUsage.KeyPadLeds, PinValue.Low);

            Manager.AddRange(new[]
            {
                _button1, 
                _button2, 
                _button3, 
                _button4, 
            });

            base.Init(controller);
        }


        /// <summary>
        /// Use some polling for the sensor values now, since we seem to need all channels and we can only enable interrupts usefully if a 
        /// single channel is observed at once. 
        /// </summary>
        protected override void UpdateSensors()
        {
            base.UpdateSensors();

            if (!ButtonsEnabled)
            {
                // If the buttons are locked, show the status LED as red and do nothing more.
                _ledController.WriteLed(ExtendedDisplayController.PinUsage.Led5Green, PinValue.Low);
                _ledController.WriteLed(ExtendedDisplayController.PinUsage.Led5Red, PinValue.High);
                return;
            }

            _ledController.WriteLed(ExtendedDisplayController.PinUsage.Led5Red, PinValue.Low);

            try
            {
                _ledController.WriteLed(ExtendedDisplayController.PinUsage.KeyPadLeds, PinValue.Low);
                // First, read all four inputs with the led off (default)
                double b1Low = RetryableReadAdc(InputMultiplexer.AIN0);
                double b2Low = RetryableReadAdc(InputMultiplexer.AIN1);
                double b3Low = RetryableReadAdc(InputMultiplexer.AIN2);
                double b4Low = RetryableReadAdc(InputMultiplexer.AIN3);
                _ledController.WriteLed(ExtendedDisplayController.PinUsage.KeyPadLeds, PinValue.High);
                // Then read them again, now with the reflection led on
                double b1High = RetryableReadAdc(InputMultiplexer.AIN0);
                double b2High = RetryableReadAdc(InputMultiplexer.AIN1);
                double b3High = RetryableReadAdc(InputMultiplexer.AIN2);
                double b4High = RetryableReadAdc(InputMultiplexer.AIN3);
                _ledController.WriteLed(ExtendedDisplayController.PinUsage.KeyPadLeds, PinValue.Low);
                double averageLow = (b1Low + b2Low + b3Low + b4Low) / 4;

                // Calculates the average of the three smaller values
                double averageHigh = (b1High + b2High + b3High + b4High) / 4;
                bool sunIsShining = averageLow > 0.9 || averageHigh > 0.4;
                _buttonEnableFilter.Update(sunIsShining);
                // Find out which button might be pressed (obstructed)
                // if the low average is high the LED might not have an effect at all. 
                // if the low average equals the high average, the led has no effect (or is broken)
                if (!_buttonEnableFilter.Output)
                {
                    // The "normal" button mode: buttons are pressed when the voltage rises
                    _button1.UpdateValue(ElectricPotential.FromVolts(b1High));
                    _button2.UpdateValue(ElectricPotential.FromVolts(b2High));
                    _button3.UpdateValue(ElectricPotential.FromVolts(b3High));
                    _button4.UpdateValue(ElectricPotential.FromVolts(b4High));

                    _ledController.WriteLed(ExtendedDisplayController.PinUsage.Led5Green, PinValue.High);

                    // Individual trigger limits for the buttons, a kind of calibration
                    double averageLimit = 0.35;
                    if (b1High > 0.36 && averageHigh < averageLimit)
                    {
                        if (_button1Filter.Update(true))
                        {
                            SendButtonPressedIfOk(DisplayButton.Back);
                            _button1Filter.Update(false);
                        }
                    }
                    else if (b2High > 0.37 && averageHigh < averageLimit)
                    {
                        if (_button2Filter.Update(true))
                        {
                            SendButtonPressedIfOk(DisplayButton.Previous);
                            _button2Filter.Update(false);
                        }
                    }
                    else if (b3High > 0.35 && averageHigh < averageLimit)
                    {
                        if (_button3Filter.Update(true))
                        {
                            SendButtonPressedIfOk(DisplayButton.Next);
                            _button3Filter.Update(false);
                        }
                    }
                    else if (b4High > 0.42 && averageHigh < averageLimit)
                    {
                        if (_button4Filter.Update(true))
                        {
                            SendButtonPressedIfOk(DisplayButton.Enter);
                            _button4Filter.Update(false);
                        }
                    }
                    else
                    {
                        _button1Filter.Update(false);
                        _button2Filter.Update(false);
                        _button3Filter.Update(false);
                        _button4Filter.Update(false);
                    }
                }
                else
                {
                    // the environmental light is so high, that the LED has no effect and the input
                    // is more or less saturated regardless of whether buttons are pressed or not
                    _button1.UpdateValue(ElectricPotential.FromVolts(-b1Low)); // Negative, so we can distinguish the case
                    _button2.UpdateValue(ElectricPotential.FromVolts(-b2Low));
                    _button3.UpdateValue(ElectricPotential.FromVolts(-b3Low));
                    _button4.UpdateValue(ElectricPotential.FromVolts(-b4Low));
                    // Disable the green led 5, meaning the display is locked.
                    _ledController.WriteLed(ExtendedDisplayController.PinUsage.Led5Green, PinValue.Low);
                    /* This intends to do the opposite from the normal behavior: If all sensors get a lot of light,
                     the one that gets the least is probably obstructed.
                     Disabled - not reliable enough (causes many random button presses)
                    const double lowThreshold = 0.5;
                    double bt1Delta = b1Low - (b2Low + b3Low + b4Low) / 3;
                    double bt2Delta = b2Low - (b1Low + b3Low + b4Low) / 3;
                    double bt3Delta = b3Low - (b1Low + b2Low + b4Low) / 3;
                    double bt4Delta = b4Low - (b1Low + b2Low + b3Low) / 3;
                    // All the deltas are negative, if relevant
                    // Only the maximum (the one with the largest difference) is relevant
                    double maxDelta = Math.Min(Math.Min(bt1Delta, bt2Delta), Math.Min(bt3Delta, bt4Delta));
                    if (Math.Abs(bt1Delta) > lowThreshold && Math.Abs(maxDelta - bt1Delta) < 1E-10)
                    {
                        SendButtonPressedIfOk(DisplayButton.Back);
                    }
                    else if (Math.Abs(bt2Delta) > lowThreshold && Math.Abs(maxDelta - bt2Delta) < 1E-10)
                    {
                        SendButtonPressedIfOk(DisplayButton.Previous);
                    }
                    else if (Math.Abs(bt3Delta) > lowThreshold && Math.Abs(maxDelta - bt3Delta) < 1E-10)
                    {
                        SendButtonPressedIfOk(DisplayButton.Next);
                    }
                    else if (Math.Abs(bt4Delta) > lowThreshold && Math.Abs(maxDelta - bt4Delta) < 1E-10)
                    {
                        SendButtonPressedIfOk(DisplayButton.Enter);
                    }
                    else
                    {
                        // Reset once no buttons are pressed any more
                    }
                    */
                }
            }
            catch (Exception x) when (x is IOException || x is TimeoutException)
            {
                _logger.LogError(x, $"Remote ADC communication error: {x.Message}. Remote display disconnected?");
            }
        }

        private double RetryableReadAdc(InputMultiplexer mpx)
        {
            int retries = 3;
            while(true)
            {
                try
                {
                    double result = m_displayAdc.ReadVoltage(mpx).Volts;
                    return result;
                }
                catch(IOException)
                {
                    if (retries <= 0)
                    {
                        throw;
                    }
                }

                retries--;
            }
        }

        private void SendButtonPressedIfOk(DisplayButton button)
        {
            ExtendedDisplayController.PinUsage usage = ExtendedDisplayController.PinUsage.Led1Green;
            switch (button)
            {
                case DisplayButton.Back:
                    usage = ExtendedDisplayController.PinUsage.Led1Green;
                    break;
                case DisplayButton.Previous:
                    usage = ExtendedDisplayController.PinUsage.Led2Green;
                    break;
                case DisplayButton.Next:
                    usage = ExtendedDisplayController.PinUsage.Led3Green;
                    break;
                case DisplayButton.Enter:
                    usage = ExtendedDisplayController.PinUsage.Led4Green;
                    break;
            }

            _ledController.WriteLed(usage, PinValue.High);
            Thread.Sleep(300);
            _ledController.WriteLed(usage, PinValue.Low);

            ButtonPressed?.Invoke(button, true);
        }

        protected override void Dispose(bool disposing)
        {
            StopThread();
            if (m_displayAdc != null)
            {
                m_displayAdc.DeviceMode = DeviceMode.PowerDown;
                m_displayAdc.Dispose();
            }
            
            base.Dispose(disposing);
        }
    }
}
