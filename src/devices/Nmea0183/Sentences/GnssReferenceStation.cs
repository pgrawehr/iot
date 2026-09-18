using System;

namespace Iot.Device.Nmea0183.Sentences;

#pragma warning disable CS1591
public sealed class GnssReferenceStation
{
    public GnssReferenceStation(byte referenceStationType, ushort stationId, TimeSpan ageOfCorrections)
    {
        ReferenceStationType = referenceStationType;
        StationId = stationId;
        AgeOfCorrections = ageOfCorrections;
    }

    public byte ReferenceStationType { get; }
    public ushort StationId { get; }
    public TimeSpan AgeOfCorrections { get; }
}
