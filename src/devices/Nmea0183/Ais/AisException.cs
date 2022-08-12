// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Iot.Device.Nmea0183.Ais
{
    public class AisException : Exception
    {
        public AisException(string message)
            : base(message)
        {
        }
    }
}