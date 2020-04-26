using System;
using System.Collections.Generic;
using System.Text;
using Iot.Device.Nmea0183;
using Nmea0183.Sentences;

namespace Nmea0183
{
    /// <summary>
    /// A filter rule for the <see cref="MessageRouter"/>.
    /// </summary>
    public class FilterRule
    {
        private readonly bool _rawMessagesOnly;

        /// <summary>
        /// A standard filter rule
        /// </summary>
        /// <param name="sourceName">Name of the source (Nmea stream name) for which the filter applies or * for all</param>
        /// <param name="talkerId">TalkerId for which the rule applies or <see cref="TalkerId.Any"/></param>
        /// <param name="sentenceId">SentenceId for which the rule applies or <see cref="SentenceId.Any"/></param>
        /// <param name="standardFilterAction">Action to perform when the filter matches</param>
        /// <param name="rawMessagesOnly">The filter matches raw messages only. This is the default, because otherwise known message
        /// types would be implicitly duplicated on forwarding</param>
        public FilterRule(string sourceName, TalkerId talkerId, SentenceId sentenceId, StandardFilterAction standardFilterAction, bool rawMessagesOnly = true)
        {
            _rawMessagesOnly = rawMessagesOnly;
            SourceName = sourceName;
            TalkerId = talkerId;
            SentenceId = sentenceId;
            StandardFilterAction = standardFilterAction;
        }

        /// <summary>
        /// Name of the source for which this filter shall apply
        /// </summary>
        public string SourceName { get; }

        /// <summary>
        /// TalkerId for which this filter applies
        /// </summary>
        public TalkerId TalkerId { get; }

        /// <summary>
        /// SentenceId for which this filter applies
        /// </summary>
        public SentenceId SentenceId { get; }

        /// <summary>
        /// Action this filter performs
        /// </summary>
        public StandardFilterAction StandardFilterAction { get; }

        /// <summary>
        /// True if this filter matches the given sentence and source
        /// </summary>
        public bool SentenceMatch(string nmeaSource, NmeaSentence sentence)
        {
            if (sentence.Valid)
            {
                if (!(sentence is RawSentence) && _rawMessagesOnly)
                {
                    // Non-raw sentences are thrown away by default
                    return false;
                }

                if (TalkerId != TalkerId.Any)
                {
                    if (TalkerId != sentence.TalkerId)
                    {
                        return false;
                    }
                }

                if (SentenceId != Iot.Device.Nmea0183.SentenceId.Any)
                {
                    if (SentenceId != sentence.SentenceId)
                    {
                        return false;
                    }
                }

                if (SourceName != "*" && !string.IsNullOrWhiteSpace(SourceName))
                {
                    if (SourceName != nmeaSource)
                    {
                        return false;
                    }
                }

                return true;
            }

            // Invalid sentences can't be routed anywhere
            return false;
        }
    }
}
