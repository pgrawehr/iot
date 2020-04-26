using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Iot.Device.Nmea0183;
using Nmea0183.Sentences;

#pragma warning disable CS1591
namespace Nmea0183
{
    /// <summary>
    ///  Message routing for NMEA messages
    /// </summary>
    public sealed class MessageRouter : NmeaSinkAndSource
    {
        public const string LocalMessageSource = "LOCAL";
        private readonly Dictionary<string, NmeaSinkAndSource> _sourcesAndSinks;
        private readonly List<string> _sourceSinkOrder;
        private List<FilterRule> _filterRules;
        private bool _localInterfaceActive;

        public MessageRouter()
        {
            _sourcesAndSinks = new Dictionary<string, NmeaSinkAndSource>();
            _sourceSinkOrder = new List<string>();
            // Always add ourselves as message source
            _sourcesAndSinks.Add(LocalMessageSource, this);
            _sourceSinkOrder.Add(LocalMessageSource);
            _filterRules = new List<FilterRule>();
            _localInterfaceActive = true;
        }

        public IReadOnlyDictionary<string, NmeaSinkAndSource> EndPoints
        {
            get
            {
                return _sourcesAndSinks;
            }
        }

        public bool AddEndPoint(string name, NmeaSinkAndSource parser)
        {
            if (!_sourcesAndSinks.ContainsKey(name))
            {
                _sourcesAndSinks.Add(name, parser);
                _sourceSinkOrder.Add(name);
                parser.OnNewSequence += OnSequenceReceived;
                // Todo: Also monitor errors, should eventually attempt to reconnect
                return true;
            }

            return false;
        }

        private void OnSequenceReceived(NmeaSinkAndSource source, NmeaSentence sentence)
        {
            // Get name of source for this message (as defined in the AddStream call)
            string name = _sourcesAndSinks.First(x => x.Value == source).Key;
            foreach (var filter in _filterRules)
            {
                if (filter.SentenceMatch(name, sentence))
                {
                    switch (filter.StandardFilterAction)
                    {
                        case StandardFilterAction.DiscardMessage:
                            return;
                        case StandardFilterAction.ForwardToAllOthers:
                            SendMessageTo(_sourcesAndSinks.Values.Where(x => x != source), sentence);
                            return;
                        case StandardFilterAction.ForwardToAll:
                            SendMessageTo(_sourcesAndSinks.Values, sentence);
                            return;
                        case StandardFilterAction.SendBack:
                            SendMessageTo(new[] { source }, sentence);
                            return;
                        case StandardFilterAction.ForwardToLocal:
                            SendMessageTo(_sourcesAndSinks.Where(x => x.Key == LocalMessageSource)
                                .Select(y => y.Value), sentence);
                            return;
                        case StandardFilterAction.ForwardToPrimary:
                            SendMessageTo(_sourcesAndSinks.Where(x => x.Key == _sourceSinkOrder[1])
                                .Select(y => y.Value), sentence);
                            return;
                        case StandardFilterAction.ForwardToSecondary:
                            SendMessageTo(_sourcesAndSinks.Where(x => x.Key == _sourceSinkOrder[2])
                                .Select(y => y.Value), sentence);
                            return;
                        case StandardFilterAction.ForwardToTernary:
                            SendMessageTo(_sourcesAndSinks.Where(x => x.Key == _sourceSinkOrder[3])
                                .Select(y => y.Value), sentence);
                            return;
                    }
                }
            }
        }

        private void SendMessageTo(IEnumerable<NmeaSinkAndSource> sinks, NmeaSentence sentence)
        {
            foreach (var sink in sinks)
            {
                if (sink == this)
                {
                    DispatchSentenceEvents(sentence);
                }
                else
                {
                    sink.SendSentence(sentence);
                }
            }
        }

        /// <summary>
        /// Adds a filter rule to the end of the rule set
        /// </summary>
        /// <param name="rule">The rule to add</param>
        /// <exception cref="ArgumentException">The filter rule cannot be added because it is invalid (i.e. an attempt to
        /// add a rule for an inexistent interface was made)</exception>
        public void AddFilterRule(FilterRule rule)
        {
            if (rule.StandardFilterAction == StandardFilterAction.ForwardToPrimary && _sourceSinkOrder.Count < 2)
            {
                throw new ArgumentException("Cannot create a rule that targets the primary interface without such an interface");
            }

            if (rule.StandardFilterAction == StandardFilterAction.ForwardToSecondary && _sourceSinkOrder.Count < 3)
            {
                throw new ArgumentException("Cannot create a rule that targets the secondary interface without such an interface");
            }

            if (rule.StandardFilterAction == StandardFilterAction.ForwardToTernary && _sourceSinkOrder.Count < 4)
            {
                throw new ArgumentException("Cannot create a rule that targets the ternary interface without such an interface");
            }

            if (rule.SourceName != "*" && !_sourceSinkOrder.Contains(rule.SourceName))
            {
                throw new ArgumentException($"Cannot define a rule for the unknown source {rule.SourceName}.");
            }

            _filterRules.Add(rule);
        }

        public override void StartDecode()
        {
            _localInterfaceActive = true;
        }

        public override void SendSentence(NmeaSentence sentence)
        {
            if (_localInterfaceActive)
            {
                // Forward to routing method with ourselves as source
                OnSequenceReceived(this, sentence);
            }
        }

        public override void StopDecode()
        {
            _localInterfaceActive = false;
        }
    }
}
