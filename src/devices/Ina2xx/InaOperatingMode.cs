namespace Iot.Device.Adc;

/// <summary>
/// An enumeration representing the operating modes available on the INA219 device.
/// </summary>
public enum InaOperatingMode : ushort
{
    /// <summary>Power Down mode</summary>
    PowerDown = 0b00000000_00000000,

    /// <summary>Mode to read the shunt voltage on demand</summary>
    ShuntVoltageTriggered = 0b00000000_00000001,

    /// <summary>Mode to read the bus voltage on demand</summary>
    BusVoltageTriggered = 0b00000000_00000010,

    /// <summary>Mode to read the shunt and bus voltage on demand</summary>
    ShuntAndBusTriggered = 0b00000000_00000011,

    /// <summary>Mode to disable the ADC</summary>
    AdcOff = 0b00000000_00000100,

    /// <summary>Mode to read the shunt voltage on continuously</summary>
    ShuntVoltageContinuous = 0b00000000_00000101,

    /// <summary>Mode to read the bus voltage on continuously</summary>
    BusVoltageContinuous = 0b00000000_00000110,

    /// <summary>Mode to read the shunt and bus voltage on continuously</summary>
    ShuntAndBusContinuous = 0b00000000_00000111
}
