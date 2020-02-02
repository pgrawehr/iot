// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Iot.Device.Nmea0183;
using Iot.Device.Nmea0183.Sentences;
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
        public void ValidSentenceButNoChecksum()
        {
            string sentence = "$GPRMC,211730.997,A,3511.28,S,13823.26,E,7.0,229.0,190120,,,";
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

        [Fact]
        public void SentenceDecode()
        {
            string sentence = "$GPRMC,211730.997,A,3511.28,S,13823.26,E,7.0,229.0,190120,,,*35";
            var ts = TalkerSentence.FromSentenceString(sentence, out var error);
            Assert.NotNull(ts);
            var decoded = ts.TryGetTypedValue();
            Assert.NotNull(decoded);
            Assert.IsType<RecommendedMinimumNavigationInformation>(decoded);
            Assert.Equal(21, decoded.DateTime.Value.Hour);
        }

        [Theory]
        [InlineData("$GPRMC,211730.997,A,3511.28000,S,13823.26000,E,7.000,229.000,190120,,*19")]
        [InlineData("$GPZDA,135302.036,02,02,2020,+01,00*7F")]
        [InlineData("$WIMWV,350.0,R,16.8,N,A*1A")]
        [InlineData("$WIMWV,220.0,T,5.0,N,A*20")]
        [InlineData("$SDDBS,177.7,f,54.2,M,29.6,F*09")] // Unknown sentence (for now)
        public void SentenceRoundTrip(string input)
        {
            var inSentence = TalkerSentence.FromSentenceString(input, out var error);
            Assert.NotNull(inSentence);
            var decoded = inSentence.TryGetTypedValue();
            Assert.NotNull(decoded);
            TalkerSentence outSentence = new TalkerSentence(inSentence.TalkerId, decoded);
            string output = outSentence.ToString();
            Assert.Equal(input, output);
        }
    }
}
