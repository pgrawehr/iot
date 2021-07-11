﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using System.Device.Gpio;
using UnitsNet;

namespace System.Device.Analog
{
    /// <summary>
    /// Driver for analog input pins
    /// </summary>
    public abstract class AnalogInputPin : IDisposable
    {
        /// <summary>
        /// Fires when a value is changed for which a trigger is registered
        /// </summary>
        public event ValueChangedEventHandler? ValueChanged;

        /// <summary>
        /// Construct an instance of an analog pin.
        /// This is not usually called directly. Use <see cref="AnalogController.OpenPin(int)"/> instead.
        /// </summary>
        public AnalogInputPin(AnalogController controller, int pinNumber, ElectricPotential voltageReference)
        {
            Controller = controller;
            VoltageReference = voltageReference;
            PinNumber = pinNumber;
        }

        /// <summary>
        /// Reference to the controller for this pin
        /// </summary>
        protected AnalogController Controller
        {
            get;
        }

        /// <summary>
        /// The reference voltage level to convert raw values into voltages.
        /// Some boards (i.e. the ADS111x series) always return an absolute voltage. Then this value is meaningless.
        /// </summary>
        public ElectricPotential VoltageReference
        {
            get;
        }

        /// <summary>
        /// The logical pin number of this instance.
        /// </summary>
        public int PinNumber
        {
            get;
        }

        /// <summary>
        /// The minimum measurable voltage. May be negative.
        /// </summary>
        public abstract ElectricPotential MinVoltage
        {
            get;
        }

        /// <summary>
        /// The largest measurable voltage.
        /// </summary>
        public abstract ElectricPotential MaxVoltage
        {
            get;
        }

        /// <summary>
        /// Resolution of the Analog-to-Digital converter in bits. If the ADC supports negative values, the sign bit is also counted.
        /// </summary>
        public abstract int AdcResolutionBits
        {
            get;
        }

        /// <summary>
        /// Read a raw value from the pin
        /// </summary>
        /// <returns>Raw value of the analog input. Scale depends on hardware.</returns>
        public abstract uint ReadRaw();

        /// <summary>
        /// Read a raw value and convert it to a voltage
        /// </summary>
        /// <returns>Reads a new value from the input and converts it to a voltage. For many boards, the <see cref="VoltageReference"/> needs to be correctly set for this to work.</returns>
        public virtual ElectricPotential ReadVoltage()
        {
            uint raw = ReadRaw();
            return ConvertToVoltage(raw);
        }

        /// <summary>
        /// Converts an input raw value to a voltage
        /// </summary>
        protected virtual ElectricPotential ConvertToVoltage(uint rawValue)
        {
            if (MinVoltage >= ElectricPotential.Zero)
            {
                // The ADC can handle only positive values
                int maxRawValue = (1 << AdcResolutionBits) - 1;
                return ((double)rawValue / maxRawValue) * MaxVoltage;
            }
            else
            {
                // The ADC also handles negative values. This means that the number of bits includes the sign.
                uint maxRawValue = (uint)((1 << (AdcResolutionBits - 1)) - 1);
                if (rawValue < maxRawValue)
                {
                    return ((double)rawValue / maxRawValue) * MaxVoltage;
                }
                else
                {
                    // This is a bitmask which has all the bits 1 that are not used in the data.
                    // We use this to sign-extend our raw value. The mask also includes the sign bit itself,
                    // but we know that this is already 1
                    uint topBits = ~maxRawValue;
                    rawValue |= topBits;
                    int raw2 = (int)rawValue;
                    return ((double)raw2 / maxRawValue) * MaxVoltage; // result is negative
                }
            }
        }

        /// <summary>
        /// Enable event callback when the value of this pin changes.
        /// </summary>
        /// <param name="masterController">If an external interrupt handler is required, it can be provided here. Can be null if another interrupt feature is available</param>
        /// <param name="masterPin">Input pin on the master controller.</param>
        public abstract void EnableAnalogValueChangedEvent(GpioController? masterController, int masterPin);

        /// <summary>
        /// Disables the event callback
        /// </summary>
        public abstract void DisableAnalogValueChangedEvent();

        /// <summary>
        /// Fires a value changed event.
        /// </summary>
        /// <param name="eventArgs">New value</param>
        protected void FireValueChanged(ValueChangedEventArgs eventArgs)
        {
            ValueChanged?.Invoke(this, eventArgs);
        }

        /// <summary>
        /// Dispose this instance
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Controller.ClosePin(this);
            }
        }

        /// <summary>
        /// Dispose this instance
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
