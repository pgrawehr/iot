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
    public sealed class MessageRouter
    {
        private Dictionary<string, NmeaParser> _parsedStreams;
        private List<FilterRule> _filterRules;

        public MessageRouter()
        {
            _parsedStreams = new Dictionary<string, NmeaParser>();
            _filterRules = new List<FilterRule>();
        }

        public bool AddStream(string name, NmeaParser parser)
        {
            if (!_parsedStreams.ContainsKey(name))
            {
                _parsedStreams.Add(name, parser);
                parser.OnNewSequence += OnSequenceReceived;
                return true;
            }

            return false;
        }

        private void OnSequenceReceived(NmeaParser source, NmeaSentence sentence)
        {
            foreach (var filter in _filterRules)
            {
                if (filter.SentenceMatch(sentence))
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

        private void SendMessageTo(IEnumerable<NmeaParser> sink, NmeaSentence sentence)
        {
            throw new NotImplementedException();
        }

        public void AddFilterRule(FilterRule rule)
        {
            _filterRules.Add(rule);
        }

        // Todo: Move outside this class
        public class FilterRule
        {
            public FilterRule(string sourceName, TalkerId talkerId, SentenceId sentenceId, StandardFilterAction standardFilterAction)
            {
                SourceName = sourceName;
                TalkerId = talkerId;
                SentenceId = sentenceId;
                StandardFilterAction = standardFilterAction;
            }

            public string SourceName { get; }
            public TalkerId TalkerId { get; }
            public SentenceId SentenceId { get; }
            public StandardFilterAction StandardFilterAction { get; }

            public bool SentenceMatch(NmeaSentence sentence)
            {
                if (sentence.Valid)
                {
                }

                return true;
            }
        }
    }
}
