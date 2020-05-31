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

        /// <summary>
        /// Last "origin" position. Used if the current route does not specify one.
        /// Assumed to be the position the user last hit "Goto" on the GPS, without explicitly defining a route.
        /// </summary>
        private RoutePoint _currentOrigin;

        private RoutePoint _knownNextWaypoint;

        public AutopilotController(SentenceCache sentenceCache, NmeaSinkAndSource output)
        {
            _output = output;
            _cache = sentenceCache;
            _threadRunning = false;
            _currentOrigin = null;
            _knownNextWaypoint = null;
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
                if (next.Position != null && next.Position.EqualPosition(_knownNextWaypoint.Position) == false)
                {
                    // the next waypoint changed. Set the new origin (if previous is undefined)
                    // This means that either the user has selected a new route or we moved to the next leg.
                    _knownNextWaypoint = next;
                    _currentOrigin = null;
                }

                RoutePoint previous = currentRoute.Find(x => x.WaypointName == previousWayPoint);
                if (previous == null && next != null)
                {
                    if (_currentOrigin != null)
                    {
                        previous = _currentOrigin;
                    }
                    else
                    {
                        // Assume the current position is the origin
                        GreatCircle.DistAndDir(position, next.Position, out Length distance, out Angle direction);
                        _currentOrigin = new RoutePoint("Manual", 1, 1, "Origin", position, direction,
                            distance);
                    }
                }
                else
                {
                    // We don't need that any more. Reinit when previous is null again
                    _currentOrigin = null;
                }

                Length distanceToNext = Length.Zero;
                Length distanceOnTrackToNext = Length.Zero;
                Length crossTrackError = Length.Zero;
                Length distancePreviousToNext = Length.Zero;
                Angle bearingCurrentToDestination = Angle.Zero;
                Angle bearingOriginToDestination = Angle.Zero;
                GeographicPosition nextPosition = null;
                Speed approachSpeedToWayPoint = Speed.Zero;

                if (next != null && next.Position != null)
                {
                    nextPosition = next.Position;
                    GreatCircle.DistAndDir(position, next.Position, out distanceToNext, out bearingCurrentToDestination);

                    // Either the last waypoint or "origin"
                    if (previous != null && previous.Position != null)
                    {
                        GreatCircle.DistAndDir(position, next.Position, out distancePreviousToNext, out bearingOriginToDestination);
                        GreatCircle.CrossTrackError(previous.Position, next.Position, position, out crossTrackError, out distanceOnTrackToNext);
                    }
                }

                RecommendedMinimumNavToDestination rmb = new RecommendedMinimumNavToDestination(DateTimeOffset.UtcNow,
                    crossTrackError, previousWayPoint, nextWayPoint, nextPosition, distanceToNext, bearingCurrentToDestination,
                    approachSpeedToWayPoint);
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
