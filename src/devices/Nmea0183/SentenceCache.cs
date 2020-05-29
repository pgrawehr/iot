using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Iot.Device.Nmea0183.Sentences;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
{
    /// <summary>
    /// Caches the last sentence(s) of each type
    /// </summary>
    public sealed class SentenceCache : IDisposable
    {
        private readonly NmeaSinkAndSource _source;
        private readonly object _lock;
        private Dictionary<SentenceId, NmeaSentence> _sentences;
        private Queue<Route> _lastRouteSentences;
        private Dictionary<string, WayPoint> _wayPoints;

        private SentenceId[] _groupSentences = new SentenceId[]
        {
            // These sentences come in groups
            new SentenceId("GSV"),
            new SentenceId("RTE"),
            new SentenceId("WPL"),
        };

        public SentenceCache(NmeaSinkAndSource source)
        {
            _source = source;
            _lock = new object();
            _sentences = new Dictionary<SentenceId, NmeaSentence>();
            _lastRouteSentences = new Queue<Route>();
            _wayPoints = new Dictionary<string, WayPoint>();
            _source.OnNewSequence += OnNewSequence;
        }

        public void Clear()
        {
            lock (_lock)
            {
                _sentences.Clear();
                _lastRouteSentences.Clear();
                _wayPoints.Clear();
            }
        }

        /// <summary>
        /// Gets the last sentence of the given type.
        /// Does not return sentences that are part of a group (i.e. GSV, RTE)
        /// </summary>
        /// <param name="id">Sentence Id to query</param>
        /// <returns>The last sentence of that type, or null.</returns>
        public NmeaSentence GetLastSentence(SentenceId id)
        {
            lock (_lock)
            {
                if (_sentences.TryGetValue(id, out var sentence))
                {
                    return sentence;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets the last sentence of the given type.
        /// Does not return sentences that are part of a group (i.e. GSV, RTE)
        /// </summary>
        /// <param name="id">Sentence Id to query</param>
        /// <param name="maxAge">Maximum age of the sentence</param>
        /// <returns>The last sentence of that type, or null if none was received within the given timespan.</returns>
        public NmeaSentence GetLastSentence(SentenceId id, TimeSpan maxAge)
        {
            lock (_lock)
            {
                if (_sentences.TryGetValue(id, out var sentence))
                {
                    if (sentence.Age < maxAge)
                    {
                        return sentence;
                    }
                }

                return null;
            }
        }

        public List<RoutePoint> GetCurrentRoute()
        {
            List<RoutePoint> route = new List<RoutePoint>();
            List<Route> segments = FindLatestCompleteRoute(out string routeName);
            if (segments == null)
            {
                return null;
            }

            List<string> wpNames = new List<string>();
            foreach (var segment in segments)
            {
                wpNames.AddRange(segment.WaypointNames);
            }

            for (var index = 0; index < wpNames.Count; index++)
            {
                var name = wpNames[index];
                GeographicPosition position = null;
                if (_wayPoints.TryGetValue(name, out var pt))
                {
                    position = pt.Position;
                }

                RoutePoint rpt = new RoutePoint(routeName, index, wpNames.Count, name, position, null, null);
                route.Add(rpt);
            }

            return route;
        }

        private List<Route> FindLatestCompleteRoute(out string routeName)
        {
            List<Route> temp;
            lock (_lock)
            {
                // Newest shall be first in list
                temp = _lastRouteSentences.ToList();
            }

            if (temp.Count == 0)
            {
                routeName = "No route";
                return null;
            }

            // Last sentence, take this as the header for what we combine
            var head = temp.First();
            int numberOfSequences = head.TotalSequences;
            routeName = head.RouteName;

            Route[] elements = new Route[numberOfSequences + 1]; // Use 1-based indexing
            bool complete = false;
            foreach (var sentence in temp)
            {
                if (sentence.RouteName == routeName && sentence.Sequence <= numberOfSequences)
                {
                    // Iterate until we found one of each of the components of route
                    elements[sentence.Sequence] = sentence;
                    complete = true;
                    for (int i = 1; i <= numberOfSequences; i++)
                    {
                        if (elements[i] == null)
                        {
                            complete = false;
                        }
                    }

                    if (complete)
                    {
                        break;
                    }
                }
            }

            if (!complete)
            {
                // The route was incomplete, or what we assumed to be the head doesn't match up with the
                // entries (possibly because the route just changed and the new one is not yet complete)
                return null;
            }

            for (var index = 1; index <= elements.Length; index++)
            {
                var elem = elements[index];
                temp.Add(elem);
            }

            return temp.OrderBy(x => x.Sequence).ToList();
        }

        private void OnNewSequence(NmeaSinkAndSource source, NmeaSentence sentence)
        {
            lock (_lock)
            {
                if (!_groupSentences.Contains(sentence.SentenceId))
                {
                    // Standalone sequences. Only the last message needs to be kept
                    _sentences[sentence.SentenceId] = sentence;
                }
                else if (sentence.SentenceId == Route.Id && (sentence is Route rte))
                {
                    _lastRouteSentences.Enqueue(rte);
                    while (_lastRouteSentences.Count > 100)
                    {
                        // Throw away old entry
                        _lastRouteSentences.Dequeue();
                    }
                }
                else if (sentence.SentenceId == WayPoint.Id && (sentence is WayPoint wpt))
                {
                    // No reason to clean this up, this will never grow larger than a few hundred entries
                    _wayPoints[wpt.Name] = wpt;
                }

                // GSV would be special, too. But we're currently not supporting it
            }
        }

        public void Dispose()
        {
            _source.OnNewSequence -= OnNewSequence;
            _sentences.Clear();
        }
    }
}
