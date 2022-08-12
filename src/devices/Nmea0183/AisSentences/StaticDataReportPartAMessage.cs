// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Iot.Device.Nmea0183.Ais;

namespace Iot.Device.Nmea0183.AisSentences
{
    public class StaticDataReportPartAMessage : StaticDataReportMessage
    {
        public string ShipName { get; set; }
        public uint Spare { get; set; }

        public StaticDataReportPartAMessage()
            : base(0)
        {
            ShipName = string.Empty;
        }

        public StaticDataReportPartAMessage(StaticDataReportMessage message, Payload payload)
            : base(message)
        {
            ShipName = payload.ReadString(40, 120);
        }
    }
}
