// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Iot.Device.Nmea0183.Ais
{
    public class AisParserException : AisException
    {
        public string Sentence { get; set; }

        public AisParserException(string message, string sentence)
            : base(message)
        {
            Sentence = sentence;
        }
    }
}