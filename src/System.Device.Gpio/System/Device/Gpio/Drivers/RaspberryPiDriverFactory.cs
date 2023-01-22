// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Device.Gpio.Drivers;

/// <summary>
/// A GPIO driver factory for the Raspberry Pi 3 or 4, when running Raspberry Pi OS (formerly Raspbian) or Ubuntu.
/// This can be used to create more concrete drivers
/// </summary>
public static class RaspberryPiDriverFactory
{
    /// <summary>
    /// Creates the driver best suited for the board given in the board information
    /// </summary>
    /// <param name="boardInfo">The system board information (typically obtained</param>
    /// <returns>A driver for this Raspberry Pi model.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="boardInfo"/>was null</exception>
    /// <exception cref="NotSupportedException">There is no driver for the given board model or the <paramref name="boardInfo"/> is invalid.</exception>
    public static GpioDriver CreateDriver(RaspberryBoardInfo boardInfo)
    {
        if (boardInfo == null)
        {
            throw new ArgumentNullException(nameof(boardInfo));
        }

        return CreateDriver(boardInfo.BoardModel, boardInfo) ?? throw new NotSupportedException($"No driver for board model {boardInfo.BoardModel}");
    }

    /// <summary>
    /// Create a driver for the given model
    /// </summary>
    /// <param name="model">The board model</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException">There was no driver for the given board model</exception>
    public static GpioDriver CreateDriver(RaspberryBoardInfo.Model model)
    {
        return CreateDriver(model, null) ?? throw new NotSupportedException($"No driver for board model {model}");
    }

    internal static GpioDriver? CreateDriver(RaspberryBoardInfo.Model model, RaspberryBoardInfo? boardInfo)
    {
        return model switch
        {
            RaspberryBoardInfo.Model.RaspberryPi3B or
                RaspberryBoardInfo.Model.RaspberryPi3APlus or
                RaspberryBoardInfo.Model.RaspberryPi3BPlus or
                RaspberryBoardInfo.Model.RaspberryPiZeroW or
                RaspberryBoardInfo.Model.RaspberryPiZero2W => new RaspberryPi3LinuxDriver(boardInfo),
            RaspberryBoardInfo.Model.RaspberryPi4 or
                RaspberryBoardInfo.Model.RaspberryPi400 => new RaspberryPi4LinuxDriver(boardInfo),
            RaspberryBoardInfo.Model.RaspberryPiComputeModule3 => new RaspberryPiCm3Driver(boardInfo),
            RaspberryBoardInfo.Model.RaspberryPiComputeModule4 => new RaspberryPiCm4Driver(boardInfo),
            _ => null,
        };
    }

    /// <summary>
    /// Returns the best matching driver for this model
    /// </summary>
    /// <exception cref="NotSupportedException">There is no matching driver for this board model,
    /// or the current board is not a Raspberry Pi running a supported operating system</exception>
    public static GpioDriver CreateDriver()
    {
        if (RaspberryBoardInfo.TryDetermineBoardInfo(out RaspberryBoardInfo boardInfo, out Exception error))
        {
            return CreateDriver(boardInfo);
        }

        throw error;
    }
}
