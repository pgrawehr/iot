// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Iot.Device.Nmea0183.Sentences;
using Xunit;

namespace Iot.Device.Nmea0183.Tests
{
    /// <summary>
    /// Tests for ISO Address Claim (PGN 60928) message
    /// </summary>
    public class IsoAddressClaimTests : IDisposable
    {
        private DateTimeOffset _lastPacketTime;

        public IsoAddressClaimTests()
        {
            NmeaSentence.OwnTalkerId = NmeaSentence.DefaultTalkerId;
            _lastPacketTime = default;
        }

        public void Dispose()
        {
            NmeaSentence.OwnTalkerId = NmeaSentence.DefaultTalkerId;
        }

        [Fact]
        public void IsoAddressClaimRoundtrip()
        {
            // Create an ISO Address Claim with typical values
            var originalClaim = new IsoAddressClaim(
                uniqueNumber: 123456,
                manufacturerCode: ManufacturerCode.Garmin,
                deviceInstance: 0,
                deviceFunction: DeviceFunction.Navigation, // Navigation
                deviceClass: DeviceClass.NavigationSystems, // Navigation systems
                systemInstance: 0,
                industryCode: IndustryCode.Marine,
                arbitraryAddressCapable: true);

            originalClaim.MessageSource = 0x42;

            // Convert to NMEA message
            string nmeaMessage = originalClaim.ToNmeaMessage();

            Assert.NotNull(nmeaMessage);
            Assert.StartsWith("$PCDIN,", nmeaMessage);

            // Parse it back
            var parsed = TalkerSentence.FromSentenceString(nmeaMessage, out var error);
            Assert.Equal(NmeaError.None, error);
            Assert.NotNull(parsed);

            // Get typed value
            var decodedClaim = (IsoAddressClaim)parsed!.TryGetTypedValue(ref _lastPacketTime)!;

            Assert.NotNull(decodedClaim);
            Assert.True(decodedClaim.Valid);
            Assert.Equal(originalClaim.UniqueNumber, decodedClaim.UniqueNumber);
            Assert.Equal(originalClaim.ManufacturerCode, decodedClaim.ManufacturerCode);
            Assert.Equal(originalClaim.DeviceInstance, decodedClaim.DeviceInstance);
            Assert.Equal(originalClaim.DeviceFunction, decodedClaim.DeviceFunction);
            Assert.Equal(originalClaim.DeviceClass, decodedClaim.DeviceClass);
            Assert.Equal(originalClaim.SystemInstance, decodedClaim.SystemInstance);
            Assert.Equal(originalClaim.IndustryCode, decodedClaim.IndustryCode);
            Assert.Equal(originalClaim.ArbitraryAddressCapable, decodedClaim.ArbitraryAddressCapable);
            Assert.Equal(originalClaim.MessageSource, decodedClaim.MessageSource);
            Assert.Equal(0xEE00u, decodedClaim.Identifier);
        }

        [Fact]
        public void IsoAddressClaimDecode()
        {
            // Example: Address claim from a Raymarine device
            string sentence = "$PCDIN,0EE00,12345678,42,08,F1BF9C3A96502004*12";

            var parsed = TalkerSentence.FromSentenceString(sentence, out var error);
            Assert.Equal(NmeaError.None, error);
            Assert.NotNull(parsed);

            var claim = (IsoAddressClaim)parsed!.TryGetTypedValue(ref _lastPacketTime)!;

            Assert.NotNull(claim);
            Assert.True(claim.Valid);
            Assert.Equal(0x42, claim.MessageSource);
            Assert.Equal(0xEE00u, claim.Identifier);
        }

        [Fact]
        public void IsoAddressClaimEncode()
        {
            var claim = new IsoAddressClaim(
                uniqueNumber: 100000,
                manufacturerCode: ManufacturerCode.Raymarine,
                deviceInstance: 1,
                deviceFunction: DeviceFunction.SteeringAndControlSurfaces,
                deviceClass: DeviceClass.SteeringAndControlSurfaces,
                systemInstance: 0,
                industryCode: IndustryCode.Marine,
                arbitraryAddressCapable: true);

            claim.MessageSource = 0x10;

            string parameters = claim.ToNmeaParameterList();

            // Should contain PGN, timestamp, source, length, and NAME data
            Assert.Contains("0EE00", parameters); // PGN
            Assert.Contains("00000000", parameters); // Timestamp
            Assert.Contains("10", parameters); // Source
        }

        [Fact]
        public void IsoAddressClaimDeviceDescriptions()
        {
            // GPS device
            var gpsClaim = new IsoAddressClaim(
                uniqueNumber: 123456,
                manufacturerCode: ManufacturerCode.Garmin,
                deviceInstance: 0,
                deviceFunction: DeviceFunction.Navigation,
                deviceClass: DeviceClass.NavigationSystems);

            Assert.Equal("GPS", gpsClaim.DeviceDescription);
            Assert.Equal("Navigation", gpsClaim.FunctionDescription);
            Assert.Equal("Navigation systems", gpsClaim.ClassDescription);
        }

        [Fact]
        public void IsoAddressClaimAutopilotDescription()
        {
            // Autopilot device
            var autopilotClaim = new IsoAddressClaim(
                uniqueNumber: 654321,
                manufacturerCode: ManufacturerCode.Raymarine,
                deviceInstance: 0,
                deviceFunction: DeviceFunction.SteeringAndControlSurfaces,
                deviceClass: DeviceClass.SteeringAndControlSurfaces);

            Assert.Equal("Autopilot", autopilotClaim.DeviceDescription);
            Assert.Equal("Steering and Control Surfaces", autopilotClaim.FunctionDescription);
            Assert.Equal("Steering and Control surfaces", autopilotClaim.ClassDescription);
        }

        [Fact]
        public void IsoAddressClaimAISDescription()
        {
            // AIS device
            var aisClaim = new IsoAddressClaim(
                uniqueNumber: 999999,
                manufacturerCode: ManufacturerCode.Garmin,
                deviceInstance: 0,
                deviceFunction: 195,
                deviceClass: DeviceClass.NavigationSystems);

            Assert.Equal("AIS", aisClaim.DeviceDescription);
        }

        [Theory]
        [InlineData(0u, 0)]
        [InlineData(123456u, 0)]
        [InlineData(2097151u, 0)] // Maximum 21-bit value
        [InlineData(100000u, 7)] // Maximum device instance
        public void IsoAddressClaimValidRanges(uint uniqueNumber, byte deviceInstance)
        {
            var claim = new IsoAddressClaim(
                uniqueNumber: uniqueNumber,
                manufacturerCode: ManufacturerCode.Garmin,
                deviceInstance: deviceInstance,
                deviceFunction: 150,
                deviceClass: 60);

            Assert.Equal(uniqueNumber, claim.UniqueNumber);
            Assert.Equal(deviceInstance, claim.DeviceInstance);
            Assert.True(claim.Valid);
        }

        [Fact]
        public void IsoAddressClaimInvalidUniqueNumber()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new IsoAddressClaim(
                    uniqueNumber: 0x200000,
                    manufacturerCode: ManufacturerCode.Garmin,
                    deviceInstance: 0,
                    deviceFunction: 150,
                    deviceClass: 60));
        }

        [Fact]
        public void IsoAddressClaimInvalidDeviceInstance()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new IsoAddressClaim(
                    uniqueNumber: 123456,
                    manufacturerCode: ManufacturerCode.Garmin,
                    deviceInstance: 8,
                    deviceFunction: 150,
                    deviceClass: 60));
        }

        [Fact]
        public void IsoAddressClaimInvalidDeviceClass()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new IsoAddressClaim(
                    uniqueNumber: 123456,
                    manufacturerCode: ManufacturerCode.Garmin,
                    deviceInstance: 0,
                    deviceFunction: 150,
                    deviceClass: 128));
        }

        [Fact]
        public void IsoAddressClaimInvalidSystemInstance()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new IsoAddressClaim(
                    uniqueNumber: 123456,
                    manufacturerCode: ManufacturerCode.Garmin,
                    deviceInstance: 0,
                    deviceFunction: 150,
                    deviceClass: 60,
                    systemInstance: 16));
        }

        [Fact]
        public void IsoAddressClaimArbitraryAddressCapableFalse()
        {
            var claim = new IsoAddressClaim(
                uniqueNumber: 123456,
                manufacturerCode: ManufacturerCode.Garmin,
                deviceInstance: 0,
                deviceFunction: 150,
                deviceClass: 60,
                arbitraryAddressCapable: false);

            Assert.False(claim.ArbitraryAddressCapable);

            // Roundtrip
            string nmeaMessage = claim.ToNmeaMessage();
            var parsed = TalkerSentence.FromSentenceString(nmeaMessage, out var error);
            var decoded = (IsoAddressClaim)parsed!.TryGetTypedValue(ref _lastPacketTime)!;

            Assert.False(decoded.ArbitraryAddressCapable);
        }

        [Fact]
        public void IsoAddressClaimToReadableContentWithDeviceDescription()
        {
            var claim = new IsoAddressClaim(
                uniqueNumber: 123456,
                manufacturerCode: ManufacturerCode.Raymarine,
                deviceInstance: 0,
                deviceFunction: 150,
                deviceClass: 40, // Autopilot
                industryCode: IndustryCode.Marine);

            claim.MessageSource = 0x42;

            string readable = claim.ToReadableContent();

            Assert.Contains("ISO Address Claim", readable);
            Assert.Contains("Autopilot", readable); // Device description
            Assert.Contains("Raymarine", readable);
            Assert.Contains("123456", readable);
            Assert.Contains("Marine", readable);
        }

        [Fact]
        public void IsoAddressClaimReplacesOlderInstance()
        {
            var claim = new IsoAddressClaim(
                uniqueNumber: 123456,
                manufacturerCode: ManufacturerCode.Garmin,
                deviceInstance: 0,
                deviceFunction: 150,
                deviceClass: 60);

            Assert.True(claim.ReplacesOlderInstance);
        }

        [Theory]
        [InlineData(ManufacturerCode.Garmin)]
        [InlineData(ManufacturerCode.Raymarine)]
        [InlineData(ManufacturerCode.Lowrance)]
        [InlineData(ManufacturerCode.BandG)]
        public void IsoAddressClaimDifferentManufacturers(ManufacturerCode manufacturer)
        {
            var claim = new IsoAddressClaim(
                uniqueNumber: 999999,
                manufacturerCode: manufacturer,
                deviceInstance: 0,
                deviceFunction: 150,
                deviceClass: 60);

            Assert.Equal(manufacturer, claim.ManufacturerCode);

            string nmeaMessage = claim.ToNmeaMessage();
            var parsed = TalkerSentence.FromSentenceString(nmeaMessage, out var error);
            var decoded = (IsoAddressClaim)parsed!.TryGetTypedValue(ref _lastPacketTime)!;

            Assert.Equal(manufacturer, decoded.ManufacturerCode);
        }

        [Theory]
        [InlineData(IndustryCode.Marine)]
        [InlineData(IndustryCode.Global)]
        [InlineData(IndustryCode.Highway)]
        [InlineData(IndustryCode.Agriculture)]
        public void IsoAddressClaimDifferentIndustryCodes(IndustryCode industry)
        {
            var claim = new IsoAddressClaim(
                uniqueNumber: 123456,
                manufacturerCode: ManufacturerCode.Garmin,
                deviceInstance: 0,
                deviceFunction: 150,
                deviceClass: 60,
                industryCode: industry);

            Assert.Equal(industry, claim.IndustryCode);

            string nmeaMessage = claim.ToNmeaMessage();
            var parsed = TalkerSentence.FromSentenceString(nmeaMessage, out var error);
            var decoded = (IsoAddressClaim)parsed!.TryGetTypedValue(ref _lastPacketTime)!;

            Assert.Equal(industry, decoded.IndustryCode);
        }
    }
}
