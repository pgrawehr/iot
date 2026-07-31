// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Iot.Device.Nmea0183.Sentences;
using Microsoft.Extensions.Logging;
using UnitsNet;
using UnitsNet.Units;

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
        private int _messageCounter;

        public string Nmea2000Source { get; }

        public Nmea2000AutopilotEmulator(string interfaceName, string nmea2000Source)
            : base(interfaceName)
        {
            Nmea2000Source = nmea2000Source;
            _messageCounter = 0;
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
                if (sentence is GroupFunctionMessage gf)
                {
                    if (gf.Pgn == SeatalkNgPilotStatus.HexId && gf.ParameterConstantsMatch() &&
                        gf.Parameters[0].Constant == 1851 && gf.Function == GroupFunction.Command)
                    {
                        int newMode = gf.Parameters[3].Value.GetValueOrDefault();
                        AutopilotStatus desiredStatus = SeatalkNgPilotStatus.AutopilotStatusFromNumber(newMode);
                        Logger.LogInformation($"New status was commanded: {desiredStatus}!");
                        var reply = gf.CreateAck();
                        DispatchSentenceEvents(reply);
                        HeadingAndTrackControl control = new HeadingAndTrackControl(
                            HeadingAndTrackControlStatus.FromAutopilotStatus(desiredStatus),
                            null, string.Empty, string.Empty, null, null, null, null, null, null, null, false);
                        DispatchSentenceEvents(control);
                    }
                    else if (gf.Pgn == SeatalkNgPilotLockedHeading.HexId && gf.ParameterConstantsMatch() &&
                                  gf.Parameters[0].Constant == 1851 && gf.Function == GroupFunction.Command)
                    {
                        double newDirection = gf.Parameters[5].Value.GetValueOrDefault(); // New magnetic heading
                        var currentDesiredHeading = Angle.FromRadians(newDirection * 0.0001).ToUnit(AngleUnit.Degree);
                        Logger.LogInformation($"Updated desired heading to {currentDesiredHeading}");
                        var reply = gf.CreateAck();
                        DispatchSentenceEvents(reply);
                        HeadingAndTrackControl control = new HeadingAndTrackControl(string.Empty,
                            null, string.Empty, string.Empty, null, null, null, null, currentDesiredHeading, null, null, false);
                        DispatchSentenceEvents(control);
                    }
                    else if (gf.Pgn == 126720 && gf.ParameterConstantsMatch() && gf.Function == GroupFunction.Request)
                    {
                        int prop = gf.Parameters[3].Value.GetValueOrDefault();
                        int command = gf.Parameters[4].Value.GetValueOrDefault();
                        if (prop == 108 && command == 38)
                        {
                            SeatalkNgPilotConfigurationValue value = new SeatalkNgPilotConfigurationValue()
                            {
                                Command = 38,
                                ProprietaryId = 108,
                                DateTime = DateTimeOffset.UtcNow,
                                Value = false,
                            };

                            DispatchSentenceEvents(value);
                        }
                        else
                        {
                            var reply = gf.CreateNoAck(x => x.Description == "Command" ? 5 : null);
                            DispatchSentenceEvents(reply);
                        }
                    }
                    else
                    {
                        Logger.LogWarning($"Unknown Group function '{gf.Function}' message about {gf.Pgn}");
                    }
                }

                return;
            }

            if (sentence is HeadingAndTrackControlStatus st)
            {
                Logger.LogInformation($"Received autopilot status message from Seatalk. Status {st.PilotStatus}");
                // NMEA0183 autopilot status received.
                // Send out an NMEA2000 autopilot status
                if (_messageCounter % 3 == 0)
                {
                    SeatalkNgPilotStatus pilotStatus = new SeatalkNgPilotStatus(st.PilotStatus);
                    DispatchSentenceEvents(pilotStatus);
                }
                else if (_messageCounter % 3 == 1)
                {
                    SeatalkNgPilotHeading pilotHeading = new SeatalkNgPilotHeading(null, st.ActualHeading);
                    DispatchSentenceEvents(pilotHeading);
                }
                else
                {
                    SeatalkNgPilotLockedHeading
                        lockedHeading = new SeatalkNgPilotLockedHeading(null, st.DesiredHeading);
                    DispatchSentenceEvents(lockedHeading);
                }

                _messageCounter++;
            }
        }
    }
}
