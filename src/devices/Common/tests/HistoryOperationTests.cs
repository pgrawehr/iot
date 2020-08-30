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
    }
}
