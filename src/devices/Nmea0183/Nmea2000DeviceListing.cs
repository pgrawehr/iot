// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iot.Device.Nmea0183.Sentences;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
{
    public class Nmea2000DeviceListing : NmeaSinkAndSource
    {
        private DateTimeOffset _lastUpdate;
        private ConcurrentDictionary<uint, IsoAddressClaim> _devices;

        public Nmea2000DeviceListing(string interfaceName)
            : base(interfaceName)
        {
            _lastUpdate = DateTimeOffset.UnixEpoch;
            _devices = new ConcurrentDictionary<uint, IsoAddressClaim>();
            UpdateInterval = TimeSpan.FromMinutes(10);
        }

        public TimeSpan UpdateInterval
        {
            get;
            set;
        }

        public override void StartDecode()
        {
        }

        public override void SendSentence(NmeaSinkAndSource source, NmeaSentence sentence)
        {
            // Received a sentence (from the component's point of view)
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (now - _lastUpdate > UpdateInterval)
            {
                DispatchSentenceEvents(new IsoRequest(IsoAddressClaim.HexId));
                _lastUpdate = now;
            }

            if (sentence is IsoAddressClaim claim)
            {
                _devices[claim.MessageSource] = claim;
            }
        }

        public List<Nmea2000DeviceInformation> GetDeviceList()
        {
            var ret = _devices.Select(x => new Nmea2000DeviceInformation(x.Value))
                .OrderBy(y => y.BusAddress)
                .ToList();
            return ret;
        }

        public override void StopDecode()
        {
        }
    }
}
