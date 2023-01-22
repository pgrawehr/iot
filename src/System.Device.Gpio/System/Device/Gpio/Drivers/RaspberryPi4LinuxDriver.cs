// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Device.Gpio.Drivers;

/// <summary>
/// A GPIO driver for the Raspberry Pi 4 and 400
/// </summary>
internal class RaspberryPi4LinuxDriver : RaspberryPi3LinuxDriver
{
    public RaspberryPi4LinuxDriver()
        : base()
    {
    }

    public RaspberryPi4LinuxDriver(RaspberryBoardInfo? boardInfo)
    : base(boardInfo)
    {
    }

    protected override bool IsPi4 => true;

    /// <summary>
    /// Converts a board pin number to the driver's logical numbering scheme.
    /// </summary>
    /// <param name="pinNumber">The board pin number to convert.</param>
    /// <returns>The pin number in the driver's logical numbering scheme.</returns>
    protected internal override int ConvertPinNumberToLogicalNumberingScheme(int pinNumber)
    {
        // CM3 has no physical numbering scheme
        return pinNumber;
    }
}
