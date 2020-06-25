using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Iot.Device.Nmea0183.Sentences;
using UnitsNet;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
{
    /// <summary>
    /// Corrects the magnetic deviation of an electronic compass.
    /// This calculates the corrected magnetic heading from the measurement of an actual instrument and vice-versa.
    /// </summary>
    public class MagneticDeviationCorrection
    {
        // Only used temporarily during build of the deviation table
        private List<NmeaSentence> _interestingSentences;
        private Angle _magneticVariation;

        public MagneticDeviationCorrection()
        {
            _interestingSentences = new List<NmeaSentence>();
            _magneticVariation = Angle.Zero;
        }

        public void CreateCorrectionTable(string file)
        {
            CreateCorrectionTable(new[] { file });
        }

        public void CreateCorrectionTable(string[] fileSet)
        {
            foreach (var f in fileSet)
            {
                NmeaLogDataReader reader = new NmeaLogDataReader("Reader", f);
                reader.OnNewSequence += MessageFilter;
                reader.StartDecode();
                reader.Dispose();
            }

            DeviationPoint[] circle = new DeviationPoint[360]; // One entry per degree
            for (int i = 0; i < 360; i++)
            {
                FindAllTracksWith(i, out var tracks, out var headings);
                if (tracks.Count > 0 && headings.Count > 0)
                {
                    // Add another circle, so we don't have to worry about wraparounds
                    double averageTrack = tracks.Sum(x => x + 360.0) / tracks.Count;
                    Angle magneticTrack = Angle.FromDegrees(averageTrack - 360);
                    magneticTrack -= _magneticVariation; // Now in degrees magnetic
                    magneticTrack = magneticTrack.Normalize(true);
                    // This should be i + 0.5 if the data is good
                    double averageHeading = headings.Sum() / headings.Count;
                    double deviation = (magneticTrack.Degrees - averageHeading) % 360;
                    circle[i] = new DeviationPoint()
                    {
                        CompassReading = (float)magneticTrack.Degrees,
                        MagneticHeading = (float)averageHeading,
                        Deviation = (float)deviation,
                    };
                }
            }

            _interestingSentences.Clear();
            _magneticVariation = Angle.Zero;
        }

        private void FindAllTracksWith(double direction, out List<double> tracks, out List<double> headings)
        {
            tracks = new List<double>();
            headings = new List<double>();

            bool useNextTrack = false;
            foreach (var s in _interestingSentences)
            {
                if (s is HeadingMagnetic hdm)
                {
                    if (!hdm.Valid)
                    {
                        continue;
                    }

                    if (Math.Abs(Math.Floor(hdm.Angle.Degrees) - direction) > 1E-8)
                    {
                        continue;
                    }

                    // Now we have an entry hdt that is a true heading more or less pointing to direction
                    headings.Add(hdm.Angle.Degrees);
                    useNextTrack = true;
                }

                // Select the first track after the last matching heading received
                if (s is RecommendedMinimumNavigationInformation rmc && useNextTrack)
                {
                    useNextTrack = false;
                    tracks.Add(rmc.TrackMadeGoodInDegreesTrue.Degrees);
                }
            }
        }

        private void MessageFilter(NmeaSinkAndSource nmeaSinkAndSource, NmeaSentence nmeaSentence)
        {
            if (nmeaSentence is RecommendedMinimumNavigationInformation rmc)
            {
                // Track over ground from GPS is useless if not moving
                if (rmc.Valid && rmc.SpeedOverGround > Speed.FromKnots(1.0))
                {
                    _interestingSentences.Add(rmc);
                    if (rmc.MagneticVariationInDegrees.HasValue)
                    {
                        _magneticVariation = rmc.MagneticVariationInDegrees.Value;
                    }
                }
            }

            if (nmeaSentence is HeadingMagnetic hdm)
            {
                if (hdm.Valid)
                {
                    _interestingSentences.Add(hdm);
                }
            }
        }
    }
}
