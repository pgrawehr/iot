// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace Iot.Device.Nmea0183.Sentences
{
    /// <summary>
    /// NMEA2000 Device Function codes
    /// Defines the primary function of a device on the NMEA2000 network
    /// </summary>
    public enum DeviceFunction : byte
    {
        /// <summary>NMEA 2000 to Analog Gateway</summary>
        [Description("NMEA 2000 to Analog Gateway")]
        Nmea2000ToAnalogGateway = 130,

        /// <summary>Propulsion Engine</summary>
        [Description("Propulsion Engine")]
        PropulsionEngine = 140,

        /// <summary>Steering and Control Surfaces</summary>
        [Description("Steering and Control Surfaces")]
        SteeringAndControlSurfaces = 150,

        /// <summary>Navigation</summary>
        [Description("Navigation")]
        Navigation = 155,

        /// <summary>Communication</summary>
        [Description("Communication")]
        Communication = 160,

        /// <summary>Sensor Communication Interface</summary>
        [Description("Sensor Communication Interface")]
        SensorCommunicationInterface = 170,

        /// <summary>Instrumentation/General Systems</summary>
        [Description("Instrumentation/General Systems")]
        InstrumentationGeneralSystems = 175,

        /// <summary>External Environment</summary>
        [Description("External Environment")]
        ExternalEnvironment = 180,

        /// <summary>Internal Environment</summary>
        [Description("Internal Environment")]
        InternalEnvironment = 185,

        /// <summary>Deck + Cargo + Fishing Equipment Systems</summary>
        [Description("Deck + Cargo + Fishing Equipment Systems")]
        DeckCargoFishingEquipment = 190,
    }
}
