using System;
using System.Collections.Generic;
using System.Text;
using Iot.Device.Nmea0183.Sentences;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
{
    public abstract class NmeaSinkAndSource : IDisposable
    {
        public virtual event PositionUpdate? OnNewPosition;
        public virtual event Action<DateTimeOffset>? OnNewTime;
        public virtual event Action<NmeaSinkAndSource, NmeaSentence>? OnNewSequence;
        public virtual event Action<NmeaSinkAndSource, string, NmeaError>? OnParserError;

        protected NmeaSinkAndSource(string interfaceName)
        {
            InterfaceName = interfaceName;
        }

        public string InterfaceName
        {
            get;
        }

        public abstract void StartDecode();
        public abstract void SendSentence(NmeaSinkAndSource source, NmeaSentence sentence);

        public void SendSentence(NmeaSentence sentence)
        {
            SendSentence(this, sentence);
        }

        public abstract void StopDecode();

        protected void FireOnParserError(string message, NmeaError error)
        {
            OnParserError?.Invoke(this, message, error);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                StopDecode();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void DispatchSentenceEvents(NmeaSentence? typedSequence)
        {
            if (typedSequence != null)
            {
                OnNewSequence?.Invoke(this, typedSequence);
            }

            if (typedSequence is RecommendedMinimumNavigationInformation rmc && rmc.Valid)
            {
                OnNewPosition?.Invoke(rmc.Position, rmc.TrackMadeGoodInDegreesTrue, rmc.SpeedOverGround);
            }
            else if (typedSequence is TimeDate td)
            {
                if (td.Valid && td.DateTime.HasValue)
                {
                    OnNewTime?.Invoke(td.DateTime.Value);
                }
            }
        }

        public virtual void SendSentences(IEnumerable<NmeaSentence> sentencesToSend)
        {
            foreach (var sentence in sentencesToSend)
            {
                SendSentence(sentence);
            }
        }
    }
}
