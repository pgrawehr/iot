﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Iot.Device.Nmea0183.AisSentences;

namespace Iot.Device.Nmea0183.Ais
{
    public class AisMessageFactory
    {
        public Payload Encode<T>(T message)
            where T : AisMessage
        {
            Payload payload = new Payload();
            switch (message)
            {
                case PositionReportClassAMessage t1:
                    t1.Encode(payload);
                    break;
                default:
                    throw new NotImplementedException($"Payloads of type {typeof(T)} cannot be encoded.");
            }

            return payload;
        }

        public AisMessage? Create(Payload payload, string transceiverClass, bool throwOnUnknownMessage)
        {
            AisMessage? ret = Create(payload, throwOnUnknownMessage);
            if (ret != null)
            {
                ret.TransceiverType = transceiverClass == "B" ? AisTransceiverClass.B : AisTransceiverClass.A;
            }

            return ret;
        }

        public AisMessage? Create(Payload payload, bool throwOnUnknownMessage)
        {
            switch (payload.MessageType)
            {
                case 0:
                case AisMessageType.PositionReportClassA:
                    return new PositionReportClassAMessage(payload);
                case AisMessageType.PositionReportClassAAssignedSchedule:
                    return new PositionReportClassAAssignedScheduleMessage(payload);
                case AisMessageType.PositionReportClassAResponseToInterrogation:
                    return new PositionReportClassAResponseToInterrogationMessage(payload);
                case AisMessageType.BaseStationReport:
                    return new BaseStationReportMessage(payload);
                case AisMessageType.StaticAndVoyageRelatedData:
                    return new StaticAndVoyageRelatedDataMessage(payload);
                case AisMessageType.BinaryAddressedMessage:
                    return new BinaryAddressedMessage(payload);
                case AisMessageType.BinaryAcknowledge:
                    return new BinaryAcknowledgeMessage(payload);
                case AisMessageType.BinaryBroadcastMessage:
                    return new BinaryBroadcastMessage(payload);
                case AisMessageType.StandardSarAircraftPositionReport:
                    return new StandardSarAircraftPositionReportMessage(payload);
                case AisMessageType.UtcAndDateInquiry:
                    return new UtcAndDateInquiryMessage(payload);
                case AisMessageType.UtcAndDateResponse:
                    return new UtcAndDateResponseMessage(payload);
                case AisMessageType.AddressedSafetyRelatedMessage:
                    return new AddressedSafetyRelatedMessage(payload);
                case AisMessageType.SafetyRelatedAcknowledgement:
                    return new SafetyRelatedAcknowledgementMessage(payload);
                case AisMessageType.Interrogation:
                    return new InterrogationMessage(payload);
                case AisMessageType.StandardClassBCsPositionReport:
                    return new StandardClassBCsPositionReportMessage(payload);
                case AisMessageType.ExtendedClassBCsPositionReport:
                    return new ExtendedClassBCsPositionReportMessage(payload);
                case AisMessageType.DataLinkManagement:
                    return new DataLinkManagementMessage(payload);
                case AisMessageType.AidToNavigationReport:
                    return new AidToNavigationReportMessage(payload);
                case AisMessageType.StaticDataReport:
                    return StaticDataReportMessage.Create(payload);
                case AisMessageType.PositionReportForLongRangeApplications:
                    return new PositionReportForLongRangeApplicationsMessage(payload);
                default:
                    if (throwOnUnknownMessage)
                    {
                        throw new AisMessageException($"Unrecognised message type: {payload.MessageType}");
                    }
                    else
                    {
                        return null;
                    }
            }
        }
    }
}