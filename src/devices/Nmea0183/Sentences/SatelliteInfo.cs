// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Iot.Device.Nmea0183.Sentences
{
    public class SatelliteInfo
    {
        public SatelliteInfo(string id)
        {
            Id = id;
        }

        public string Id
        {
            get;
            internal set;
        }

        public double? Azimuth
        {
            get;
            internal set;
        }

        public double? Elevation
        {
            get;
            internal set;
        }

        public double? Snr
        {
            get;
            internal set;
        }
    }
}
