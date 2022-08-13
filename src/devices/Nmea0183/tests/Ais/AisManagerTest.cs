// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shouldly;
using Xunit;

namespace Iot.Device.Nmea0183.Tests.Ais
{
    public class AisManagerTest
    {
        private AisManager _manager;
        public AisManagerTest()
        {
            _manager = new AisManager("Test");
        }

        [Fact]
        public void FeedWithRealData()
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
                Assert.True(s.Position.ContainsValidPosition());
                // The recording is from somewhere in the baltic, so this is a very broad bounding rectangle.
                // If this is exceeded, the position decoding was most likely wrong.
                s.Position.Longitude.ShouldBeInRange(9.0, 10.5);
                s.Position.Latitude.ShouldBeInRange(56.0, 58.0);
            }
        }
    }
}
