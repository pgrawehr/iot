using System;
using System.Collections.Generic;
using System.Globalization;
using Iot.Device.Nmea0183;

#pragma warning disable CS1591
namespace Nmea0183.Sentences
{
    public class TimeDate : NmeaSentence
    {
        public static SentenceId Id => new SentenceId('Z', 'D', 'A');
        private static bool Matches(SentenceId sentence) => Id == sentence;
        private static bool Matches(TalkerSentence sentence) => Matches(sentence.Id);

        public TimeDate(TalkerSentence sentence, DateTimeOffset time)
            : this(sentence.TalkerId, Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        /// <summary>
        /// Date and time message (ZDA). This should not normally need the last time as argument, because it defines it.
        /// </summary>
        public TimeDate(TalkerId talkerId, IEnumerable<string> fields, DateTimeOffset time)
            : base(talkerId, Id, time)
        {
            IEnumerator<string> field = fields.GetEnumerator();

            string timeString = ReadString(field);
            TimeSpan? localTimeOfDay = null;
            if (!string.IsNullOrWhiteSpace(timeString))
            {
                // Can't use the base class methods here, because we just shouldn't rely on the input variable "today" here, as this message defines the date
                int hour = int.Parse(timeString.Substring(0, 2));
                int minute = int.Parse(timeString.Substring(2, 2));
                int seconds = int.Parse(timeString.Substring(4, 2));
                double millis = double.Parse("0" + timeString.Substring(6)) * 1000;
                localTimeOfDay = new TimeSpan(0, hour, minute, seconds, (int)millis);
            }

            double year = ReadValue(field) ?? time.Year;
            double month = ReadValue(field) ?? time.Month;
            double day = ReadValue(field) ?? time.Day;
            // Offset hours and minutes (last two fields, optional and usually 0). Some sources say these fields are first, but that's apparently wrong
            double offset = ReadValue(field) ?? 0.0;
            offset += (ReadValue(field) ?? 0.0) / 60;

            // Some sources say the parameter order is day-month-year, some say it shall be year-month-day. Luckily, the cases are easy to distinguish,
            // since the year is always given as 4-digit number
            if (day > 2000)
            {
                double ytemp = day;
                day = year;
                year = ytemp;
                ReverseDateFormat = true; // If the input format is exchanged, we by default send the message out the same way
            }

            // These may be undefined or zero if the GPS receiver is not receiving valid satellite data (i.e. the receiver works, but there's no antenna connected)
            if (localTimeOfDay.HasValue)
            {
                DateTimeOffset t = new DateTimeOffset((int)year, (int)month, (int)day, localTimeOfDay.Value.Hours, localTimeOfDay.Value.Minutes, localTimeOfDay.Value.Seconds,
                    localTimeOfDay.Value.Milliseconds, gregorianCalendar, TimeSpan.FromHours(offset));
                DateTime = t;
                Valid = true;
            }
            else
            {
                // Set the reception time anyway, but tell clients that this was not a complete ZDA message
                Valid = false;
                DateTime = time;
            }
        }

        public TimeDate(DateTimeOffset dateTime)
        : base(OwnTalkerId, Id, dateTime)
        {
            Valid = true;
        }

        public bool ReverseDateFormat
        {
            get;
            set;
        }

        public override string ToNmeaMessage()
        {
            // seems nullable don't interpolate well
            if (DateTime.HasValue && Valid)
            {
                var t = DateTime.Value;
                string time = $"{t.ToString("HHmmss.fff")}";
                string year = $"{t.ToString("yyyy")}";
                string month = $"{t.ToString("MM")}";
                string day = $"{t.ToString("dd")}";
                string offset = $"{t.Offset.Hours.ToString("00")}";
                if (t.Offset >= TimeSpan.Zero)
                {
                    offset = "+" + offset;
                }
                else
                {
                    offset = "-" + offset;
                }

                string minuteOffset = $"{t.Offset.Minutes.ToString("00")}";

                // Return as UTC for now
                if (ReverseDateFormat)
                {
                    return $"{time},{day},{month},{year},{offset},{minuteOffset}";
                }
                else
                {
                    return $"{time},{year},{month},{day},{offset},{minuteOffset}";
                }

            }

            return $",,,,00,";
        }

        public override string ToReadableContent()
        {
            if (DateTime.HasValue)
            {
                return $"Date/Time: {DateTime.Value:G}";
            }
            else
            {
                return "Unknown date/time";
            }
        }
    }
}
