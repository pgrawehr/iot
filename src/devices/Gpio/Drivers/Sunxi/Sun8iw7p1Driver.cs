﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Iot.Device.Gpio.Drivers
{
    /// <summary>
    /// A GPIO driver for Allwinner H2+/H3.
    /// </summary>
    public class Sun8iw7p1Driver : SunxiDriver
    {
        /// <inheritdoc/>
        protected override int CpuxPortBaseAddress => 0x01C20800;

        /// <inheritdoc/>
        protected override int CpusPortBaseAddress => 0x01F02C00;
    }
}