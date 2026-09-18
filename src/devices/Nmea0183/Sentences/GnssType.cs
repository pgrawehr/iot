namespace Iot.Device.Nmea0183.Sentences;

#pragma warning disable CS1591
public enum GnssType : byte
{
    Gps = 0,
    Glonass = 1,
    GpsGlonass = 2,
    GpsSbasWaas = 3,
    GpsSbasWaasGlonass = 4,
    Chayka = 5,
    Integrated = 6,
    Surveyed = 7,
    Galileo = 8,
}
