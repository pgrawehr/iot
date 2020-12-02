// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Device.Gpio
{
    /// <summary>
    /// Platform-dependent description of an alternate pin mode (i.e. a pin that is used for I2C or SPI transfer).
    /// Depending on the hardware in use, the use will reflect the effective mode ("PWM", "SPI") or the state of the
    /// mode multiplexer of the CPU ("ALT0", "ALT1").
    /// </summary>
    public class AlternatePinMode : IEquatable<AlternatePinMode>
    {
        /// <summary>
        /// Default Gpio mode
        /// </summary>
        public static AlternatePinMode Gpio = new AlternatePinMode("GPIO", 0);

        /// <summary>
        /// The mode is not known
        /// </summary>
        public static AlternatePinMode Unknown = new AlternatePinMode("Unknown", 0xffffffff);

        /// <summary>
        /// Alternate mode 0 for Raspberry Pi
        /// </summary>
        public static AlternatePinMode Alt0 = new AlternatePinMode("Alt0", 0b100);

        /// <summary>
        /// Alternate mode 1 for Raspberry Pi
        /// </summary>
        public static AlternatePinMode Alt1 = new AlternatePinMode("Alt1", 0b101);

        /// <summary>
        /// Alternate mode 2 for Raspberry Pi
        /// </summary>
        public static AlternatePinMode Alt2 = new AlternatePinMode("Alt2", 0b110);

        /// <summary>
        /// Alternate mode 3 for Raspberry Pi
        /// </summary>
        public static AlternatePinMode Alt3 = new AlternatePinMode("Alt3", 0b111);

        /// <summary>
        /// Alternate mode 4 for Raspberry Pi
        /// </summary>
        public static AlternatePinMode Alt4 = new AlternatePinMode("Alt4", 0b011);

        /// <summary>
        /// Alternate mode 5 for Raspberry Pi
        /// </summary>
        public static AlternatePinMode Alt5 = new AlternatePinMode("Alt5", 0b010);

        /// <summary>
        /// Creates a new mode.
        /// </summary>
        /// <param name="modeName">Common name of the mode</param>
        /// <param name="modeValue">Internal value to identify this mode</param>
        public AlternatePinMode(string modeName, uint modeValue)
        {
            ModeName = modeName;
            ModeValue = modeValue;
        }

        /// <summary>
        /// User readable name of the mode
        /// </summary>
        public string ModeName
        {
            get;
        }

        /// <summary>
        /// Internal value used for this mode
        /// </summary>
        public uint ModeValue
        {
            get;
        }

        /// <summary>
        /// Equality implementation
        /// </summary>
        public virtual bool Equals(AlternatePinMode? other)
        {
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return other.ModeValue == ModeValue && other.ModeName == ModeName;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            AlternatePinMode? other = obj as AlternatePinMode;
            if (ReferenceEquals(other, null))
            {
                return false;
            }

            return other.ModeValue == ModeValue && other.ModeName == ModeName;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ModeValue.GetHashCode() ^ ModeName.GetHashCode();
        }

        /// <summary>
        /// Equality operator
        /// </summary>
        public static bool operator ==(AlternatePinMode a, AlternatePinMode b)
        {
            return a.Equals(b);
        }

        /// <summary>
        /// Inequality operator
        /// </summary>
        public static bool operator !=(AlternatePinMode a, AlternatePinMode b)
        {
            return !a.Equals(b);
        }
    }
}
