using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Iot.Device.Nmea0183.Sentences;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
{
    public sealed class AutopilotController : IDisposable
    {
        private readonly NmeaSinkAndSource _output;
        private readonly TimeSpan _loopTime = TimeSpan.FromMilliseconds(200);
        private bool _threadRunning;
        private Thread _updateThread;
        private SentenceCache _cache;

        public AutopilotController(SentenceCache sentenceCache, NmeaSinkAndSource output)
        {
            _output = output;
            _cache = sentenceCache;
            _threadRunning = false;
        }

        public void Start()
        {
            if (_threadRunning)
            {
                return;
            }

            _threadRunning = true;
            _updateThread = new Thread(Loop);
            _updateThread.Start();
        }

        public void Stop()
        {
            if (_updateThread != null)
            {
                _threadRunning = false;
                _updateThread.Join();
                _updateThread = null;
            }
        }

        private void Loop()
        {
            while (_threadRunning)
            {
                CalculateNewStatus();
                Thread.Sleep(_loopTime);
            }
        }

        private void CalculateNewStatus()
        {
            if (!_cache.TryGetLastSentence(RecommendedMinimumNavToDestination.Id, out RecommendedMinimumNavToDestination currentLeg))
            {
                // TODO: Find out ourselves
                Console.WriteLine("No current leg.");
                return;
            }

            if (_cache.TryGetCurrentPosition(out var position, out Angle track, out Speed sog, out Angle? heading))
            {
                List<RoutePoint> currentRoute = _cache.GetCurrentRoute();
                if (currentRoute == null || currentRoute.Count == 0)
                {
                    Console.WriteLine("No route - skipping");
                    return; // Nothing to do - no route
                }

                string previousWayPoint = currentLeg.PreviousWayPointName;
                string nextWayPoint = currentLeg.NextWayPointName;

                RoutePoint next = currentRoute.Find(x => x.WaypointName == nextWayPoint);
                RoutePoint previous = currentRoute.Find(x => x.WaypointName == previousWayPoint);

                Length distanceToNext = Length.Zero;
                Angle bearingCurrentToDestination = Angle.Zero;
                if (next != null && next.Position != null)
                {
                    GreatCircle.DistAndDir(position, next.Position, out double distance, out double direction);
                    distanceToNext = Length.FromMeters(distance);
                    bearingCurrentToDestination = Angle.FromDegrees(direction);
                }
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
