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

        public bool Running
        {
            get
            {
                return _threadRunning && _updateThread != null && _updateThread.IsAlive;
            }
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
            int loops = 0;
            while (_threadRunning)
            {
                CalculateNewStatus(loops, DateTimeOffset.UtcNow);
                loops++;
                Thread.Sleep(_loopTime);
            }
        }

        /// <summary>
        /// Navigation loop. Generally called internally only.
        /// </summary>
        public void CalculateNewStatus(int loops, DateTimeOffset now)
        {
            if (!_cache.TryGetLastSentence(RecommendedMinimumNavToDestination.Id, out RecommendedMinimumNavToDestination currentLeg))
            {
                // TODO: Find out ourselves
                Console.WriteLine("No current leg.");
                return;
            }

            if (!_cache.TryGetLastSentence(HeadingAndDeviation.Id, out HeadingAndDeviation deviation) || !deviation.Variation.HasValue)
            {
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

                RoutePoint next = currentRoute.FirstOrDefault(x => x.WaypointName == nextWayPoint);
                if (next != null && next.Position != null && (_knownNextWaypoint == null || next.Position.EqualPosition(_knownNextWaypoint.Position) == false))
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

                if (next == null)
                {
                    // No next waypoint
                    return;
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
                    approachSpeedToWayPoint = GreatCircle.CalculateVelocityTowardsTarget(next.Position, position, sog, track);

                    // Either the last waypoint or "origin"
                    if (previous != null && previous.Position != null)
                    {
                        GreatCircle.DistAndDir(previous.Position, next.Position, out distancePreviousToNext, out bearingOriginToDestination);
                        GreatCircle.CrossTrackError(previous.Position, next.Position, position, out crossTrackError, out distanceOnTrackToNext);
                    }
                }

                List<NmeaSentence> sentencesToSend = new List<NmeaSentence>();
                RecommendedMinimumNavToDestination rmb = new RecommendedMinimumNavToDestination(now,
                    crossTrackError, previousWayPoint, nextWayPoint, nextPosition, distanceToNext, bearingCurrentToDestination,
                    approachSpeedToWayPoint, currentLeg.Arrived);

                CrossTrackError xte = new CrossTrackError(crossTrackError);

                TrackMadeGood vtg = new TrackMadeGood(track, AngleExtensions.TrueToMagnetic(track, deviation.Variation.Value), sog);

                BearingAndDistanceToWayPoint bwc = new BearingAndDistanceToWayPoint(now, nextWayPoint, nextPosition, distanceToNext,
                    bearingCurrentToDestination, AngleExtensions.TrueToMagnetic(bearingCurrentToDestination, deviation.Variation.Value));

                BearingOriginToDestination bod = new BearingOriginToDestination(bearingOriginToDestination, AngleExtensions.TrueToMagnetic(
                    bearingOriginToDestination, deviation.Variation.Value), previousWayPoint, nextWayPoint);

                sentencesToSend.AddRange(new NmeaSentence[] { rmb, xte, vtg, bwc, bod });

                if (loops % 2 == 0)
                {
                    // Only send these once a second
                    IEnumerable<Route> rte;
                    IEnumerable<WayPoint> wpt;
                    CreateRouteMessages(currentRoute, out rte, out wpt);
                    sentencesToSend.AddRange(wpt);
                    sentencesToSend.AddRange(rte);
                }

                _output.SendSentences(sentencesToSend);
            }
        }

        private bool CreateRouteMessages(List<RoutePoint> currentRoute, out IEnumerable<Route> rte, out IEnumerable<WayPoint> wpt)
        {
            // empty route (but valid message)
            List<Route> route = new List<Route>() { new Route(string.Empty, 1, 1, new List<string>()) };
            List<WayPoint> waypoints = new List<WayPoint>();
            if (currentRoute.Any() == false)
            {
                rte = route;
                wpt = waypoints;
                return false;
            }

            route.Clear();
            List<string> currentRouteElements = new List<string>();
            int totalElements = (int)Math.Ceiling(currentRoute.Count / 3.0);
            foreach (var pt in currentRoute)
            {
                currentRouteElements.Add(pt.WaypointName);
                // Add 3 points to each route message
                if (currentRouteElements.Count >= 3)
                {
                    route.Add(new Route(pt.RouteName, totalElements, route.Count + 1, currentRouteElements));
                    currentRouteElements = new List<string>();
                }

                waypoints.Add(new WayPoint(pt.Position, pt.WaypointName));
            }

            if (currentRouteElements.Any())
            {
                // Remainder
                route.Add(new Route(currentRoute[0].RouteName, totalElements, route.Count + 1, currentRouteElements));
            }

            wpt = waypoints;
            rte = route;
            return true;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
