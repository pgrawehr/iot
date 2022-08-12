// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Iot.Device.Nmea0183.Ais
{
    public class AisMessageException : AisException
    {
        public AisMessageException(string message) : base(message)
        {
        }
    }
}