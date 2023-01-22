// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;

namespace System.Device.Gpio;

/// <summary>
/// Identification of Raspberry Pi board models
/// </summary>
public class RaspberryBoardInfo
{
    private const string ModelFilePath = "/proc/device-tree/model";

    /// <summary>
    /// The Raspberry Pi model.
    /// </summary>
    public enum Model
    {
        /// <summary>
        /// Unknown model.
        /// </summary>
        Unknown,

        /// <summary>
        /// Raspberry Model A.
        /// </summary>
        RaspberryPiA,

        /// <summary>
        /// Model A+.
        /// </summary>
        RaspberryPiAPlus,

        /// <summary>
        /// Model B rev1.
        /// </summary>
        RaspberryPiBRev1,

        /// <summary>
        /// Model B rev2.
        /// </summary>
        RaspberryPiBRev2,

        /// <summary>
        /// Model B+.
        /// </summary>
        RaspberryPiBPlus,

        /// <summary>
        /// Compute module.
        /// </summary>
        RaspberryPiComputeModule,

        /// <summary>
        /// Pi 2 Model B.
        /// </summary>
        RaspberryPi2B,

        /// <summary>
        /// Pi Zero.
        /// </summary>
        RaspberryPiZero,

        /// <summary>
        /// Pi Zero W.
        /// </summary>
        RaspberryPiZeroW,

        /// <summary>
        /// Pi Zero 2 W.
        /// </summary>
        RaspberryPiZero2W,

        /// <summary>
        /// Pi 3 Model B.
        /// </summary>
        RaspberryPi3B,

        /// <summary>
        /// Pi 3 Model A+.
        /// </summary>
        RaspberryPi3APlus,

        /// <summary>
        /// Pi 3 Model B+.
        /// </summary>
        RaspberryPi3BPlus,

        /// <summary>
        /// Compute module 3.
        /// </summary>
        RaspberryPiComputeModule3,

        /// <summary>
        /// Pi 4 all versions.
        /// </summary>
        RaspberryPi4,

        /// <summary>
        /// Pi 400
        /// </summary>
        RaspberryPi400,

        /// <summary>
        /// Compute module 4.
        /// </summary>
        RaspberryPiComputeModule4,
    }

    #region Fields

    private readonly Dictionary<string, string> _settings;

    private RaspberryBoardInfo(Dictionary<string, string> settings)
    {
        _settings = settings;

        ProcessorName = _settings.TryGetValue("Hardware", out string? hardware) && hardware is object ? hardware : string.Empty;

        if (_settings.TryGetValue("Revision", out string? revision)
            && revision is { Length: > 0 }
            && int.TryParse(revision, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int firmware))
        {
            Firmware = firmware;
        }

        if (_settings.TryGetValue("Serial", out string? serial))
        {
            SerialNumber = serial;
        }

        BoardModel = GetBoardModel();
    }

    #endregion

    /// <summary>
    /// Returns a read-only collection of the board properties.
    /// </summary>
    public IReadOnlyDictionary<string, string> BoardSettings => _settings;

    /// <summary>
    /// Returns the board model
    /// </summary>
    public Model BoardModel
    {
        get;
    }

    /// <summary>
    /// Gets the processor name.
    /// </summary>
    /// <value>
    /// The name of the processor.
    /// </value>
    public string ProcessorName
    {
        get;
    }

    /// <summary>
    /// Gets the board firmware version.
    /// </summary>
    public int Firmware
    {
        get;
    }

    /// <summary>
    /// Gets the serial number.
    /// </summary>
    public string? SerialNumber
    {
        get;
    }

    /// <summary>
    /// Gets a value indicating whether board is overclocked.
    /// </summary>
    /// <value>
    ///   <c>true</c> if board is overclocked; otherwise, <c>false</c>.
    /// </value>
    public bool IsOverclocked
    {
        get
        {
            return (Firmware & 0xFFFF0000) != 0;
        }
    }

    /// <summary>
    /// Get board model from firmware revision
    /// See http://www.raspberrypi-spy.co.uk/2012/09/checking-your-raspberry-pi-board-version/ for information.
    /// </summary>
    /// <returns></returns>
    private Model GetBoardModel() => (Firmware & 0xFFFF) switch
    {
        0x2 or 0x3 => Model.RaspberryPiBRev1,
        0x4 or 0x5 or 0x6 or 0xd or 0xe or 0xf => Model.RaspberryPiBRev2,
        0x7 or 0x8 or 0x9 => Model.RaspberryPiA,
        0x10 or 0x13 or 0x32 => Model.RaspberryPiBPlus,
        0x11 or 0x14 or 0x61 => Model.RaspberryPiComputeModule,
        0x12 or 0x15 or 0x21 => Model.RaspberryPiAPlus,
        0x1040 or 0x1041 or 0x2042 => Model.RaspberryPi2B,
        0x0092 or 0x0093 => Model.RaspberryPiZero,
        0x00C1 => Model.RaspberryPiZeroW,
        0x2120 => Model.RaspberryPiZero2W,
        0x2082 or 0x2083 => Model.RaspberryPi3B,
        0x20D3 => Model.RaspberryPi3BPlus,
        0x20E0 => Model.RaspberryPi3APlus,
        0x20A0 or 0x2100 => Model.RaspberryPiComputeModule3,
        0x3111 or 0x3112 or 0x3114 or 0x3115 => Model.RaspberryPi4,
        0x3140 or 0x3141 => Model.RaspberryPiComputeModule4,
        0x3130 => Model.RaspberryPi400,
        _ => Model.Unknown,
    };

    #region Private Helpers

    /// <summary>
    /// Detect the board and CPU information
    /// </summary>
    /// <param name="boardInfo">An instance of <see cref="RaspberryBoardInfo"/></param>
    /// <param name="error">The internal error in case of a problem</param>
    /// <returns>
    /// True on success, false otherwise
    /// </returns>
    public static bool TryDetermineBoardInfo(
#if NET5_0_OR_GREATER
    [NotNullWhen(true)]
#endif
    out RaspberryBoardInfo boardInfo,
#if NET5_0_OR_GREATER
    [NotNullWhen(false)]
#endif
    out Exception error)
    {
        try
        {
            const string filePath = "/proc/cpuinfo";

            var cpuInfo = File.ReadAllLines(filePath);
            var settings = new Dictionary<string, string>();
            var suffix = string.Empty;

            foreach (var line in cpuInfo)
            {
                var separator = line.IndexOf(':');

                if (!string.IsNullOrWhiteSpace(line) && separator > 0)
                {
                    var key = line.Substring(0, separator).Trim();
                    var val = line.Substring(separator + 1).Trim();
                    if (string.Equals(key, "processor", StringComparison.InvariantCultureIgnoreCase))
                    {
                        suffix = "." + val;
                    }

                    settings.Add(key + suffix, val);
                }
                else
                {
                    suffix = string.Empty;
                }
            }

            boardInfo = new RaspberryBoardInfo(settings);
            error = null!;
            return true;
        }
        catch (Exception ex)
        {
            error = ex;
            boardInfo = null!;
            return false;
        }
    }
    #endregion
}
