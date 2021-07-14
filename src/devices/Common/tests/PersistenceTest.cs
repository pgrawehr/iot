using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Iot.Device.Common;
using Xunit;

namespace Iot.Device.Common.Tests
{
    public sealed class PersistenceTest : IDisposable
    {
        private string _pfName;
        private PersistenceFile? _pf;

        public PersistenceTest()
        {
            _pfName = Path.GetTempFileName();
            _pf = new PersistenceFile(_pfName);
        }

        public void Dispose()
        {
            _pf = null;
            File.Delete(_pfName);
        }

        [Fact]
        public void PersistenceOfDoublesWorks()
        {
            PersistentDouble dbl = new PersistentDouble(_pf!, "MyDouble", 1.0, TimeSpan.Zero);
            Assert.Equal(1.0, dbl.Value);
            dbl.Value = 2.0;
            Assert.Equal(2.0, dbl.Value);
            PersistentDouble dbl2 = new PersistentDouble(_pf!, "MyDouble", 1.0, TimeSpan.Zero);
            Assert.Equal(2.0, dbl2.Value);
            dbl2 = new PersistentDouble(_pf!, "MyDouble2", 4.0, TimeSpan.Zero);
            Assert.Equal(4.0, dbl2.Value);
        }

        [Fact]
        public void PersistenceOfTimeSpanWorks()
        {
            var p1 = new PersistentTimeSpan(_pf, "TimeSpan", TimeSpan.FromMinutes(10), TimeSpan.Zero);
            Assert.Equal(TimeSpan.FromMinutes(10), p1.Value);
            p1.Value = TimeSpan.FromSeconds(2);
            Assert.Equal(2.0, p1.Value.TotalSeconds);
            var p2 = new PersistentTimeSpan(_pf, "TimeSpan", TimeSpan.Zero, TimeSpan.Zero);
            Assert.Equal(TimeSpan.FromSeconds(2), p2.Value);
        }

        [Fact]
        public void PersistenceOfBoolWorks()
        {
            var p1 = new PersistentBool(_pf!, "bool", true);
            Assert.True(p1.Value);
            p1.Value = true;
            Assert.True(p1.Value);
            var p2 = new PersistentBool(_pf!, "bool", false);
            Assert.True(p2.Value);
        }
    }
}
