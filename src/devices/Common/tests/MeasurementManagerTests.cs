using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Iot.Device.Common;
using UnitsNet;
using Xunit;

namespace Iot.Device.Common.Tests
{
    public sealed class MeasurementManagerTests : IDisposable
    {
        private readonly MeasurementManager _manager;

        public MeasurementManagerTests()
        {
            _manager = new MeasurementManager();
        }

        public void Dispose()
        {
            _manager.Dispose();
        }

        [Fact]
        public void InsertAndRetrieveMeasurements()
        {
            SensorMeasurement handle = new SensorMeasurement("Wind", Speed.FromMetersPerSecond(10.0), SensorSource.WindRelative);
            Assert.Empty(_manager.Measurements());
            _manager.AddMeasurement(handle);
            Assert.NotEmpty(_manager.Measurements());
            var existing = _manager.Measurements(x => x.SensorSource == SensorSource.WindRelative).ToList();
            Assert.NotEmpty(existing);
            Assert.Equal(handle, existing.First());
        }

        [Fact]
        public void DuplicateInsertThrows()
        {
            SensorMeasurement handle = new SensorMeasurement("WindSpeed", Speed.FromMetersPerSecond(10.0), SensorSource.Air);
            Assert.Empty(_manager.Measurements());
            _manager.AddMeasurement(handle);
            Assert.Throws<InvalidOperationException>(() => _manager.AddMeasurement(handle));
            SensorMeasurement handle2 = new SensorMeasurement("WindDirection", Angle.FromDegrees(180), SensorSource.Air);
            _manager.AddMeasurement(handle2); // no exception (different quantity)
        }

        [Fact]
        public void WindHistory()
        {
            SensorMeasurement handle = new SensorMeasurement("RelativeWind", Speed.FromMetersPerSecond(10.0), SensorSource.WindRelative);
            _manager.AddMeasurement(handle, TimeSpan.FromMinutes(1), TimeSpan.FromDays(1));
            Assert.NotEmpty(_manager.Measurements()); // Returns the measurements, not necessarily with meaningful data!

            var existing = _manager.Measurements(x => x.SensorSource == SensorSource.WindRelative).ToList();
            Assert.Single(existing);
            Assert.Null(existing[0].Value);
            handle.UpdateValue(Speed.FromMetersPerSecond(15.0));
            handle.UpdateValue(Speed.FromMetersPerSecond(11));
            existing = _manager.Measurements(x => x.SensorSource == SensorSource.WindRelative).ToList();
            Assert.Single(existing);
            Assert.NotNull(existing[0].Value);
            var history = _manager.ObtainHistory(handle, TimeSpan.FromDays(1), TimeSpan.FromHours(1));
            Assert.Single(history); // because it certainly didn't take an hour to add the two values in this test
            Assert.True(history[0].Value.Equals(Speed.FromMetersPerSecond(15)));
        }

        [Fact]
        public void ReconfigureHistory()
        {
            SensorMeasurement handle = new SensorMeasurement("SpeedTroughWater", Speed.FromMetersPerSecond(10.0), SensorSource.WaterRelative);
            _manager.AddMeasurement(handle, TimeSpan.FromMinutes(1), TimeSpan.FromDays(1));
            handle.UpdateValue(Speed.FromMetersPerSecond(20));
            handle.UpdateValue(Speed.FromMetersPerSecond(30));
            _manager.ConfigureHistory(handle, TimeSpan.MaxValue, TimeSpan.MaxValue);
            Assert.Empty(_manager.ObtainHistory(handle, TimeSpan.Zero, TimeSpan.Zero));
            Assert.NotEmpty(_manager.ObtainHistory(handle, TimeSpan.FromMinutes(2), TimeSpan.Zero));
        }

        [Fact]
        public void DeleteHistory()
        {
            SensorMeasurement handle = new SensorMeasurement("WindSpeed", Speed.FromMetersPerSecond(10.0), SensorSource.WindRelative);
            _manager.AddMeasurement(handle, TimeSpan.FromMinutes(1), TimeSpan.FromDays(1));
            handle.UpdateValue(Speed.FromMetersPerSecond(1));
            Assert.NotEmpty(_manager.ObtainHistory(handle, TimeSpan.FromDays(1), TimeSpan.Zero));
            _manager.RemoveHistory(handle);
            Assert.Throws<InvalidOperationException>(() => _manager.ObtainHistory(handle, TimeSpan.FromDays(1), TimeSpan.Zero));
            Assert.NotNull(_manager.Measurements());
        }

        [Fact]
        public void RemoveMeasurement()
        {
            SensorMeasurement handle = new SensorMeasurement("WindSpeed", Speed.FromMetersPerSecond(10.0), SensorSource.Air);
            _manager.AddMeasurement(handle);
            Assert.NotEmpty(_manager.Measurements());
            _manager.RemoveMeasurement(handle);
            Assert.Empty(_manager.Measurements());
        }
    }
}
