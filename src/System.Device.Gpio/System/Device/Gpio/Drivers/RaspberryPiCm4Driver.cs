// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Device.Gpio.Drivers;

/// <summary>
/// A GPIO driver for the Raspberry Pi Compute Module 4
/// </summary>
internal class RaspberryPiCm4Driver : RaspberryPiDriver
{
    public RaspberryPiCm4Driver()
        : base()
    {
    }

    public RaspberryPiCm4Driver(RaspberryBoardInfo? boardInfo)
    : base(boardInfo)
    {
    }

    /// <summary>
    /// Raspberry CM4 has 48 GPIO pins.
    /// </summary>
    protected internal override int PinCount => 48;

    protected override bool IsPi4 => true;

    /// <summary>
    /// Converts a board pin number to the driver's logical numbering scheme.
    /// </summary>
    /// <param name="pinNumber">The board pin number to convert.</param>
    /// <returns>The pin number in the driver's logical numbering scheme.</returns>
    protected internal override int ConvertPinNumberToLogicalNumberingScheme(int pinNumber)
    {
        // CM4 has no physical numbering scheme
        return pinNumber;
    }
}
