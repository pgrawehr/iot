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
        private Dictionary<string, NmeaSinkAndSource> _parsedStreams;
        private List<FilterRule> _filterRules;
        private bool _localInterfaceActive;

        public MessageRouter()
        {
            _parsedStreams = new Dictionary<string, NmeaSinkAndSource>();
            // Always add ourselves as message source
            _parsedStreams.Add(LocalMessageSource, this);
            _filterRules = new List<FilterRule>();
            _localInterfaceActive = true;
        }

        public IReadOnlyDictionary<string, NmeaSinkAndSource> EndPoints
        {
            get
            {
                return _parsedStreams;
            }
        }

        public bool AddEndPoint(string name, NmeaSinkAndSource parser)
        {
            if (!_parsedStreams.ContainsKey(name))
            {
                _parsedStreams.Add(name, parser);
                parser.OnNewSequence += OnSequenceReceived;
                // Todo: Also monitor errors, should eventually attempt to reconnect
                return true;
            }

            return false;
        }

        private void OnSequenceReceived(NmeaSinkAndSource source, NmeaSentence sentence)
        {
            // Get name of source for this message (as defined in the AddStream call)
            string name = _parsedStreams.First(x => x.Value == source).Key;
            foreach (var filter in _filterRules)
            {
                if (filter.SentenceMatch(name, sentence))
                {
                    switch (filter.StandardFilterAction)
                    {
                        case StandardFilterAction.DiscardMessage:
                            return;
                        case StandardFilterAction.ForwardToAllOthers:
                            SendMessageTo(_parsedStreams.Values.Where(x => x != source), sentence);
                            return;
                        case StandardFilterAction.ForwardToAll:
                            SendMessageTo(_parsedStreams.Values, sentence);
                            return;
                        case StandardFilterAction.SendBack:
                            SendMessageTo(new[] { source }, sentence);
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

        public void AddFilterRule(FilterRule rule)
        {
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
