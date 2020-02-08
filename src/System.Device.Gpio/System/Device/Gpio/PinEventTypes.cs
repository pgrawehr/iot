// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Device.Gpio
{
    /// <summary>
    /// Event types that can be triggered by the GPIO.
    /// Also used to report the received event types back.
    /// </summary>
    [Flags]
    public enum PinEventTypes
    {
        /// <summary>
        /// None.
        /// </summary>
        None = 0,

        /// <summary>
        /// Triggered when pin value goes from low to high.
        /// </summary>
        Rising = 1,

        /// <summary>
        /// Triggered when a pin value goes from high to low.
        /// </summary>
        Falling = 2,

        /// <summary>
        /// The driver does not know what event was received. This is used when the driver does not support
        /// detecting the type of event that actually occurred (and both edges are monitored).
        /// </summary>
        Unknown = 0x8000
    }
}
