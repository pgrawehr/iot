// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iot.Device.Common;
using Iot.Device.Nmea0183.Ais;
using Iot.Device.Nmea0183.AisSentences;
using Iot.Device.Nmea0183.Sentences;

namespace Iot.Device.Nmea0183
{
    public class AisManager : NmeaSinkAndSource
    {
        private AisParser _aisParser;

        /// <summary>
        /// We keep our own position cache, as we need to calculate CPA and TCPA values.
        /// </summary>
        private SentenceCache _cache;

        private ConcurrentDictionary<uint, Ship> _ships;

        private object _shipLock;

        public AisManager(string interfaceName)
            : base(interfaceName)
        {
            _aisParser = new AisParser();
            _cache = new SentenceCache(this);
            _ships = new ConcurrentDictionary<uint, Ship>();
            _shipLock = new object();
        }

        public override void StartDecode()
        {
        }

        public bool TryGetShip(uint mmsi,
#if NET5_0_OR_GREATER
            [NotNullWhen(true)]
#endif
            out Ship? ship)
        {
            lock (_shipLock)
            {
                return _ships.TryGetValue(mmsi, out ship);
            }
        }

        private Ship GetOrCreateShip(uint mmsi)
        {
            lock (_shipLock)
            {
                if (TryGetShip(mmsi, out Ship? ship))
                {
                    return ship!;
                }

                var ret = new Ship(mmsi);
                _ships.TryAdd(mmsi, ret);
                return ret;
            }
        }

        public IEnumerable<Ship> GetShips()
        {
            lock (_shipLock)
            {
                return _ships.Values;
            }
        }

        public override void SendSentence(NmeaSinkAndSource source, NmeaSentence sentence)
        {
            _cache.Add(sentence);
            AisMessage? msg = _aisParser.Parse(sentence);
            if (msg == null)
            {
                return;
            }

            Ship? ship;
            lock (_shipLock)
            {
                switch (msg.MessageType)
                {
                    case AisMessageType.PositionReportClassA:
                    {
                        PositionReportClassAMessage msgPos = (PositionReportClassAMessage)msg;
                        ship = GetOrCreateShip(msgPos.Mmsi);
                        ship.Position = new GeographicPosition(msgPos.Latitude, msgPos.Longitude, 0);
                        break;
                    }
                }
            }
        }

        public override void StopDecode()
        {
        }
    }
}
