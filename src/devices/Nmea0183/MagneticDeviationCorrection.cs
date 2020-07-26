using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
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
        private DeviationPoint[] _deviationPointsToCompassReading;
        private DeviationPoint[] _deviationPointsFromCompassReading;
        private Identification _identification;

        public MagneticDeviationCorrection()
        {
            _interestingSentences = new List<NmeaSentence>();
            _magneticVariation = Angle.Zero;
            _identification = null;
        }

        public Identification Identification
        {
            get
            {
                return _identification;
            }
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
            string[] pointsWithProblems = new string[360];
            // This will get the average offset, which is assumed to be orientation dependent (i.e. if the magnetic compass's forward
            // direction doesn't properly align with the ship)
            double averageOffset = 0;
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

                    averageOffset += deviation;
                    circle[i] = pt;
                }
            }

            averageOffset /= circle.Count(x => x != null);
            int numberOfConsecutiveGaps = 0;
            const int maxConsecutiveGaps = 5;
            // Evaluate the quality of the result
            DeviationPoint previous = null;
            double maxLocalChange = 0;
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
                    if (Math.Abs(pt.Deviation - averageOffset) > 30)
                    {
                        pointsWithProblems[i] = ($"Your magnetic compass shows deviations of more than 30 degrees. Use a better installation location or buy a new one.");
                    }

                    numberOfConsecutiveGaps = 0;
                    if (previous != null)
                    {
                        if (Math.Abs(previous.Deviation - pt.Deviation) > maxLocalChange)
                        {
                            maxLocalChange = Math.Abs(previous.Deviation - pt.Deviation);
                            pointsWithProblems[i] = $"Big deviation change near heading {i}";
                        }
                    }

                    previous = pt;
                }
            }

            for (int i = 0; i < 360; i++)
            {
                if (pointsWithProblems[i] != null)
                {
                    circle[i] = null;
                }
            }

            // Validate again
            for (int i = 0; i < 360; i++)
            {
                var pt = circle[i];
                if (pt == null)
                {
                    numberOfConsecutiveGaps++;
                    if (numberOfConsecutiveGaps > maxConsecutiveGaps)
                    {
                        throw new InvalidDataException($"Not enough data points after cleanup. There is not enough data near heading {i} degrees");
                    }
                }
                else
                {
                    numberOfConsecutiveGaps = 0;
                }
            }

            CalculateSmoothing(circle);

            _deviationPointsToCompassReading = circle;
            _deviationPointsFromCompassReading = null;

            circle = new DeviationPoint[360];
            for (int i = 0; i < 360; i++)
            {
                var ptToUse =
                    _deviationPointsToCompassReading.FirstOrDefault(x => (int)x.CompassReadingSmooth == i);

                int offs = 1;
                while (ptToUse == null)
                {
                    ptToUse =
                        _deviationPointsToCompassReading.FirstOrDefault(x => (int)x.CompassReadingSmooth == (i + offs) % 360 ||
                                                                             (int)x.CompassReadingSmooth == (i + 360 - offs) % 360);
                }

                circle[i] = new DeviationPoint()
                {
                    CompassReading = ptToUse.CompassReading,
                    CompassReadingSmooth = ptToUse.CompassReadingSmooth,
                    Deviation = ptToUse.Deviation,
                    DeviationSmooth = ptToUse.DeviationSmooth,
                    MagneticHeading = ptToUse.MagneticHeading
                };
            }

            _deviationPointsFromCompassReading = circle;

            // Now create the inverse of the above map, to get from compass reading back to undisturbed magnetic heading
            _interestingSentences.Clear();
            _magneticVariation = Angle.Zero;
        }

        private static void CalculateSmoothing(DeviationPoint[] circle)
        {
            for (int i = 0; i < 360; i++)
            {
                const int smoothingPoints = 10; // each side
                double avgDeviation = 0;
                int usedPoints = 0;
                for (int k = i - smoothingPoints; k <= i + smoothingPoints; k++)
                {
                    var ptIn = circle[(k + 360) % 360];
                    if (ptIn != null)
                    {
                        avgDeviation += ptIn.Deviation;
                        usedPoints++;
                    }
                }

                avgDeviation /= usedPoints;
                if (circle[i] != null)
                {
                    circle[i].DeviationSmooth = (float)avgDeviation;
                    // The compass reading we get if we apply the smoothed deviation
                    circle[i].CompassReadingSmooth = (float)Angle.FromDegrees((-1 * (avgDeviation - (i + 0.5)))).Normalize(true).Degrees;
                }
                else
                {
                    // The value that we would have read if we had a value for this direction
                    Angle expectedReading = Angle.FromDegrees((-1 * (avgDeviation - (i + 0.5)))).Normalize(true);
                    circle[i] = new DeviationPoint()
                    {
                        CompassReading = (float)expectedReading.Degrees, // Constructed from the result
                        CompassReadingSmooth = (float)expectedReading.Degrees,
                        MagneticHeading = (i + 0.5f),
                        Deviation = (float)avgDeviation,
                        DeviationSmooth = (float)avgDeviation
                    };
                }
            }
        }

        public void Save(string file, string shipName, string callSign, string mmsi)
        {
            CompassCalibration topLevel = new CompassCalibration();
            var id = new Identification
            {
                CalibrationDate = DateTime.Today, ShipName = shipName, Callsign = callSign, MMSI = mmsi
            };
            topLevel.CalibrationDataToCompassReading = _deviationPointsToCompassReading;
            topLevel.CalibrationDataFromCompassReading = _deviationPointsFromCompassReading;
            topLevel.Identification = id;

            _identification = id;
            XmlSerializer ser = new XmlSerializer(topLevel.GetType());

            using (StreamWriter tw = new StreamWriter(file))
            {
                ser.Serialize(tw, topLevel);
                tw.Close();
                tw.Dispose();
            }
        }

        public void Load(string file)
        {
            XmlSerializer ser = new XmlSerializer(typeof(CompassCalibration));

            CompassCalibration topLevel = null;

            using (StreamReader tw = new StreamReader(file))
            {
                topLevel = (CompassCalibration)ser.Deserialize(tw);
                tw.Close();
                tw.Dispose();
            }

            _identification = topLevel.Identification;
            _deviationPointsToCompassReading = topLevel.CalibrationDataToCompassReading;
            _deviationPointsFromCompassReading = topLevel.CalibrationDataFromCompassReading;
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

        /// <summary>
        /// Convert a magnetic heading to a compass reading (to tell the helmsman what he should steer on the compass for the desired course)
        /// </summary>
        /// <param name="magneticHeading">Magnetic heading input</param>
        /// <returns>The compass reading for the given magnetic heading</returns>
        public Angle FromMagneticHeading(Angle magneticHeading)
        {
            int ptIndex = (int)(magneticHeading.Normalize(true).Degrees);
            var ptToUse = _deviationPointsToCompassReading[ptIndex];
            return (magneticHeading - Angle.FromDegrees(ptToUse.DeviationSmooth)).Normalize(true);
        }

        /// <summary>
        /// Convert a compass reading to a magnetic heading
        /// </summary>
        /// <param name="compassReading">Reading of the compass</param>
        /// <returns>The corrected magnetic heading</returns>
        public Angle ToMagneticHeading(Angle compassReading)
        {
            int ptIndex = (int)(compassReading.Normalize(true).Degrees);
            var ptToUse = _deviationPointsFromCompassReading[ptIndex];
            return (compassReading + Angle.FromDegrees(ptToUse.DeviationSmooth)).Normalize(true);
        }
    }
}
