using System;
using System.Collections.Generic;
using System.Text;
using Nmea0183.Sentences;

#pragma warning disable CS1591

namespace Nmea0183
{
    public class NmeaVirtualInterface : NmeaSinkAndSource
    {
        private bool _enabled;

        public NmeaVirtualInterface()
        {
            _enabled = false;
        }

        public override void StartDecode()
        {
            _enabled = true;
        }

        public override void SendSentence(NmeaSentence sentence)
        {
            if (_enabled)
            {
            }
        }

        public override void StopDecode()
        {
            _enabled = false;
        }
    }
}
