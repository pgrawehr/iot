using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Iot.Device.Nmea0183.Sentences;
using Moq;
using Xunit;

namespace Iot.Device.Nmea0183.Tests
{
    public class AutopilotControllerTest
    {
        private AutopilotController _autopilot;
        private Mock<NmeaSinkAndSource> _source;
        private Mock<NmeaSinkAndSource> _output;
        private SentenceCache _sentenceCache;

        public AutopilotControllerTest()
        {
            _source = new Mock<NmeaSinkAndSource>(MockBehavior.Loose, "Input");
            _output = new Mock<NmeaSinkAndSource>(MockBehavior.Strict, "Output");
            _sentenceCache = new SentenceCache(_source.Object);
            _autopilot = new AutopilotController(_sentenceCache, _output.Object);
        }

        [Fact]
        public void DoesNothingWhenNoUsableInput()
        {
            _autopilot.CalculateNewStatus(0, DateTimeOffset.UtcNow);
        }

        [Fact]
        public void CalculationLoopWithExternalInput()
        {
            string[] inputSequences =
            {
                // Messages in a typical scenario, where an external GPS sends a route and navigates along it
                "$GPWPL,4729.0235,N,00929.0536,E,R1",
                "$GPWPL,4729.1845,N,00929.7746,E,R2",
                "$GPWPL,4729.1214,N,00930.2754,E,R3",
                "$GPWPL,4728.9218,N,00930.3359,E,R4",
                "$GPWPL,4728.8150,N,00929.9999,E,R5",
                "$GPBOD,244.8,T,242.9,M,R5,R4",
                "$GPRMB,A,0.50,L,R4,R5,4728.8150,N,00929.9999,E,0.737,201.6,-1.7,V,D",
                "$GPRMC,115615.000,A,4729.49810,N,00930.39910,E,1.600,38.500,240520,1.900,E,D",
                "$GPRTE,1,1,c,Route 008,R1,R2,R3,R4,R5",
                "$HCHDG,30.9,,,1.9,E",
                "$GPXTE,A,A,0.500,L,N,D",
                "$GPVTG,38.5,T,36.6,M,1.6,N,3.0,K,A"
            };

            // Similar to input, except with added accuracy and some additional messages
            // (even though quite a bit of the information is redundant across messages)
            string[] expectedOutput =
            {
                "$GPRMB,A,0.504,L,R4,R5,4728.81500,N,00929.99990,E,0.735,201.6,-1.5,V,D",
                "$GPXTE,A,A,0.504,L,N,D",
                "$GPVTG,38.5,T,36.6,M,1.6,N,3.0,K,A",
                "$GPBWC,183758.833,4728.81500,N,00929.99990,E,201.6,T,199.7,M,0.735,N,R5,D",
                "$GPBOD,244.9,T,243.0,M,R5,R4",
                "$GPWPL,4729.02350,N,00929.05360,E,R1",
                "$GPWPL,4729.18450,N,00929.77460,E,R2",
                "$GPWPL,4729.12140,N,00930.27540,E,R3",
                "$GPWPL,4728.92180,N,00930.33590,E,R4",
                "$GPWPL,4728.81500,N,00929.99990,E,R5",
                "$GPRTE,2,1,c,Route 008,R1,R2,R3",
                "$GPRTE,2,2,c,Route 008,R4,R5"
            };

            DateTimeOffset now = new DateTimeOffset(2020, 05, 31, 18, 37, 58, 833, TimeSpan.Zero);
            _output.Setup(x => x.SendSentences(It.IsNotNull<IEnumerable<NmeaSentence>>())).Callback<IEnumerable<NmeaSentence>>(
                outputSentence =>
                {
                    // Check that the messages that should be sent are equal to what's defined above
                    Assert.True(outputSentence.Any());
                    int index = 0;
                    foreach (var msg in outputSentence)
                    {
                        string txt = msg.ToNmeaMessage();
                        txt = $"${TalkerId.GlobalPositioningSystem}{msg.SentenceId},{txt}";
                        Assert.Equal(expectedOutput[index], txt);
                        index++;
                    }
                });

            ParseSequencesAndAddToCache(inputSequences);
            _autopilot.CalculateNewStatus(0, now);
        }

        private void ParseSequencesAndAddToCache(IEnumerable<string> inputSequences)
        {
            foreach (var seq in inputSequences)
            {
                var decoded = TalkerSentence.FromSentenceString(seq, out var error);
                Assert.Equal(NmeaError.None, error);
                Assert.NotNull(decoded);
                var s = decoded.TryGetTypedValue();
                _sentenceCache.Add(s);
            }
        }
    }
}
