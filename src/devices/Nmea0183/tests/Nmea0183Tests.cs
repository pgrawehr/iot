// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Iot.Device.Nmea0183;
using Iot.Device.Nmea0183.Sentences;
using Nmea0183;
using Nmea0183.Sentences;
using Units;
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

        [Fact]
        public void CorrectlyDecodesXdrEvenIfExtraChars()
        {
            // Seen in one example (the last "A" shouldn't be there)
            string sentence = "$IIXDR,A,4,D,ROLL,A,-2,D,PTCH,A*1A";
            var inSentence = TalkerSentence.FromSentenceString(sentence, out var error);
            Assert.Equal(NmeaError.None, error);
            Assert.NotNull(inSentence);
            var decoded = (TransducerMeasurement)inSentence.TryGetTypedValue();
            Assert.NotNull(decoded);
            var roll = decoded.DataSets[0];
            Assert.Equal(4.0, roll.Value);
            Assert.Equal("A", roll.DataType);
            Assert.Equal("D", roll.Unit);
            Assert.Equal("ROLL", roll.DataName);

            var pitch = decoded.DataSets[1];
            Assert.Equal(-2.0, pitch.Value);
            Assert.Equal("A", pitch.DataType);
            Assert.Equal("D", pitch.Unit);
            Assert.Equal("PTCH", pitch.DataName);
        }

        [Fact]
        public void GgaDecode()
        {
            string msg = "$GPGGA,163810,4728.7027,N,00929.9666,E,2,12,0.6,397.4,M,46.8,M,,*4C";

            var inSentence = TalkerSentence.FromSentenceString(msg, out var error);
            Assert.Equal(NmeaError.None, error);
            Assert.NotNull(inSentence);
            GlobalPositioningSystemFixData nmeaSentence = (GlobalPositioningSystemFixData)inSentence.TryGetTypedValue();
            var expectedPos = new GeographicPosition(47.478378333333332, 9.4994433333333337, 397.4 + 46.8);
            Assert.True(expectedPos.EqualPosition(nmeaSentence.Position));
            Assert.Equal(GpsQuality.DifferentialFix, nmeaSentence.Status);
            Assert.Equal(12, nmeaSentence.NumberOfSatellites);
            Assert.Equal(0.6, nmeaSentence.Hdop);
        }

        [Theory]
        // These were seen in actual NMEA data streams
        [InlineData("$GPGGA,163806,,*4E")]
        // GGA, but without elevation (basically valid, but rather useless if RMC is also provided)
        [InlineData("$YDGGA,163804.00,4728.7001,N,00929.9640,E,1,10,1.00,,M,,M,,*68")]
        public void DontCrashOnTheseInvalidSentences(string sentence)
        {
            var inSentence = TalkerSentence.FromSentenceString(sentence, out var error);
            if (error == NmeaError.InvalidChecksum)
            {
                Assert.Null(inSentence);
            }
            else
            {
                Assert.NotNull(inSentence);
                Assert.True(inSentence.Fields != null);
            }
        }

        [Theory]
        [InlineData("$GPRMC,211730.997,A,3511.28000,S,13823.26000,E,7.000,229.000,190120,,*19")]
        [InlineData("$GPZDA,135302.036,02,02,2020,+01,00*7F")]
        [InlineData("$WIMWV,350.0,R,16.8,N,A*1A")]
        [InlineData("$WIMWV,220.0,T,5.0,N,A*20")]
        [InlineData("$SDDBS,177.9,f,54.21,M,29.3,F*33")]
        [InlineData("$IIXDR,P,1.02481,B,Barometer*29")]
        [InlineData("$IIXDR,A,4,D,ROLL,A,-2,D,PITCH*3E")]
        [InlineData("$IIXDR,C,18.2,C,ENV_WATER_T,C,28.69,C,ENV_OUTAIR_T,P,101400,P,ENV_ATMOS_P*7C")]
        // GGA with elevation
        [InlineData("$GPGGA,163810.000,4728.70270,N,00929.96660,E,2,12,0.6,397.4,M,46.8,M,,*52")]
        [InlineData("$IIDBK,29.2,f,8.90,M,4.9,F*0B")] // Unknown sentence (for now)
        public void SentenceRoundTrip(string input)
        {
            var inSentence = TalkerSentence.FromSentenceString(input, out var error);
            Assert.Equal(NmeaError.None, error);
            Assert.NotNull(inSentence);
            var decoded = inSentence.TryGetTypedValue();
            Assert.NotNull(decoded);
            TalkerSentence outSentence = new TalkerSentence(inSentence.TalkerId, decoded);
            string output = outSentence.ToString();
            Assert.Equal(input, output);
        }
    }
}
