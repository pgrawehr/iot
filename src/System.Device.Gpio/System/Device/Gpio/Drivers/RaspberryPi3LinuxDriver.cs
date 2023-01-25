// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Device.Gpio.Drivers;

/// <summary>
/// A GPIO driver for the Raspberry Pi 3 (all variants)
/// </summary>
internal class RaspberryPi3LinuxDriver : RaspberryPiDriver
{
    public RaspberryPi3LinuxDriver()
        : base()
    {
    }

    public RaspberryPi3LinuxDriver(RaspberryBoardInfo? boardInfo)
    : base(boardInfo)
    {
    }

    protected internal override int PinCount => 28;

    protected override bool IsPi4 => false;
}
