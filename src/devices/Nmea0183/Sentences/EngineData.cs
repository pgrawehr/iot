// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

namespace Iot.Device.Nmea0183.Sentences
{
    /// <summary>
    /// A helper DTO to transfer engine data in one blob
    /// </summary>
    public class EngineData
    {
        /// <summary>
        /// Constructs an instance containing all relevant data
        /// </summary>
        public EngineData(int messageTimeStamp, int engineNo, RotationalSpeed revolutions, Ratio pitch, TimeSpan operatingTime, Temperature engineTemperature)
        {
            MessageTimeStamp = messageTimeStamp;
            EngineNo = engineNo;
            Revolutions = revolutions;
            Pitch = pitch;
            OperatingTime = operatingTime;
            EngineTemperature = engineTemperature;
        }

        /// <summary>
        /// The NMEA2000 bus timestamp (not really relevant, I think)
        /// </summary>
        public int MessageTimeStamp
        {
            get;
        }

        /// <summary>
        /// The number of the engine. 0 = Single / Starboard, 1 = Port, 2 = Center
        /// </summary>
        public int EngineNo
        {
            get;
        }

        /// <summary>
        /// Current running speed of the engine
        /// </summary>
        public RotationalSpeed Revolutions
        {
            get;
        }

        /// <summary>
        /// Propeller pitch
        /// </summary>
        public Ratio Pitch
        {
            get;
        }

        /// <summary>
        /// Total engine run time
        /// </summary>
        public TimeSpan OperatingTime
        {
            get;
        }

        /// <summary>
        /// Engine temperature
        /// </summary>
        public Temperature EngineTemperature
        {
            get;
        }
    }
}
