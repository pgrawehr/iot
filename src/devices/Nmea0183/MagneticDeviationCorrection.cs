using System;
using System.Collections.Generic;
using System.IO;
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
                    // Add another circle, so we don't have to worry about wraparounds, but this means
                    // that if we sum values that wrap around (i.e. 350 and 5), the result may be 180° off.
                    Angle averageTrack;
                    if (!tracks.TryAverageAngle(out averageTrack))
                    {
                        averageTrack = tracks[0]; // Should be a rare case - just use the first one then
                    }

                    Angle magneticTrack = averageTrack - _magneticVariation; // Now in degrees magnetic
                    magneticTrack = magneticTrack.Normalize(true);
                    // This should be i + 0.5 if the data is good
                    double averageHeading = headings.Sum() / headings.Count;
                    double deviation = (Angle.FromDegrees(averageHeading) - magneticTrack).Normalize(false).Degrees;
                    var pt = new DeviationPoint()
                    {
                        // First is less "true" than second, so CompassReading + Deviation => MagneticHeading
                        CompassReading = (float)magneticTrack.Degrees,
                        MagneticHeading = (float)averageHeading,
                        Deviation = (float)deviation,
                    };

                    circle[i] = pt;
                }
            }

            int numberOfConsecutiveGaps = 0;
            const int maxConsecutiveGaps = 5;
            // Evaluate the quality of the result
            DeviationPoint previous = null;
            for (int i = 0; i < 360; i++)
            {
                var pt = circle[i];
                if (pt == null)
                {
                    numberOfConsecutiveGaps++;
                    if (numberOfConsecutiveGaps > maxConsecutiveGaps)
                    {
                        throw new InvalidDataException($"Not enough data points. There is not enough data near heading {i} degrees");
                    }
                }
                else
                {
                    if (Math.Abs(pt.Deviation) > 30)
                    {
                        throw new InvalidDataException($"Your magnetic compass shows deviations of more than 30 degrees. Use a better installation location or buy a new one.");
                    }

                    numberOfConsecutiveGaps = 0;
                    if (previous != null)
                    {
                        if (Math.Abs(previous.Deviation - pt.Deviation) > 5)
                        {
                            throw new InvalidDataException($"Very big disturbances between similar directions near heading {i}");
                        }
                    }

                    previous = pt;
                }
            }

            _interestingSentences.Clear();
            _magneticVariation = Angle.Zero;
        }

        private void FindAllTracksWith(double direction, out List<Angle> tracks, out List<double> headings)
        {
            tracks = new List<Angle>();
            headings = new List<double>();

            bool useNextTrack = false;
            DateTimeOffset? timeOfHdm = default;
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
                    timeOfHdm = hdm.DateTime;
                }

                // Select the first track after the last matching heading received
                if (s is RecommendedMinimumNavigationInformation rmc && useNextTrack)
                {
                    useNextTrack = false;
                    if (rmc.DateTime - timeOfHdm < TimeSpan.FromSeconds(10))
                    {
                        // Only if this is near the corresponding heading message
                        tracks.Add(rmc.TrackMadeGoodInDegreesTrue);
                    }
                }
            }
        }

        private void MessageFilter(NmeaSinkAndSource nmeaSinkAndSource, NmeaSentence nmeaSentence)
        {
            if (nmeaSentence is RecommendedMinimumNavigationInformation rmc)
            {
                // Track over ground from GPS is useless if not moving
                if (rmc.Valid && rmc.SpeedOverGround > Speed.FromKnots(0.5))
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
