// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Iot.Device.Nmea0183.Ais;

namespace Iot.Device.Nmea0183.AisSentences
{
    public class PositionReportClassAResponseToInterrogationMessage : PositionReportClassAMessageBase
    {
        public PositionReportClassAResponseToInterrogationMessage()
            : base(AisMessageType.PositionReportClassAResponseToInterrogation)
        {
        }

        public PositionReportClassAResponseToInterrogationMessage(Payload payload)
            : base(AisMessageType.PositionReportClassAResponseToInterrogation, payload)
        {
        }
    }
}