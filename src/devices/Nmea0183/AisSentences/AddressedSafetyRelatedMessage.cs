// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace AisParser.Messages
{
    public class AddressedSafetyRelatedMessage : AisMessage
    {
        public uint SequenceNumber { get; set; }
        public uint DestinationMmsi { get; set; }
        public bool RetransmitFlag { get; set; }
        public uint Spare { get; set; }
        public string Text { get; set; }

        public AddressedSafetyRelatedMessage()
            : base(AisMessageType.AddressedSafetyRelatedMessage)
        {
            
        }
        
        public AddressedSafetyRelatedMessage(Payload payload)
            : base(AisMessageType.AddressedSafetyRelatedMessage, payload)
        {
            SequenceNumber = payload.ReadUInt(38, 2);
            DestinationMmsi = payload.ReadUInt(40, 30);
            RetransmitFlag = payload.ReadBoolean(70, 1);
            Spare = payload.ReadUInt(71, 1);
            Text = payload.ReadString(72, 936);
        }
    }
}
