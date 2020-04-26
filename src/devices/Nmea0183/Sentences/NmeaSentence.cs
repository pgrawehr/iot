using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Iot.Device.Nmea0183.Sentences
{
    /// <summary>
    /// Base class for Nmea Sentences
    /// </summary>
    public abstract class NmeaSentence
    {
        /// <summary>
        /// The julian calendar (the one that most of the world uses)
        /// </summary>
        protected static Calendar gregorianCalendar = new GregorianCalendar(GregorianCalendarTypes.USEnglish);

        private static TalkerId _ownTalkerId = TalkerId.ElectronicChartDisplayAndInformationSystem;

        /// <summary>
        /// Our own talker ID (default when we send messages ourselves)
        /// </summary>
        public static TalkerId OwnTalkerId
        {
            get
            {
                return _ownTalkerId;
            }
            set
            {
                _ownTalkerId = value;
            }
        }

        /// <summary>
        /// Constructs an instance of this abstract class
        /// </summary>
        /// <param name="talker">The talker (sender) of this message</param>
        /// <param name="id">Sentence Id</param>
        /// <param name="time">Date/Time this message was valid (derived from last time message)</param>
        protected NmeaSentence(TalkerId talker, SentenceId id, DateTimeOffset time)
        {
            SentenceId = id;
            TalkerId = talker;
            DateTime = time;
        }

        /// <summary>
        /// The talker (sender) of this message
        /// </summary>
        public TalkerId TalkerId
        {
            get;
        }

        /// <summary>
        /// The sentence Id of this packet
        /// </summary>
        public SentenceId SentenceId
        {
            get;
        }

        /// <summary>
        /// The time tag on this message
        /// </summary>
        public DateTimeOffset? DateTime
        {
            get;
            protected set;
        }

        /// <summary>
        /// True if the contents of this message are valid / understood
        /// This is false if the message type could be decoded, but the contents seem invalid or there's no useful data
        /// </summary>
        public bool Valid
        {
            get;
            protected set;
        }

        /// <summary>
        /// Decodes the next field into a string
        /// </summary>
        protected string ReadString(IEnumerator<string> field)
        {
            if (!field.MoveNext())
            {
                return string.Empty;
            }

            return field.Current;
        }

        /// <summary>
        /// Decodes the next field into a char
        /// </summary>
        protected char? ReadChar(IEnumerator<string> field)
        {
            string val = ReadString(field);
            return string.IsNullOrEmpty(val) ? (char?)null : val.Single();
        }

        /// <summary>
        /// Decodes the next field into a double
        /// </summary>
        protected double? ReadValue(IEnumerator<string> field)
        {
            string val = ReadString(field);
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            else
            {
                return double.Parse(val, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Decodes the next field into an int
        /// </summary>
        protected int? ReadInt(IEnumerator<string> field)
        {
            string val = ReadString(field);
            if (string.IsNullOrEmpty(val))
            {
                return null;
            }
            else
            {
                return int.Parse(val, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Parses a date and a time field or any possible combinations of those
        /// </summary>
        protected static DateTimeOffset ParseDateTime(string date, string time)
        {
            DateTimeOffset d1;
            TimeSpan t1;

            if (time.Length != 0)
            {
                // DateTimeOffset.Parse often fails for no apparent reason
                int hour = int.Parse(time.Substring(0, 2), CultureInfo.InvariantCulture);
                int minute = int.Parse(time.Substring(2, 2), CultureInfo.InvariantCulture);
                int seconds = int.Parse(time.Substring(4, 2), CultureInfo.InvariantCulture);
                double millis = double.Parse("0" + time.Substring(6), CultureInfo.InvariantCulture) * 1000;
                t1 = new TimeSpan(0, hour, minute, seconds, (int)millis);
            }
            else
            {
                t1 = new TimeSpan();
            }

            if (date.Length != 0)
            {
                d1 = DateTimeOffset.ParseExact(date, "ddMMyy", null);
            }
            else
            {
                d1 = DateTimeOffset.Now.Date;
            }

            return new DateTimeOffset(d1.Year, d1.Month, d1.Day, t1.Hours, t1.Minutes, t1.Seconds, t1.Milliseconds, gregorianCalendar, TimeSpan.Zero);
        }

        /// <summary>
        /// Parses a date and a time field or any possible combinations of those
        /// </summary>
        protected static DateTimeOffset ParseDateTime(DateTimeOffset lastSeenDate, string time)
        {
            DateTimeOffset dateTime;

            if (time.Length != 0)
            {
                int hour = int.Parse(time.Substring(0, 2));
                int minute = int.Parse(time.Substring(2, 2));
                int seconds = int.Parse(time.Substring(4, 2));
                double millis = double.Parse("0" + time.Substring(6)) * 1000;
                var t1 = new TimeSpan(0, hour, minute, seconds, (int)millis);
                dateTime = lastSeenDate.Date + t1;
            }
            else
            {
                dateTime = lastSeenDate;
            }

            return dateTime;
        }

        /// <summary>
        /// Translates the properties of this instance into an NMEA message body,
        /// without <see cref="TalkerId"/>, <see cref="SentenceId"/> and checksum.
        /// </summary>
        /// <returns></returns>
        public abstract string ToNmeaMessage();

        /// <summary>
        /// Gets an user-readable string about this message
        /// </summary>
        public abstract string ToReadableContent();

        /// <summary>
        /// Generates a readable instance of this string.
        /// Not overridable, use <see cref="ToReadableContent"/> to override.
        /// (this is to prevent confusion with <see cref="ToNmeaMessage"/>.)
        /// </summary>
        /// <returns></returns>
        public sealed override string ToString()
        {
            return ToReadableContent();
        }
    }
}
