// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Iot.Device.Marine.Sentences
{
    /// <summary>
    /// Group function enumeration for NMEA2000 <see cref="GroupFunction"/> message
    /// </summary>
    public enum GroupFunction
    {
        /// <summary>
        /// Request a particular PGN
        /// </summary>
        Request = 0,

        /// <summary>
        /// Send a one-time command to a device
        /// </summary>
        Command = 1,

        /// <summary>
        /// Acknowledge a request or command.
        /// </summary>
        Acknowledge = 2,

        /// <summary>
        /// Read a variable from a device.
        /// </summary>
        ReadFields = 3,

        /// <summary>
        /// Data of a field read from a device.
        /// </summary>
        ReadFieldsReply = 4,

        /// <summary>
        /// Write a variable to a device.
        /// </summary>
        WriteFields = 5,

        /// <summary>
        /// Indicates a response that fields have been written.
        /// </summary>
        WriteFieldsReply = 6,
    }
}
