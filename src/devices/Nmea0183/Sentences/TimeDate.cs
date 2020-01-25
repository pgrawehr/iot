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

        private bool _validDate;

        /// <summary>
        /// Represents the time of this instance. Returns null if the message did not contain a valid date and time instance
        /// </summary>
        public bool ValidDate
        {
            get
            {
                return _validDate;
            }
        }

        public TimeDate(TalkerSentence sentence, DateTimeOffset time)
            : this(Matches(sentence) ? sentence.Fields : throw new ArgumentException($"SentenceId does not match expected id '{Id}'"), time)
        {
        }

        /// <summary>
        /// Date and time message (ZDA). This should not normally need the last time as argument, because it defines it.
        /// </summary>
        public TimeDate(IEnumerable<string> fields, DateTimeOffset today)
            : base(Id)
        {
            IEnumerator<string> field = fields.GetEnumerator();

            string time = ReadString(field);
            TimeSpan? localTimeOfDay = null;
            if (!string.IsNullOrWhiteSpace(time))
            {
                // Can't use the base class methods here, because we just shouldn't rely on the input variable "today" here, as this message defines the date
                int hour = int.Parse(time.Substring(0, 2));
                int minute = int.Parse(time.Substring(2, 2));
                int seconds = int.Parse(time.Substring(4, 2));
                double millis = double.Parse("0" + time.Substring(6)) * 1000;
                localTimeOfDay = new TimeSpan(0, hour, minute, seconds, (int)millis);
            }

            double year = ReadValue(field) ?? today.Year;
            double month = ReadValue(field) ?? today.Month;
            double day = ReadValue(field) ?? today.Day;
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
            }

            // These may be undefined or zero if the GPS receiver is not receiving valid satellite data (i.e. the receiver works, but there's no antenna connected)
            if (localTimeOfDay.HasValue)
            {
                DateTimeOffset t = new DateTimeOffset((int)year, (int)month, (int)day, localTimeOfDay.Value.Hours, localTimeOfDay.Value.Minutes, localTimeOfDay.Value.Seconds,
                    localTimeOfDay.Value.Milliseconds, gregorianCalendar, TimeSpan.FromHours(offset));
                DateTime = t;
                _validDate = true;
            }
            else
            {
                // Set the reception time anyway, but tell clients that this was not a complete ZDA message
                _validDate = false;
                DateTime = today;
            }
        }

        public TimeDate(DateTimeOffset dateTime)
        : base(Id)
        {
            DateTime = dateTime;
            _validDate = true;
        }

        public override string ToString()
        {
            // seems nullable don't interpolate well
            if (DateTime.HasValue && _validDate)
            {
                var t = DateTime.Value;
                string time = $"{t.ToString("HHmmss.ff")}";
                string year = $"{t.ToString("yyyy")}";
                string month = $"{t.ToString("MM")}";
                string day = $"{t.ToString("dd")}";
                // Return as UTC for now
                return $"{time},{year},{month},{day},00,00";
            }

            return $",,,,00,";
        }
    }
}
