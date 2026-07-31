using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iot.Device.Common;
using Iot.Device.Nmea0183;
using Iot.Device.Nmea0183.Sentences;
using UnitsNet;
using Xunit;
using Xunit.Abstractions;

namespace DisplayController.Tests
{
    public class EngineDataVerification
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public EngineDataVerification(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void CheckEngineDataFile()
        {
            string file = "C:\\projects\\ShipLogs\\Engine.txt";
            PersistenceFile pf = new PersistenceFile(file);
            PersistentTimeSpan ts = new PersistentTimeSpan(pf, "Operating Hours", TimeSpan.Zero, TimeSpan.FromMinutes(1));
            var allValues = ts.GetAllValues();
            var prev = allValues[0];
            List<(DateTime, TimeSpan)> suspiciousDelta = new List<(DateTime, TimeSpan)>();
            foreach (var e in allValues)
            {
                TimeSpan deltaBetweenWrites = e.TimeStamp - prev.TimeStamp;
                TimeSpan deltaBetweenValues = e.Element - prev.Element;
                prev = e;
                if (deltaBetweenValues < deltaBetweenWrites && (deltaBetweenValues - TimeSpan.FromMinutes(1).Duration() > TimeSpan.FromSeconds(5)))
                {
                    _testOutputHelper.WriteLine($"At {e.TimeStamp}: Actual delta: {deltaBetweenWrites}, measured delta: {deltaBetweenValues}");
                }
            }
        }

        [Fact]
        public void ReadEngineHoursFromLogFiles()
        {
            LoggingConfiguration config = new LoggingConfiguration()
            {
                Path = "c:\\projects\\ShipLogs", MaxFileSize = 1024 * 1024 * 10, SortByDate = true
            };
            List<String> logFiles = NmeaLogDataReader.GetAllLogFilesInFolder(config);
            logFiles = logFiles.Where(x => x.Contains("Nmea-2026", StringComparison.OrdinalIgnoreCase)).ToList();
            using NmeaLogDataReader reader = new NmeaLogDataReader("Reader",
                logFiles, new Nmea0183ParserFactory());

            DateTimeOffset previousRpmPacketTime = default;
            DateTimeOffset now = new DateTimeOffset(2026, 06, 01, 0, 0, 0, TimeSpan.Zero);
            bool engineOn = false;
            DateTimeOffset switchOnTime = default;
            TimeSpan engineOnTime = TimeSpan.Parse("12:16:07:32.0240000", CultureInfo.InvariantCulture);
            reader.OnNewSequence += (source, sentence) =>
            {
                if (sentence is TimeDate zda)
                {
                    now = sentence.DateTime;
                }

                if (sentence is SeaSmartEngineFast rpm)
                {
                    if (previousRpmPacketTime.Ticks == 0)
                    {
                        previousRpmPacketTime = now;
                    }

                    if (rpm.RotationalSpeed > RotationalSpeed.Zero)
                    {
                        if (engineOn == false)
                        {
                            switchOnTime = now;
                            // _testOutputHelper.WriteLine($"Engine switched on at {now}");
                        }
                        engineOn = true;
                        TimeSpan timeSinceLastPacket = now - previousRpmPacketTime;
                        engineOnTime += timeSinceLastPacket;
                    }
                    else
                    {
                        if (engineOn == true)
                        {
                            TimeSpan ranFor = now - switchOnTime;
                            if (ranFor.TotalMinutes > 2)
                            {
                                _testOutputHelper.WriteLine(
                                    $"Engine switched off at {now}. It ran for {ranFor.TotalMinutes:F0} minutes");
                            }
                        }
                        engineOn = false;
                    }

                    previousRpmPacketTime = now;
                }
            };

            reader.StartDecode();
            reader.StopDecode();

            _testOutputHelper.WriteLine($"Computed engine runtime during these logs: {engineOnTime}");
        }
    }
}
