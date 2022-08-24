// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Iot.Device.Nmea0183.Ais;

namespace Iot.Device.Nmea0183.AisSentences
{
    public class PositionReportClassAMessage : PositionReportClassAMessageBase
    {
        public PositionReportClassAMessage()
            : base(AisMessageType.PositionReportClassA)
        {
        }

        public PositionReportClassAMessage(Payload payload)
            : base(AisMessageType.PositionReportClassA, payload)
        {
        }
    }
}
