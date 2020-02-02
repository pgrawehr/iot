using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Iot.Device.Nmea0183;

namespace Nmea0183.Sentences
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

        /// <summary>
        /// Constructs an instance of this abstract class
        /// </summary>
        /// <param name="id">Sentence Id</param>
        protected NmeaSentence(SentenceId id)
        {
            SentenceId = id;
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
                return double.Parse(val);
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
                int hour = int.Parse(time.Substring(0, 2));
                int minute = int.Parse(time.Substring(2, 2));
                int seconds = int.Parse(time.Substring(4, 2));
                double millis = double.Parse("0" + time.Substring(6)) * 1000;
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
        /// Gets an user-readable string about this message
        /// </summary>
        public abstract string ToReadableContent();
    }
}
