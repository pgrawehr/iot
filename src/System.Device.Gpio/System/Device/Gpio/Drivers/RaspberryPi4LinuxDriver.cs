// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Device.Gpio.Drivers;

/// <summary>
/// A GPIO driver for the Raspberry Pi 4 and 400
/// </summary>
internal class RaspberryPi4LinuxDriver : RaspberryPiDriver
{
    public RaspberryPi4LinuxDriver()
        : base()
    {
    }

    public RaspberryPi4LinuxDriver(RaspberryBoardInfo? boardInfo)
    : base(boardInfo)
    {
    }

    protected internal override int PinCount => 28;

    protected override bool IsPi4 => true;
}
