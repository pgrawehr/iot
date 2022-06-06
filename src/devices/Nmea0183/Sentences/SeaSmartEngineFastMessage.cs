// Licensed to the .NET Foundation under one or more agreements.
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
    /// This message mostly provides the RPM value and can be sent with a high frequency.
    /// </summary>
    public class SeaSmartEngineFastMessage : ProprietaryMessage
    {
        /// <summary>
        /// Constructs a new sentence
        /// </summary>
        public SeaSmartEngineFastMessage(RotationalSpeed speed, int engineNumber, Ratio pitch)
            : base()
        {
            RotationalSpeed = speed;
            EngineNumber = engineNumber;
            PropellerPitch = pitch;
            Valid = true;
            MessageTimeStamp = Environment.TickCount;
        }

        /// <summary>
        /// Constructs a new sentence
        /// </summary>
        public SeaSmartEngineFastMessage(EngineData data)
        {
            RotationalSpeed = data.Revolutions;
            EngineNumber = data.EngineNo;
            PropellerPitch = data.Pitch;
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
        /// Engine revolutions per time, typically RPM (revolutions per minute) is used
        /// as unit for engine speed.
        /// </summary>
        public RotationalSpeed RotationalSpeed
        {
            get;
            private set;
        }

        /// <summary>
        /// Number of the engine.
        /// </summary>
        public int EngineNumber
        {
            get;
            private set;
        }

        /// <summary>
        /// Pitch of the propeller. Propellers with changeable pitch are very rare for pleasure boats, with the exception of folding propellers
        /// for sailboats, but these fold only when the engine is not in use and there's no sensor to detect the state.
        /// </summary>
        public Ratio PropellerPitch
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
                // Example data set: (bad example from the docs, since the engine is just not running here)
                // $PCDIN,01F200,000C7A4F,02,000000FFFF7FFFFF*21
                int rpm = (int)RotationalSpeed.RevolutionsPerMinute;
                rpm = rpm / 64; // Some trying shows that the last 6 bits are shifted out
                if (rpm > short.MaxValue)
                {
                    rpm = short.MaxValue;
                }

                string engineNoText = EngineNumber.ToString("X2", CultureInfo.InvariantCulture);
                string rpmText = rpm.ToString("X4", CultureInfo.InvariantCulture);
                int pitchPercent = (int)PropellerPitch.Percent;
                string pitchText = pitchPercent.ToString("X2", CultureInfo.InvariantCulture);
                string timeStampText = MessageTimeStamp.ToString("X8", CultureInfo.InvariantCulture);

                return "01F200," + timeStampText + ",02," + engineNoText + rpmText + "FFFF" + pitchText + "FFFF";
            }

            return string.Empty;
        }

        /// <inheritdoc />
        public override string ToReadableContent()
        {
            if (Valid)
            {
                return $"Engine {EngineNumber} RPM: {RotationalSpeed.RevolutionsPerMinute}";
            }

            return "No valid data (or engine off)";
        }
    }
}
