﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

namespace Iot.Device.Nmea0183.Sentences
{
    /// <summary>
    /// An extended engine data message, using a PCDIN sequence (supported by some NMEA0183 to NMEA2000 bridges)
    /// PCDIN message 01F201 Engine status data (temperatures, oil pressure, operating time)
    /// </summary>
    public class SeaSmartEngineDetail : ProprietaryMessage
    {
        /// <summary>
        /// Constructs a new sentence
        /// </summary>
        public SeaSmartEngineDetail(bool status, TimeSpan operatingTime, Temperature temperature, int engineNumber)
            : base()
        {
            Status = status;
            OperatingTime = operatingTime;
            Temperature = temperature;
            EngineNumber = engineNumber;
            MessageTimeStamp = Environment.TickCount;
            Valid = true;
        }

        /// <summary>
        /// Constructs a new sentence
        /// </summary>
        public SeaSmartEngineDetail(EngineData data)
        {
            Status = data.Revolutions != RotationalSpeed.Zero;
            OperatingTime = data.OperatingTime;
            Temperature = data.EngineTemperature;
            EngineNumber = data.EngineNo;
            MessageTimeStamp = data.MessageTimeStamp;
            Valid = true;
        }

        /// <summary>
        /// The timestamp for the NMEA 2000 message
        /// </summary>
        public int MessageTimeStamp
        {
            get;
            private set;
        }

        /// <summary>
        /// Engine status: True for running, false for not running/error.
        /// </summary>
        public bool Status
        {
            get;
            private set;
        }

        /// <summary>
        /// Total engine operating time (typically displayed in hours)
        /// </summary>
        public TimeSpan OperatingTime
        {
            get;
            private set;
        }

        /// <summary>
        /// Engine temperature
        /// </summary>
        public Temperature Temperature
        {
            get;
            private set;
        }

        /// <summary>
        /// Number of the engine
        /// </summary>
        public int EngineNumber
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns true for this message
        /// </summary>
        public override bool ReplacesOlderInstance => true;

        /// <inheritdoc />
        public override string ToNmeaParameterList()
        {
            if (Valid)
            {
                string timeStampText = MessageTimeStamp.ToString("X8", CultureInfo.InvariantCulture);
                string engineNoText = EngineNumber.ToString("X2", CultureInfo.InvariantCulture);
                // $PCDIN,01F201,000C7E1B,02,000000FFFF407F0005000000000000FFFF000000000000007F7F*24
                //                           1-2---3---4---5---6---7-------8---9---1011--12--1314
                // 1) Engine no. 0: Cntr/Single
                // 2) Oil pressure
                // 3) Oil temp
                // 4) Engine Temp
                // 5) Alternator voltage
                // 6) Fuel rate
                // 7) Engine operating time (seconds)
                // 8) Coolant pressure
                // 9) Fuel pressure
                // 10) Reserved
                // 11) Status
                // 12) Status
                // 13) Load percent
                // 14) Torque percent
                int operatingTimeSeconds = (int)OperatingTime.TotalSeconds;
                string operatingTimeString = operatingTimeSeconds.ToString("X8", CultureInfo.InvariantCulture);
                // For whatever reason, this expects this as little endian (all the other way round)
                string swappedString = operatingTimeString.Substring(6, 2) + operatingTimeString.Substring(4, 2) +
                                       operatingTimeString.Substring(2, 2) + operatingTimeString.Substring(0, 2);

                // Status = 0 is ok, anything else seems to indicate a fault
                int status = Status ? 0 : 1;
                string statusString = status.ToString("X4", CultureInfo.InvariantCulture);
                int engineTempKelvin = (int)Math.Round(Temperature.Kelvins * 100.0, 1);
                string engineTempString = engineTempKelvin.ToString("X4", CultureInfo.InvariantCulture);
                // Seems to require a little endian conversion as well
                engineTempString = engineTempString.Substring(2, 2) + engineTempString.Substring(0, 2);
                return "01F201," + timeStampText + ",02," + engineNoText + "0000FFFF" + engineTempString + "00050000" + swappedString + "FFFF000000" + statusString + "00007F7F";

            }

            return string.Empty;
        }

        /// <inheritdoc />
        public override string ToReadableContent()
        {
            if (Valid)
            {
                return $"Engine {EngineNumber} Status: {(Status ? "Running" : "Off")} Temperature {Temperature.DegreesCelsius} °C";
            }

            return "No valid data (or engine off)";
        }
    }
}