using System;
using System.Collections.Generic;
using System.Text;
using Iot.Device.Nmea0183.Sentences;
using Moq;
using Xunit;

namespace Iot.Device.Nmea0183.Tests
{
    public class SentenceCacheTest
    {
        private SentenceCache _cache;
        private Mock<NmeaSinkAndSource> _sink;

        public SentenceCacheTest()
        {
            _sink = new Mock<NmeaSinkAndSource>(MockBehavior.Strict, "Test");
            _cache = new SentenceCache(_sink.Object);
        }

        [Fact]
        public void CacheKeepsLastElement()
        {
            var sentence1 = new HeadingTrue(10.2);
            var sentence2 = new HeadingTrue(-1);
            _sink.Raise(x => x.OnNewSequence += null, null, sentence1);
            _sink.Raise(x => x.OnNewSequence += null, null, sentence2);

            Assert.Equal(sentence2, _cache.GetLastSentence(HeadingTrue.Id));
        }

        [Fact]
        public void ReturnsNullNoSuchElement()
        {
            var sentence1 = new HeadingTrue(10.2);
            _sink.Raise(x => x.OnNewSequence += null, null, sentence1);

            Assert.Null(_cache.GetLastSentence(HeadingMagnetic.Id));
        }

        [Fact]
        public void GetNothingEmptyRoute()
        {
            var sentence1 = new Route("RT", 1, 1, new List<string>());
            _sink.Raise(x => x.OnNewSequence += null, null, sentence1);

            Assert.Null(_cache.GetCurrentRoute());
        }

        [Fact]
        public void GetCompleteRouteOneElement()
        {
            var sentence1 = new Route("RT", 1, 1, new List<string>() { "A", "B" });
            _sink.Raise(x => x.OnNewSequence += null, null, sentence1);

            var route = _cache.GetCurrentRoute();
            Assert.Equal(2, route.Count);
            Assert.Equal("RT", route[0].RouteName);
            Assert.Equal(2, route[0].TotalPointsInRoute);
            Assert.Equal(0, route[0].IndexInRoute);
            Assert.Equal("A", route[0].WaypointName);
            Assert.Equal("B", route[1].WaypointName);
            Assert.Equal(1, route[1].IndexInRoute);
        }

        [Fact]
        public void GetCompleteRouteWithWaypointLookup()
        {
            var sentence1 = new Route("RT", 1, 1, new List<string>() { "A", "B" });
            var sentence2 = new WayPoint(new GeographicPosition(1, 2, 3), "A");
            _sink.Raise(x => x.OnNewSequence += null, null, sentence1);
            _sink.Raise(x => x.OnNewSequence += null, null, sentence2);

            var route = _cache.GetCurrentRoute();
            Assert.Equal(2, route.Count);
            Assert.Equal("RT", route[0].RouteName);
            Assert.Equal(2, route[0].TotalPointsInRoute);
            Assert.Equal(0, route[0].IndexInRoute);
            Assert.Equal("A", route[0].WaypointName);
            Assert.Equal("B", route[1].WaypointName);
            Assert.Equal(1, route[1].IndexInRoute);
            Assert.NotEqual(0, route[0].Position.Latitude);
        }

        [Fact]
        public void GetCompleteRouteOneElement2()
        {
            // Even with three times the same message, this should return just one route
            var sentence1 = new Route("RT", 1, 1, new List<string>() { "A", "B" });
            _sink.Raise(x => x.OnNewSequence += null, null, sentence1);
            _sink.Raise(x => x.OnNewSequence += null, null, sentence1);
            _sink.Raise(x => x.OnNewSequence += null, null, sentence1);

            var route = _cache.GetCurrentRoute();
            Assert.Equal(2, route.Count);
        }

        [Fact]
        public void GetCompleteRouteTwoElements()
        {
            // Even with three times the same message, this should return just one route
            var sentence1 = new Route("RT", 2, 1, new List<string>() { "A", "B" });
            var sentence2 = new Route("RT", 2, 2, new List<string>() { "C", "D" });
            _sink.Raise(x => x.OnNewSequence += null, null, sentence1);
            _sink.Raise(x => x.OnNewSequence += null, null, sentence2);
            _sink.Raise(x => x.OnNewSequence += null, null, sentence1);

            var route = _cache.GetCurrentRoute();
            Assert.Equal(4, route.Count);
            Assert.Equal("RT", route[0].RouteName);
            Assert.Equal(4, route[0].TotalPointsInRoute);
            Assert.Equal(0, route[0].IndexInRoute);
            Assert.Equal("A", route[0].WaypointName);
            Assert.Equal("B", route[1].WaypointName);
            Assert.Equal(1, route[1].IndexInRoute);
        }

        [Fact]
        public void GetNewestRoute()
        {
            // The latest route is interesting
            var sentence1 = new Route("RTOld", 1, 1, new List<string>() { "A", "B" });
            var sentence2 = new Route("RTNew", 1, 1, new List<string>() { "C", "D" });
            _sink.Raise(x => x.OnNewSequence += null, null, sentence1);
            _sink.Raise(x => x.OnNewSequence += null, null, sentence2);

            var route = _cache.GetCurrentRoute();
            Assert.Equal(2, route.Count);
            Assert.Equal("RTNew", route[0].RouteName);
        }
    }
}
