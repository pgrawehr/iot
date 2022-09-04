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
using UnitsNet;
using Xunit;

namespace Iot.Device.Nmea0183.Tests.Ais
{
    public class AisManagerTest
    {
        private AisManager _manager;
        public AisManagerTest()
        {
            _manager = new AisManager("Test", true, 269110660u, "Cirrus");
        }

        [Fact]
        public void Initialisation()
        {
            _manager.AllowedPositionAge.ShouldBeEquivalentTo(TimeSpan.FromMinutes(1));
            _manager.DimensionToStern.ShouldBeEquivalentTo(Length.Zero);
            _manager.OwnMmsi.ShouldBeEquivalentTo(269110660u);
            _manager.OwnShipName.ShouldNotBeEmpty();
        }

        [Fact]
        public void FeedWithRealDataAndCheckGeneralAttributes()
        {
            using NmeaLogDataReader reader = new NmeaLogDataReader("Reader", "../../../Nmea-2021-08-25-16-25.txt");
            DateTimeOffset latestPacketDate = default;
            reader.OnNewSequence += (source, msg) =>
            {
                _manager.SendSentence(source, msg);
                latestPacketDate = msg.DateTime;
            };

            reader.StartDecode();
            reader.StopDecode();

            var ships = _manager.GetSpecificTargets<Ship>();
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

            _manager.GetSpecificTargets<BaseStation>().ShouldNotBeEmpty();

            Assert.True(_manager.GetOwnShipData(out Ship own, latestPacketDate));
            Assert.Equal(own.SpeedOverGround.Value, Speed.FromKnots(2.7).Value, 1);
            own.Name.ShouldBeEquivalentTo("Cirrus");
            own.Position.ContainsValidPosition().ShouldBeTrue();
            own.SpeedOverGround.ShouldBeInRange(Speed.FromKnots(2), Speed.FromKnots(5));
        }

        ////[Fact]
        ////public void FeedWithMuchData()
        ////{
        ////    // This file contains virtual Aid-to-navigation targets (but only few ships)
        ////    var files = Directory.GetFiles("C:\\projects\\shiplogs\\Log-2022-08-29\\", "*.txt", SearchOption.TopDirectoryOnly);
        ////    using NmeaLogDataReader reader = new NmeaLogDataReader("Reader", files);
        ////    reader.OnNewSequence += (source, msg) =>
        ////    {
        ////        _manager.SendSentence(source, msg);
        ////    };

        ////    reader.StartDecode();
        ////    reader.StopDecode();

        ////    var ships = _manager.GetSpecificTargets<Ship>();
        ////    ships.ShouldNotBeEmpty();
        ////    Assert.True(ships.All(x => x.Mmsi != 0));
        ////    foreach (var s in ships)
        ////    {
        ////        // The recording is from somewhere in the baltic, so this is a very broad bounding rectangle.
        ////        // If this is exceeded, the position decoding was most likely wrong.
        ////        if (s.Position.ContainsValidPosition())
        ////        {
        ////            s.Position.Longitude.ShouldBeInRange(9.0, 10.5);
        ////            s.Position.Latitude.ShouldBeInRange(56.0, 58.0);
        ////        }

        ////        if (!string.IsNullOrEmpty(s.Name))
        ////        {
        ////            Assert.True(s.Name.Length <= 20);
        ////            Assert.True(s.Name.All(x => x < 0x128 && x >= ' ')); // Only printable ascii letters
        ////        }
        ////    }

        ////    // Check we have at least one A and B type message that contain name and a valid position
        ////    Assert.Contains(ships, x => x.TransceiverClass == AisTransceiverClass.A && !string.IsNullOrWhiteSpace(x.Name) && !string.IsNullOrWhiteSpace(x.CallSign) && x.Position.ContainsValidPosition());
        ////    Assert.Contains(ships, x => x.TransceiverClass == AisTransceiverClass.B && !string.IsNullOrWhiteSpace(x.Name) && !string.IsNullOrWhiteSpace(x.CallSign) && x.Position.ContainsValidPosition());

        ////    _manager.GetSpecificTargets<BaseStation>().ShouldNotBeEmpty();
        ////}

        [Fact]
        public void CheckSpecialTargetDecode()
        {
            using NmeaLogDataReader reader = new NmeaLogDataReader("Reader", "../../../Nmea-AisSpecialTargets.txt");
            int messagesParsed = 0;
            reader.OnNewSequence += (source, msg) =>
            {
                _manager.SendSentence(source, msg);
                messagesParsed++;
            };

            reader.StartDecode();
            reader.StopDecode();

            var ships = _manager.GetSpecificTargets<Ship>();
            ships.ShouldBeEmpty();
            // There are some very strange "group" MMSI targets in this data set. Not sure what they are about.
            Assert.True(ships.All(x => x.Mmsi != 0));

            Assert.True(_manager.TryGetTarget(1015, out AisTarget tgt));

            var sar = (SarAircraft)tgt;
            sar.Speed.GetValueOrDefault().ShouldBeGreaterThan(Speed.FromKnots(100));

            Assert.True(_manager.TryGetTarget(993672072, out tgt));
            var aton = (AidToNavigation)tgt;
            Assert.True(aton.Position.ContainsValidPosition());
            aton.Name.ShouldBeEquivalentTo("PRES ROADS ANCH B");
            Assert.Equal(MmsiType.AtoN, aton.IdentifyMmsiType());
            Assert.False(aton.Virtual);

            Assert.True(_manager.TryGetTarget(2579999, out tgt));
            var baseStation = (BaseStation)tgt;
            baseStation.Name.ShouldNotBeEmpty();
            Assert.True(baseStation.Position.ContainsValidPosition());
            Assert.Equal("002579999", baseStation.FormatMmsi());
            Assert.Equal(MmsiType.BaseStation, baseStation.IdentifyMmsiType());
        }
    }
}
