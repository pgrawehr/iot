﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Iot.Device.Common;
using Iot.Device.Nmea0183.Ais;
using Iot.Device.Nmea0183.Sentences;
using Microsoft.VisualStudio.CodeCoverage;
using Shouldly;
using UnitsNet;
using Xunit;
using NavigationStatus = Iot.Device.Nmea0183.Ais.NavigationStatus;

namespace Iot.Device.Nmea0183.Tests.Ais
{
    public class AisManagerTest : IDisposable
    {
        private readonly AisManager _manager;

        public AisManagerTest()
        {
            _manager = new AisManager("Test", true, 269110660u, "Cirrus");
        }

        public void Dispose()
        {
            _manager.Dispose();
        }

        [Fact]
        public void Initialisation()
        {
            _manager.TrackEstimationParameters.MaximumPositionAge.ShouldBeEquivalentTo(TimeSpan.FromSeconds(20));
            _manager.DimensionToStern.ShouldBeEquivalentTo(Length.Zero);
            _manager.OwnMmsi.ShouldBeEquivalentTo(269110660u);
            _manager.OwnShipName.ShouldNotBeEmpty();
        }

        [Fact]
        public void FeedWithRealDataAndCheckGeneralAttributes()
        {
            using NmeaLogDataReader reader = new NmeaLogDataReader("Reader", TestDataHelper.GetResourceStream("Nmea-2021-08-25-16-25.txt"));
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

                if (s.Name != null)
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

        [Fact]
        public void CheckSafety1()
        {
            using NmeaLogDataReader reader = new NmeaLogDataReader("Reader", TestDataHelper.GetResourceStream("Nmea-2021-08-25-16-25.txt"));
            DateTimeOffset latestPacketDate = default;
            reader.OnNewSequence += (source, msg) =>
            {
                _manager.SendSentence(source, msg);
                latestPacketDate = msg.DateTime;
            };

            reader.StartDecode();
            reader.StopDecode();

            Ship ownShip;
            Assert.True(_manager.GetOwnShipData(out ownShip, latestPacketDate));

            Assert.Equal(0, ownShip.DistanceTo(ownShip).Meters, 4);

            foreach (var ship in _manager.GetTargets())
            {
                if (!ship.Position.ContainsValidPosition())
                {
                    continue;
                }

                var distance = ownShip.DistanceTo(ship);
                distance.ShouldBeGreaterThan(Length.FromMeters(50));
                distance.ShouldBeLessThan(Length.FromNauticalMiles(30));

                var age = ship.Age(latestPacketDate);
                if (age.Duration() > TimeSpan.FromMinutes(5))
                {
                    continue;
                }

                var relativePosition = ownShip.RelativePositionTo(ship, latestPacketDate, new TrackEstimationParameters());
                Assert.NotNull(relativePosition);
                Assert.True(relativePosition!.From == ownShip);
                Assert.True(relativePosition.To == ship);
                // Some error is acceptable, since "distance" is not corrected for the time between the now and the last position
                Assert.True(Math.Abs(distance.Meters - relativePosition.Distance.Meters) < 100);
            }

            var ship1 = _manager.GetTarget(211810280) as Ship;
            Assert.NotNull(ship1);
            var relativePos1 = ownShip.RelativePositionTo(ship1!, latestPacketDate, new TrackEstimationParameters());
            Assert.NotNull(relativePos1);
            Assert.True(relativePos1!.TimeOfClosestPointOfApproach.HasValue);
            Assert.True(relativePos1.ClosestPointOfApproach.HasValue);

            Assert.True(relativePos1.ClosestPointOfApproach < relativePos1.Distance);
            Assert.True(relativePos1.TimeToClosestPointOfApproach(latestPacketDate) < TimeSpan.Zero);

            // this ship is directly moving towards us at the end of the test sequence (if we didn't move, and ignoring that
            // in reality, the test data set is from a river with many bends and even drawbridges)
            ship1 = _manager.GetTarget(305966000) as Ship;
            Assert.NotNull(ship1);
            ownShip.SpeedOverGround = Speed.Zero; // Easier to get a close passage if we don't move here
            relativePos1 = ownShip.RelativePositionTo(ship1!, latestPacketDate, new TrackEstimationParameters());
            Assert.NotNull(relativePos1);
            Assert.True(relativePos1!.TimeOfClosestPointOfApproach.HasValue);
            Assert.True(relativePos1.ClosestPointOfApproach.HasValue);

            Assert.True(relativePos1.ClosestPointOfApproach < relativePos1.Distance);
            Assert.Equal(1790, relativePos1.TimeToClosestPointOfApproach(latestPacketDate)!.Value.TotalSeconds, 0);
        }

        [Fact]
        public void CheckSafetyPermanently()
        {
            // This does a safety check all the time. Very expensive...
            using NmeaLogDataReader reader = new NmeaLogDataReader("Reader", TestDataHelper.GetResourceStream("Nmea-2021-08-25-16-25.txt"));
            List<string> messages = new List<string>();
            _manager.TrackEstimationParameters.AisSafetyCheckInterval = TimeSpan.Zero;
            _manager.OnMessage += (received, sourceMmsi, destinationMmsi, text) =>
            {
                messages.Add(text);
                Assert.True(sourceMmsi != _manager.OwnMmsi);
            };

            int msgCount = 0;
            reader.OnNewSequence += (source, msg) =>
            {
                _manager.SendSentence(source, msg);
                if (msg.SentenceId == new SentenceId("VDM"))
                {
                    // This is a big number that misses some dangerous encounters, but causes the test to end in reasonable time (10s instead of 22s)
                    if ((msgCount++ % 60) == 0)
                    {
                        // Call directly, so our test is deterministic
                        _manager.AisAlarmThread(msg.DateTime);
                    }
                }
            };

            reader.StartDecode();
            reader.StopDecode();

            Assert.Equal(20, messages.Count(x => x.Contains("TCPA")));

            var ship = _manager.GetTarget(305966000);
            Assert.NotNull(ship);
            Assert.False(ship!.IsEstimate);
            Assert.NotNull(ship.RelativePosition);
            Assert.True(ship.RelativePosition!.From.Name == "Cirrus");
            Assert.Equal(8260.2, ship.RelativePosition.Distance.Meters, 1);
        }

        [Fact]
        public void EnableDisableBackgroundThread()
        {
            ManualResetEvent ev = new ManualResetEvent(false);
            _manager.OnMessage += (received, sourceMmsi, destinationMmsi, text) =>
            {
                if (text.Contains("GNSS"))
                {
                    ev.Set();
                }
            };

            _manager.ClearWarnings();
            _manager.EnableAisAlarms(true, new TrackEstimationParameters() { AisSafetyCheckInterval = TimeSpan.Zero, WarnIfGnssMissing = true });

            // Fails if we actually hit the timeout
            Assert.True(ev.WaitOne(TimeSpan.FromSeconds(30)));
            _manager.EnableAisAlarms(false);
        }

        [Fact]
        public void UseOfWithWorksCorrectly()
        {
            AisTarget ship = new Ship(1)
            {
                CallSign = "ABCD", CourseOverGround = Angle.FromDegrees(10)
            };

            var ship2 = ship with
            {
                Position = new GeographicPosition(1, 2, 3)
            };

            Assert.True(ship is Ship); // and not its base type
            Assert.True(ship2 is Ship);

            Assert.True(ship2.Position.ContainsValidPosition());
        }

        [Fact]
        public void CheckSpecialTargetDecode()
        {
            using NmeaLogDataReader reader = new NmeaLogDataReader("Reader", TestDataHelper.GetResourceStream("Nmea-AisSpecialTargets.txt"));
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
            sar.SpeedOverGround.ShouldBeGreaterThan(Speed.FromKnots(100));

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

        [Fact]
        public void ShouldEncodeShipCorrectly1()
        {
            // Note that this tests internal conversion functionality of the manager. The public api is tested below.
            Ship ship = new Ship(970001001)
            {
                ShipType = ShipType.NotAvailable, Position = new GeographicPosition(53.7, 9.44, 0), CourseOverGround = Angle.FromDegrees(220),
                RateOfTurn = RotationalSpeed.FromDegreesPerMinute(8.75), TrueHeading = Angle.FromDegrees(20),
                NavigationStatus = NavigationStatus.UnderWaySailing
            };
            var msg = _manager.ShipToPositionReportClassAMessage(ship);
            Assert.Equal(ship.Mmsi, msg.Mmsi);

            var ship2 = new Ship(ship.Mmsi);
            _manager.PositionReportClassAToShip(ship2, msg);
            ship2.Position.ShouldBeEquivalentTo(ship.Position);
            ship2.CourseOverGround.ShouldBeEquivalentTo(ship.CourseOverGround);
            Assert.Equal(ship.RateOfTurn.GetValueOrDefault().Value, ship2.RateOfTurn.GetValueOrDefault().Value, 2);
            ship2.TrueHeading.ShouldBeEquivalentTo(ship.TrueHeading);
            ship2.NavigationStatus.ShouldBeEquivalentTo(ship.NavigationStatus);
        }

        [Fact]
        public void ShouldEncodeShipCorrectly2()
        {
            // Note that this tests internal conversion functionality of the manager. The public api is tested below.
            Ship ship = new Ship(970001001)
            {
                ShipType = ShipType.NotAvailable,
                Position = new GeographicPosition(53.7, 9.45, 0),
                CourseOverGround = Angle.FromDegrees(220),
                RateOfTurn = null,
                TrueHeading = null,
                NavigationStatus = NavigationStatus.RestrictedManeuverability
            };
            var msg = _manager.ShipToPositionReportClassAMessage(ship);
            Assert.Equal(ship.Mmsi, msg.Mmsi);

            var ship2 = new Ship(ship.Mmsi);
            _manager.PositionReportClassAToShip(ship2, msg);
            ship2.Position.ShouldBeEquivalentTo(ship.Position);
            ship2.CourseOverGround.ShouldBeEquivalentTo(ship.CourseOverGround);
            Assert.Equal(ship.RateOfTurn.GetValueOrDefault().Value, ship2.RateOfTurn.GetValueOrDefault().Value, 2);
            ship2.TrueHeading.ShouldBeEquivalentTo(ship.TrueHeading);
            ship2.NavigationStatus.ShouldBeEquivalentTo(ship.NavigationStatus);
        }

        [Fact]
        public void ShouldEncodeShipCorrectly3()
        {
            // Note that this tests internal conversion functionality of the manager. The public api is tested below.
            Ship ship = new Ship(970001001)
            {
                ShipType = ShipType.NotAvailable,
                Position = new GeographicPosition(53.7, 9.45, 0),
                CourseOverGround = Angle.FromDegrees(220),
                RateOfTurn = RotationalSpeed.FromDegreesPerMinute(-25.71),
                TrueHeading = Angle.FromDegrees(180),
                NavigationStatus = NavigationStatus.RestrictedManeuverability
            };
            var msg = _manager.ShipToPositionReportClassAMessage(ship);
            Assert.Equal(ship.Mmsi, msg.Mmsi);

            var ship2 = new Ship(ship.Mmsi);
            _manager.PositionReportClassAToShip(ship2, msg);
            ship2.Position.ShouldBeEquivalentTo(ship.Position);
            ship2.CourseOverGround.ShouldBeEquivalentTo(ship.CourseOverGround);
            Assert.Equal(ship.RateOfTurn.GetValueOrDefault().Value, ship2.RateOfTurn.GetValueOrDefault().Value, 2);
            ship2.TrueHeading.ShouldBeEquivalentTo(ship.TrueHeading);
            ship2.NavigationStatus.ShouldBeEquivalentTo(ship.NavigationStatus);
        }

        [Fact]
        public void SendsShipPositionReport()
        {
            int seenMessages = 0;

            void Report(NmeaSinkAndSource nmeaSinkAndSource, NmeaSentence nmeaSentence)
            {
                nmeaSentence.ShouldBeOfType<RawSentence>();
                seenMessages++;
            }

            Ship ship = new Ship(970001001) { ShipType = ShipType.OtherType, Position = new GeographicPosition(53.7, 9.44, 0), CourseOverGround = Angle.FromDegrees(220) };

            _manager.OnNewSequence += Report;
            _manager.SendShipPositionReport(AisTransceiverClass.A, ship);

            _manager.OnNewSequence -= Report;
            Assert.Equal(1, seenMessages);
        }

        [Fact]
        public void CreatesWarningWhenMobTargetSeen()
        {
            int seenMessages = 0;

            bool warningReceived = false;
            // The sentences that make up the warning are collected here
            List<NmeaSentence> sentences = new List<NmeaSentence>();
            void Report(NmeaSinkAndSource nmeaSinkAndSource, NmeaSentence nmeaSentence)
            {
                nmeaSentence.ShouldBeOfType<RawSentence>();
                seenMessages++;
                sentences.Add(nmeaSentence);
            }

            void MessageReceived(bool received, uint source, uint destination, string text)
            {
                // We get the message twice, but only the second time is tested here
                if (received)
                {
                    warningReceived = true;
                    source.ShouldBeEquivalentTo(970001001u);
                    destination.ShouldBeEquivalentTo(0u);
                    // Because UnitsNet still has the culture bug (uses UI culture), we cannot test the last part of the message
                    text.ShouldStartWith("AIS SART TARGET ACTIVATED: MMSI 970001001 IN POSITION 53* 42.0'N 9* 26.4'E! DISTANCE");
                }
            }

            Ship ship = new Ship(970001001) { ShipType = ShipType.OtherType, Position = new GeographicPosition(53.7, 9.44, 0), CourseOverGround = Angle.FromDegrees(220) };

            var criticalMessage = _manager.SendShipPositionReport(AisTransceiverClass.A, ship);

            _manager.OnNewSequence += Report;
            _manager.OnMessage += MessageReceived;

            _manager.SendSentence(criticalMessage);
            // This path is actually for received messages (we use this to decode our own message here)
            _manager.SendSentences(sentences);

            _manager.OnNewSequence -= Report;
            _manager.OnMessage -= MessageReceived;
            Assert.Equal(2, seenMessages);
            Assert.True(warningReceived);
        }
    }
}
