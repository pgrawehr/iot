// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Iot.Device.Marine.Sentences;

/// <summary>
/// GNSS integrity status. This is used in sentences like GNS, GGA, and RMC to indicate the integrity of the GNSS fix.
/// </summary>
public enum GnssIntegrity : byte
{
    /// <summary>
    /// Integrity checking is not performed. This is the default value and indicates that the Gnss receiver does not provide integrity information.
    /// </summary>
    NoIntegrityChecking = 0,

    /// <summary>
    /// Gnss Integrity is good. The GNSS fix is considered reliable and can be used for navigation.
    /// </summary>
    Safe = 1,

    /// <summary>
    /// Gnss Integrity is caution. The GNSS fix may not be reliable and should be used with caution.
    /// </summary>
    Caution = 2,
}
