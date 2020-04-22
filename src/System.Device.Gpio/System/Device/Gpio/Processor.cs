// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

namespace System.Device.Gpio
{
    /// <summary>
    /// The board processor.
    /// </summary>
    public enum Processor
    {
        /// <summary>
        /// Processor is unknown.
        /// </summary>
        Unknown,

        /// <summary>
        /// Processor is a BCM2708.
        /// </summary>
        Bcm2708,

        /// <summary>
        /// Processor is a BCM2709.
        /// </summary>
        Bcm2709,
    }
}
