// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Iot.Device.Nmea0183.Sentences;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
{
    /// <summary>
    ///  Message routing for NMEA messages
    /// </summary>
    public sealed class MessageRouter : NmeaSinkAndSource
    {
        public const string LocalMessageSource = "LOCAL";
        public const string LoggingSinkName = "LOGGER";
        private readonly Dictionary<string, NmeaSinkAndSource> _sourcesAndSinks;
        private List<FilterRule> _filterRules;
        private bool _localInterfaceActive;
        private NmeaSinkAndSource _loggingSink;

        public MessageRouter(LoggingConfiguration? loggingConfiguration = null)
        : base(LocalMessageSource)
        {
            _sourcesAndSinks = new Dictionary<string, NmeaSinkAndSource>();
            // Always add ourselves as message source
            _sourcesAndSinks.Add(LocalMessageSource, this);

            // Also always add a logging sink
            _loggingSink = new LoggingSink(LoggingSinkName, loggingConfiguration);
            _sourcesAndSinks.Add(LoggingSinkName, _loggingSink);
            if (loggingConfiguration != null)
            {
                // Immediately start logger, unless inactive
                _loggingSink.StartDecode();
            }

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

        public bool AddEndPoint(NmeaSinkAndSource parser)
        {
            if (!_sourcesAndSinks.ContainsKey(parser.InterfaceName))
            {
                _sourcesAndSinks.Add(parser.InterfaceName, parser);
                parser.OnNewSequence += OnSequenceReceived;
                // Todo: Also monitor errors, should eventually attempt to reconnect
                return true;
            }

            return false;
        }

        private void OnSequenceReceived(NmeaSinkAndSource source, NmeaSentence sentence)
        {
            // Get name of source for this message
            string name = source.InterfaceName;

            foreach (var filter in _filterRules)
            {
                if (filter.SentenceMatch(name, sentence))
                {
                    SendSentenceToFilterItems(source, sentence, filter);

                    if (!filter.ContinueAfterMatch)
                    {
                        return;
                    }
                }
            }
        }

        private void SendSentenceToFilterItems(NmeaSinkAndSource source, NmeaSentence sentence, FilterRule filter)
        {
            foreach (var sinkName in filter.Sinks)
            {
                if (_sourcesAndSinks.TryGetValue(sinkName, out var sink))
                {
                    // If both source and sink are the local interface, we abort here, as we would be causing a
                    // stack overflow. Note that it is legal to loop messages over Uart interfaces as the TX and RX
                    // line need not be connected to the same physical device. If the receiver is doing the same there,
                    // we'll end up in trouble anyway, though.
                    if (source.InterfaceName == LocalMessageSource && sink.InterfaceName == LocalMessageSource)
                    {
                        continue;
                    }

                    if (filter.ForwardingAction != null)
                    {
                        var newMsg = filter.ForwardingAction(source, sink, sentence);
                        if (newMsg != null)
                        {
                            sink.SendSentence(source, newMsg);
                        }
                    }
                    else
                    {
                        sink.SendSentence(source, sentence);
                    }
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
            foreach (var sinkName in rule.Sinks)
            {
                if (!_sourcesAndSinks.ContainsKey(sinkName))
                {
                    throw new ArgumentException($"Rule contains sink {sinkName} which is unknown.");
                }
            }

            if (rule.SourceName != "*" && !_sourcesAndSinks.ContainsKey(rule.SourceName))
            {
                throw new ArgumentException($"Cannot define a rule for the unknown source {rule.SourceName}.");
            }

            // So we can update the rule list in an atomic operation without requiring a lock
            List<FilterRule> newRules = new List<FilterRule>(_filterRules);
            newRules.Add(rule);
            _filterRules = newRules;
        }

        public override void StartDecode()
        {
            _localInterfaceActive = true;
        }

        public override void SendSentence(NmeaSinkAndSource source, NmeaSentence sentence)
        {
            if (_localInterfaceActive)
            {
                if (source != this)
                {
                    // We're not the source, so this should use the sink here.
                    DispatchSentenceEvents(sentence);
                    return;
                }

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
