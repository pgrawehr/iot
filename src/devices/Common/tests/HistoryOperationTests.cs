// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using UnitsNet;
using Xunit;

namespace Iot.Device.Common.Tests
{
    public class HistoryOperationTests
    {
        [Fact]
        public void Maximum()
        {
            HistoricValue h1 = new HistoricValue(DateTime.Now, Speed.FromMetersPerSecond(1));
            HistoricValue h2 = new HistoricValue(DateTime.Now, Speed.FromMetersPerSecond(2));
            var list = new List<HistoricValue>() { h1, h2 };
            Assert.Equal(Speed.FromMetersPerSecond(2), list.MaxValue());
        }

        [Fact]
        public void Minimum()
        {
            HistoricValue h1 = new HistoricValue(DateTime.Now, Speed.FromMetersPerSecond(1));
            HistoricValue h2 = new HistoricValue(DateTime.Now, Speed.FromMetersPerSecond(2));
            var list = new List<HistoricValue>() { h1, h2 };
            Assert.Equal(Speed.FromMetersPerSecond(1), list.MinValue());
        }

        [Fact]
        public void ConvertToAges()
        {
            DateTime now = DateTime.Now;
            HistoricValue h1 = new HistoricValue(now, Speed.FromMetersPerSecond(1));
            HistoricValue h2 = new HistoricValue(now - TimeSpan.FromSeconds(1), Speed.FromMetersPerSecond(2));
            var list = new List<HistoricValue>() { h1, h2 };
            var result = list.ConvertToAges(now);
            Assert.Equal(TimeSpan.Zero, result[0].Age);
            Assert.Equal(TimeSpan.FromSeconds(1), result[1].Age);
        }
    }
}
