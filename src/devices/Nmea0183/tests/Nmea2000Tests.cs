// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Iot.Device.Nmea0183.Tests
{
    public class Nmea2000Tests
    {
        [Fact]
        public void ParseRawNmea2000Sentence()
        {
            var m = new MemoryStream();
            var parser = new Nmea2000YdwgParser("Test", m, null);
            var sentence = parser.ParseSentence("17:33:21.141 R 09F80115 A0 7D E6 18 C0 05 FB D5", out NmeaError error);
            Assert.NotNull(sentence);
            Assert.IsType<TalkerSentence>(sentence);
            Assert.StartsWith("$PCDIN,01F801,0000F6E1,15,A07DE618C005FBD5*", sentence.ToString());
        }

        [Fact]
        public void DecodeNothing()
        {
            var m = new MemoryStream();
            var parser = new Nmea2000YdwgParser("Test", m, null);
            parser.StartDecode();
            parser.StopDecode();
        }
    }
}
