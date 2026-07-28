// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Iot.Device.Nmea0183.Sentences;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
{
    /// <summary>
    /// Allows translating the input/output of an <see cref="AutopilotController"/> to NMEA2000, so that
    /// the pilot can be controlled from there.
    /// This is for cases when the AP is on the NMEA0183 network and the keypad/plotter is NMEA2000.
    /// It currently emulates an EV1-type autopilot from Raymarine. Unfortunately, the protocol messages
    /// to control autopilots are highly manufacturer-dependent, so that this might not work with non-Raymarine
    /// equipment (e.g. a B&amp;G plotter)
    /// </summary>
    public sealed class Nmea2000AutopilotEmulator : NmeaSinkAndSource
    {
        public string Nmea2000Source { get; }

        public Nmea2000AutopilotEmulator(string interfaceName, string nmea2000Source)
            : base(interfaceName)
        {
            Nmea2000Source = nmea2000Source;
        }

        public override void StartDecode()
        {
        }

        public override void StopDecode()
        {
        }

        public override void SendSentence(NmeaSinkAndSource source, NmeaSentence sentence)
        {
            // We received a sentence, check if we can handle it
            if (source.InterfaceName == Nmea2000Source)
            {
                // We received something from the NMEA2000 interface.
                // Todo...
                return;
            }

            if (sentence is HeadingAndTrackControlStatus st)
            {
                // NMEA0183 autopilot status received.
                // Send out an NMEA2000 autopilot status
                SeatalkNgPilotStatus pilotStatus = new SeatalkNgPilotStatus(st.PilotStatus);
                DispatchSentenceEvents(pilotStatus);

                SeatalkNgPilotHeading pilotHeading = new SeatalkNgPilotHeading(null, st.ActualHeading);
                DispatchSentenceEvents(pilotHeading);
                SeatalkNgPilotLockedHeading lockedHeading = new SeatalkNgPilotLockedHeading(null, st.DesiredHeading);
                DispatchSentenceEvents(lockedHeading);
            }
        }
    }
}
