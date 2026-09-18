namespace Iot.Device.Nmea0183.Sentences;

#pragma warning disable CS1591
public enum GnssIntegrity : byte
{
    NoIntegrityChecking = 0,
    Safe = 1,
    Caution = 2,
}
