﻿using Iot.Device.Ads1115;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.I2c;
using System.IO;
using System.Text;
using System.Threading;
using Iot.Device.Common;
using UnitsNet;

namespace DisplayControl
{
    public sealed class AdcSensors : PollingSensorBase
    {
        private Ads1115 m_cpuAdc;
        private Ads1115 m_displayAdc;
        private ExtendedDisplayController _ledController;

        private SensorMeasurement _voltage3_3V;

        private SensorMeasurement _currentSunBrightness;
        private SensorMeasurement _button1;
        private SensorMeasurement _button2;
        private SensorMeasurement _button3;
        private SensorMeasurement _button4;
        private bool _atLeastOneButtonPressed = false;
        private int _count;

        public AdcSensors(MeasurementManager manager)
        : base(manager, TimeSpan.FromMilliseconds(200))
        {
            _count = 0;
        }

        public event Action<DisplayButton, bool> ButtonPressed;

        public override void Init(GpioController gpioController)
        {
            throw new InvalidOperationException("Use another overload");
        }

        internal void Init(GpioController controller, ExtendedDisplayController ledController)
        {
            _ledController = ledController ?? throw new ArgumentNullException(nameof(ledController));
            var cpuI2c = I2cDevice.Create(new I2cConnectionSettings(1, (int)I2cAddress.GND));
            m_cpuAdc = new Ads1115(cpuI2c, InputMultiplexer.AIN0, MeasuringRange.FS4096, DataRate.SPS128, DeviceMode.PowerDown);

            var displayI2c = I2cDevice.Create(new I2cConnectionSettings(1, (int)I2cAddress.VCC));
            m_displayAdc = new Ads1115(displayI2c, InputMultiplexer.AIN0, MeasuringRange.FS4096, DataRate.SPS064, DeviceMode.PowerDown);

            _voltage3_3V = new SensorMeasurement("3.3V supply voltage", ElectricPotential.Zero, SensorSource.MainPower);
            _currentSunBrightness = new SensorMeasurement("Sunlight strength", ElectricPotential.Zero, SensorSource.Air);
            _button1 = new SensorMeasurement("Button 1 voltage", ElectricPotential.Zero, SensorSource.UserInput);
            _button2 = new SensorMeasurement("Button 2 voltage", ElectricPotential.Zero, SensorSource.UserInput);
            _button3 = new SensorMeasurement("Button 3 voltage", ElectricPotential.Zero, SensorSource.UserInput);
            _button4 = new SensorMeasurement("Button 4 voltage", ElectricPotential.Zero, SensorSource.UserInput);
            _ledController.WriteLed(ExtendedDisplayController.PinUsage.KeyPadLeds, PinValue.Low);

            Manager.AddRange(new[] { _voltage3_3V, _currentSunBrightness, _button1, _button2, _button3, _button4 });

            base.Init(controller);
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
                    Console.WriteLine($"Local ADC communication error: {x.Message}");
                }
            }

            try
            {
                // First, read all four inputs with the led off (default)
                double b1Low = m_displayAdc.ReadVoltage(InputMultiplexer.AIN0).Volts;
                double b2Low = m_displayAdc.ReadVoltage(InputMultiplexer.AIN1).Volts;
                double b3Low = m_displayAdc.ReadVoltage(InputMultiplexer.AIN2).Volts;
                double b4Low = m_displayAdc.ReadVoltage(InputMultiplexer.AIN3).Volts;
                _ledController.WriteLed(ExtendedDisplayController.PinUsage.KeyPadLeds, PinValue.High);
                // Then read them again, now with the reflection led on
                double b1High = m_displayAdc.ReadVoltage(InputMultiplexer.AIN0).Volts;
                double b2High = m_displayAdc.ReadVoltage(InputMultiplexer.AIN1).Volts;
                double b3High = m_displayAdc.ReadVoltage(InputMultiplexer.AIN2).Volts;
                double b4High = m_displayAdc.ReadVoltage(InputMultiplexer.AIN3).Volts;
                _ledController.WriteLed(ExtendedDisplayController.PinUsage.KeyPadLeds, PinValue.Low);
                double averageLow = (b1Low + b2Low + b3Low + b4Low) / 4;
                double averageHigh = (b1High + b2High + b3High + b4High) / 4;
                bool sunIsShining = averageLow > 0.9;
                // Todo: find out which button might be pressed (obstructed)
                // if the low average is high the LED might not have an effect at all. 
                // if the low average equals the high average, the led has no effect (or is broken)
                if (!sunIsShining)
                {
                    _button1.UpdateValue(ElectricPotential.FromVolts(b1High));
                    _button2.UpdateValue(ElectricPotential.FromVolts(b2High));
                    _button3.UpdateValue(ElectricPotential.FromVolts(b3High));
                    _button4.UpdateValue(ElectricPotential.FromVolts(b4High));

                    _ledController.WriteLed(ExtendedDisplayController.PinUsage.Led5Green, PinValue.High);
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
                    _button1.UpdateValue(
                        ElectricPotential.FromVolts(-b1Low)); // Negative, so we can distinguish the case
                    _button2.UpdateValue(ElectricPotential.FromVolts(-b2Low));
                    _button3.UpdateValue(ElectricPotential.FromVolts(-b3Low));
                    _button4.UpdateValue(ElectricPotential.FromVolts(-b4Low));
                    // Disable the green led 5, meaning the display is locked.
                    _ledController.WriteLed(ExtendedDisplayController.PinUsage.Led5Green, PinValue.Low);
                    /* Disabled - not reliable enough (causes many random button presses)
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
                        _atLeastOneButtonPressed = false;
                    }
                    */
                }
            }
            catch (IOException x)
            {
                Console.WriteLine($"Remote ADC communication error: {x.Message}");
            }

            _count++;

        }

        private void SendButtonPressedIfOk(DisplayButton button)
        {
            if (_atLeastOneButtonPressed)
            {
                // Already one button pressed. Wait until released
                return;
            }

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
            
            base.Dispose(disposing);
        }
    }
}