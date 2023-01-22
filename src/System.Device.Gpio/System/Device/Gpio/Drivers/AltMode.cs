// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace System.Device.Gpio.Drivers
{
    /// <summary>
    /// Used to set the Alternate Pin Mode on Raspberry Pi 3/4.
    /// The actual pin function for anything other than Input or Output is dependent
    /// on the pin and can be looked up in the Raspi manual.
    /// </summary>
    public enum AltMode
    {
        /// <summary>
        /// The mode is unknown
        /// </summary>
        Unknown,

        /// <summary>
        /// Gpio mode input
        /// </summary>
        Input,

        /// <summary>
        /// Gpio mode output
        /// </summary>
        Output,

        /// <summary>
        /// Mode ALT0
        /// </summary>
        Alt0,

        /// <summary>
        /// Mode ALT1
        /// </summary>
        Alt1,

        /// <summary>
        /// Mode ALT2
        /// </summary>
        Alt2,

        /// <summary>
        /// Mode ALT3
        /// </summary>
        Alt3,

        /// <summary>
        /// Mode ALT4
        /// </summary>
        Alt4,

        /// <summary>
        /// Mode ALT5
        /// </summary>
        Alt5,
    }
}
