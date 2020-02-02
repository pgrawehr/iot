// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Iot.Device.Nmea0183;
using Nmea0183;
using Nmea0183.Sentences;
using Xunit;

namespace Nmea0183.Tests
{
    public class Nmea0183Tests
    {
        [Fact]
        public void SentenceIdentify()
        {
            string sentence = "$GPRMC,211730.997,A,3511.28,S,13823.26,E,7.0,229.0,190120,,,*35";
            var ts = TalkerSentence.FromSentenceString(sentence, out var error);
            Assert.NotNull(ts);
            Assert.Equal(NmeaError.None, error);
            Assert.Equal(new SentenceId("RMC"), ts.Id);
            Assert.Equal(TalkerId.GlobalPositioningSystem, ts.TalkerId);
            Assert.Equal(12, ts.Fields.Count());
        }

        [Fact]
        public void InvalidChecksum()
        {
            string sentence = "$GPRMC,211730.997,A,3511.28,S,13823.26,E,7.0,229.0,190120,,,*1A";
            var ts = TalkerSentence.FromSentenceString(sentence, out var error);
            Assert.Null(ts);
            Assert.Equal(NmeaError.InvalidChecksum, error);
        }

        [Fact]
        public void ChecksumIsNotHex()
        {
            string sentence = "$GPRMC,211730.997,A,3511.28,S,13823.26,E,7.0,229.0,190120,,,*QQ";
            var ts = TalkerSentence.FromSentenceString(sentence, out var error);
            Assert.Null(ts);
            Assert.Equal(NmeaError.InvalidChecksum, error);
        }

        [Fact]
        public void NoHeader()
        {
            string sentence = "RMC,211730.997,A,3511.28,S,13823.26,E,7.0,229.0,190120,,,*1A";
            var ts = TalkerSentence.FromSentenceString(sentence, out var error);
            Assert.Null(ts);
            Assert.Equal(NmeaError.NoSyncByte, error);
        }
    }
}
