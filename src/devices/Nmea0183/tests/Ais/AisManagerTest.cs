// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Iot.Device.Nmea0183.Ais;
using Microsoft.VisualStudio.CodeCoverage;
using Shouldly;
using Xunit;

namespace Iot.Device.Nmea0183.Tests.Ais
{
    public class AisManagerTest
    {
        private AisManager _manager;
        public AisManagerTest()
        {
            _manager = new AisManager("Test", true);
        }

        [Fact]
        public void FeedWithRealDataAndCheckGeneralAttributes()
        {
            using NmeaLogDataReader reader = new NmeaLogDataReader("Reader", "../../../Nmea-2021-08-25-16-25.txt");
            reader.OnNewSequence += (source, msg) =>
            {
                _manager.SendSentence(source, msg);
            };

            reader.StartDecode();
            reader.StopDecode();

            var ships = _manager.GetShips();
            ships.ShouldNotBeEmpty();
            Assert.True(ships.All(x => x.Mmsi != 0));
            foreach (var s in ships)
            {
                // The recording is from somewhere in the baltic, so this is a very broad bounding rectangle.
                // If this is exceeded, the position decoding was most likely wrong.
                if (s.Position.ContainsValidPosition())
                {
                    s.Position.Longitude.ShouldBeInRange(9.0, 10.5);
                    s.Position.Latitude.ShouldBeInRange(56.0, 58.0);
                }

                if (!string.IsNullOrEmpty(s.Name))
                {
                    Assert.True(s.Name.Length <= 20);
                    Assert.True(s.Name.All(x => x < 0x128 && x >= ' ')); // Only printable ascii letters
                }
            }

            // Check we have at least one A and B type message that contain name and a valid position
            Assert.Contains(ships, x => x.TransceiverClass == AisTransceiverClass.A && !string.IsNullOrWhiteSpace(x.Name) && !string.IsNullOrWhiteSpace(x.CallSign) && x.Position.ContainsValidPosition());
            Assert.Contains(ships, x => x.TransceiverClass == AisTransceiverClass.B && !string.IsNullOrWhiteSpace(x.Name) && !string.IsNullOrWhiteSpace(x.CallSign) && x.Position.ContainsValidPosition());

            _manager.GetBaseStations().ShouldNotBeEmpty();
        }

        [Fact]
        public void CheckAtoNTargetDecode()
        {
            // This file contains virtual Aid-to-navigation targets (but only few ships)
            var files = Directory.GetFiles("C:\\projects\\shiplogs\\Log-2022-08-24\\", "Nmea-2022-08-24-15-19.txt", SearchOption.TopDirectoryOnly);
            using NmeaLogDataReader reader = new NmeaLogDataReader("Reader", files);
            int messagesParsed = 0;
            reader.OnNewSequence += (source, msg) =>
            {
                _manager.SendSentence(source, msg);
                messagesParsed++;
            };

            reader.StartDecode();
            ////var time = DateTimeOffset.UtcNow;
            ////var sentence = TalkerSentence
            ////    .FromSentenceString("!AIVDM,1,1,,B,ENk`sR9`92ah97PR9h0W1T@1@@@=MTpS<7GFP00003vP000,2*4B",
            ////        out var success)!.GetAsRawSentence(ref time);
            ////_manager.SendSentence(reader, sentence);
            reader.StopDecode();

            var ships = _manager.GetShips();
            ships.ShouldNotBeEmpty();
            // There are some very strange "group" MMSI targets in this data set. Not sure what they are about.
            Assert.True(ships.All(x => x.Mmsi != 0));
            messagesParsed.ShouldBeGreaterThanOrEqualTo(50000); // A single file has around 44000 messages

            var aton = _manager.GetAtoNTargets();
            aton.ShouldNotBeEmpty();

            foreach (var aidToNavigation in aton)
            {
                // The recording is from somewhere in the baltic, so this is a very broad bounding rectangle.
                // If this is exceeded, the position decoding was most likely wrong.
                if (aidToNavigation.Position.ContainsValidPosition())
                {
                    aidToNavigation.Position.Longitude.ShouldBeInRange(8.0, 8.1);
                    aidToNavigation.Position.Latitude.ShouldBeInRange(56.5, 57.0);
                }

                var n = aidToNavigation.Name;
                n.ShouldNotBeEmpty();
                Assert.True(n.Length <=
                            34); // This can be longer than ship names (it consists of two fields, so must be combined correctly)
                Assert.True(n.All(x => x < 0x128 && x >= ' ')); // Only printable ascii letters
            }
        }
    }
}
