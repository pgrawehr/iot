﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnitsNet;

namespace Iot.Device.Nmea0183.Sentences
{
    /// <summary>
    /// Reading of a sensor, not covered by other sentences.
    /// This can include environmental values (temperature, pressure), tank levels, etc.
    /// </summary>
    public class TransducerMeasurement : NmeaSentence
    {
        /// <summary>
        /// This sentence's id
        /// </summary>
        public static SentenceId Id => new SentenceId("XDR");
        private static bool Matches(SentenceId sentence) => Id == sentence;
        private static bool Matches(TalkerSentence sentence) => Matches(sentence.Id);

        private readonly List<TransducerDataSet> _dataSets;

        /// <summary>
        /// Constructs a new basic XDR sentence with a single data set.
        /// </summary>
        public TransducerMeasurement(string dataName, string dataType, double value, string unit)
            : base(OwnTalkerId, Id, DateTimeOffset.UtcNow)
        {
            _dataSets = new List<TransducerDataSet>();
            _dataSets.Add(new TransducerDataSet()
            {
                DataName = dataName ?? throw new ArgumentNullException(nameof(dataName)),
                DataType = dataType ?? throw new ArgumentNullException(nameof(dataType)),
                Value = value,
                Unit = unit ?? throw new ArgumentNullException(nameof(unit))
            });
            Valid = true;
        }

        /// <summary>
        /// Constructs a new basic XDR sentence with a list of data sets
        /// </summary>
        public TransducerMeasurement(IEnumerable<TransducerDataSet> dataSets)
            : base(OwnTalkerId, Id, DateTimeOffset.UtcNow)
        {
            if (dataSets == null)
            {
                throw new ArgumentNullException(nameof(dataSets));
            }

            _dataSets = new List<TransducerDataSet>(dataSets);
            if (_dataSets.Count >= 1 && _dataSets.Count < 10)
            {
                Valid = true;
            }
            else
            {
                Valid = false;
            }
        }

        /// <summary>
        /// Decodes an XDR sentence
        /// </summary>
        public TransducerMeasurement(TalkerSentence sentence, DateTimeOffset time)
            : this(sentence.TalkerId, Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        /// <summary>
        /// Decodes an XDR sentence
        /// </summary>
        internal TransducerMeasurement(TalkerId talkerId, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, Id, time)
        {
            _dataSets = new List<TransducerDataSet>();
            IEnumerator<string> field = fields.GetEnumerator();
            // Rounding is intended. The number of fields must be a multiple of 4, or the remainder is invalid
            int numDataSets = fields.Count() / 4;

            for (int i = 0; i < numDataSets; i++)
            {
                string dataType = ReadString(field) ?? string.Empty;
                double value = ReadValue(field) ?? 0.0;
                string unit = ReadString(field) ?? string.Empty;
                string dataName = ReadString(field) ?? "Unknown";
                var ds = new TransducerDataSet(dataType, value, unit, dataName);
                _dataSets.Add(ds);
            }

            if (_dataSets.Count >= 1)
            {
                Valid = true;
            }
            else
            {
                Valid = false;
            }
        }

        /// <summary>
        /// Return the data sets of this message
        /// </summary>
        public IList<TransducerDataSet> DataSets
        {
            get
            {
                return _dataSets.AsReadOnly();
            }
        }

        /// <summary>
        /// Creates a sentence from roll and pitch angles
        /// </summary>
        /// <param name="roll">Roll angle (positive right or left wing up)</param>
        /// <param name="pitch">Pitch angle (positive nose up)</param>
        /// <returns>A measurement sequence ready to send</returns>
        public static TransducerMeasurement FromRollAndPitch(Angle roll, Angle pitch)
        {
            TransducerDataSet ds1 = new TransducerDataSet("A", roll.Normalize(false).Degrees, "D", "ROLL");
            TransducerDataSet ds2 = new TransducerDataSet("A", pitch.Normalize(false).Degrees, "D", "PITCH");
            return new TransducerMeasurement(new[] { ds1, ds2 });
        }

        /// <summary>
        /// Presents this message as output
        /// </summary>
        public override string ToNmeaMessage()
        {
            if (Valid)
            {
                StringBuilder b = new StringBuilder();
                for (var index = 0; index < _dataSets.Count; index++)
                {
                    TransducerDataSet data = _dataSets[index];
                    b.Append(data.ToString());
                    if (index != _dataSets.Count - 1)
                    {
                        b.Append(",");
                    }
                }

                return b.ToString();
            }

            return string.Empty;
        }

        /// <inheritdoc />
        public override string ToReadableContent()
        {
            if (Valid)
            {
                StringBuilder b = new StringBuilder();
                foreach (var data in _dataSets)
                {
                    b.Append(data.ToReadableContent());
                    b.Append(" ");
                }

                return b.ToString();
            }

            return "No data";
        }
    }
}