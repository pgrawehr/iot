﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Iot.Device.Nmea0183.Ais
{
    public abstract class AisMessage
    {
        public AisMessageType MessageType { get; }
        public uint Repeat { get; set; }
        public uint Mmsi { get; set; }
        public AisTransceiverClass TransceiverType { get; internal set; }

        protected AisMessage(AisMessageType messageType)
        {
            MessageType = messageType;
        }

        protected AisMessage(AisMessageType messageType, Payload payload)
            : this(messageType)
        {
            Repeat = payload.ReadUInt(6, 2);
            Mmsi = payload.ReadUInt(8, 30);
        }

        public virtual void Encode(Payload payload)
        {
            payload.WriteUInt(Repeat, 2);
            payload.WriteUInt(Mmsi, 30);
        }
    }
}