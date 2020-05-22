using System;
using System.Collections.Generic;
using System.Text;
using Iot.Device.Nmea0183.Sentences;
using Units;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
{
    public abstract class NmeaSinkAndSource : IDisposable
    {
        public virtual event PositionUpdate OnNewPosition;
        public virtual event Action<DateTimeOffset> OnNewTime;
        public virtual event Action<NmeaSinkAndSource, NmeaSentence> OnNewSequence;
        public virtual event Action<NmeaSinkAndSource, string, NmeaError> OnParserError;

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

        protected void DispatchSentenceEvents(NmeaSentence typedSequence)
        {
            if (typedSequence != null)
            {
                OnNewSequence?.Invoke(this, typedSequence);
            }

            if (typedSequence is RecommendedMinimumNavigationInformation rmc)
            {
                // Todo: This sentence is only interesting if we don't have GGA and VTG
                if (rmc.LatitudeDegrees.HasValue && rmc.LongitudeDegrees.HasValue)
                {
                    GeographicPosition position = new GeographicPosition(rmc.LatitudeDegrees.Value, rmc.LongitudeDegrees.Value, 0);

                    if (rmc.TrackMadeGoodInDegreesTrue.HasValue && rmc.SpeedOverGroundInKnots.HasValue)
                    {
                        OnNewPosition?.Invoke(position, rmc.TrackMadeGoodInDegreesTrue.Value, Speed.FromKnots(rmc.SpeedOverGroundInKnots.Value));
                    }
                    else
                    {
                        OnNewPosition?.Invoke(position, Angle.Zero, Speed.FromKnots(0));
                    }
                }
            }
            else if (typedSequence is TimeDate td)
            {
                if (td.Valid && td.DateTime.HasValue)
                {
                    OnNewTime?.Invoke(td.DateTime.Value);
                }
            }
        }
    }
}
