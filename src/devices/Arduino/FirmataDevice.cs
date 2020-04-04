using System;
using System.Collections;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Device.Gpio.I2c;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

#pragma warning disable CS1591
namespace Iot.Device.Arduino
{
    public delegate void DigitalPinValueChanged(int pin, PinValue newValue);

    internal delegate void AnalogPinValueUpdated(int pin, uint rawValue);

    internal sealed class FirmataDevice : IDisposable
    {
        private const byte FIRMATA_PROTOCOL_MAJOR_VERSION = 2;
        private const byte FIRMATA_PROTOCOL_MINOR_VERSION = 6;
        private const int FIRMATA_INIT_TIMEOUT_SECONDS = 4;
        private const int MESSAGE_TIMEOUT_MILLIS = 500;
        private byte _firmwareVersionMajor;
        private byte _firmwareVersionMinor;
        private byte _actualFirmataProtocolMajorVersion;
        private byte _actualFirmataProtocolMinorVersion;

        private string _firmwareName;
        private Stream _firmataStream;
        private Thread _inputThread;
        private bool _inputThreadShouldExit;
        private List<SupportedPinConfiguration> _supportedPinConfigurations;
        private IList<byte> _lastResponse;
        private List<PinValue> _lastPinValues;
        private Dictionary<int, uint> _lastAnalogValues;
        private object _lastPinValueLock;
        private object _lastAnalogValueLock;
        private object _synchronisationLock;

        // Event used when waiting for answers (i.e. after requesting firmware version)
        private AutoResetEvent _dataReceived;

        public event DigitalPinValueChanged DigitalPortValueUpdated;

        public event AnalogPinValueUpdated AnalogPinValueUpdated;

        public event Action<string, Exception> OnError;

        public FirmataDevice()
        {
            _firmwareVersionMajor = 0;
            _firmwareVersionMinor = 0;
            _firmataStream = null;
            _inputThreadShouldExit = false;
            _dataReceived = new AutoResetEvent(false);
            _supportedPinConfigurations = new List<SupportedPinConfiguration>();
            _synchronisationLock = new object();
            _lastPinValues = new List<PinValue>();
            _lastPinValueLock = new object();
            _lastAnalogValues = new Dictionary<int, uint>();
            _lastAnalogValueLock = new object();
        }

        internal List<SupportedPinConfiguration> PinConfigurations
        {
            get
            {
                return _supportedPinConfigurations;
            }
        }

        public void Open(Stream stream)
        {
            _firmataStream = stream;

            if (_firmataStream.CanRead && _firmataStream.CanWrite)
            {
                StartListening();
            }
            else
            {
                throw new NotSupportedException("Need a read-write stream to the hardware device");
            }
        }

        public void Close()
        {
            StopThread();
            lock (_synchronisationLock)
            {
                if (_firmataStream != null)
                {
                    _firmataStream.Close();
                }

                _firmataStream = null;
            }

            if (_dataReceived != null)
            {
                _dataReceived.Dispose();
                _dataReceived = null;
            }
        }

        /// <summary>
        /// Used where?
        /// </summary>
        private void SendString(byte command, string message)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(message);
            lock (_synchronisationLock)
            {
                _firmataStream.WriteByte(240);
                _firmataStream.WriteByte((byte)(command & (uint)sbyte.MaxValue));
                SendValuesAsTwo7bitBytes(bytes);
                _firmataStream.WriteByte(247);
                _firmataStream.Flush();
            }
        }

        private void StartListening()
        {
            if (_inputThread != null && _inputThread.IsAlive)
            {
                return;
            }

            _inputThreadShouldExit = false;

            _inputThread = new Thread(InputThread);
            _inputThread.Start();
        }

        private void ProcessInput()
        {
            // ReadByte reads one byte, but takes an int as an argument, so it can specify -1 for end-of-stream or timeout
            int data = _firmataStream.ReadByte();
            if (data == (0xFFFF))
            {
                return;
            }

            byte b = (byte)(data & 0x00FF);
            byte upper_nibble = (byte)(data & 0xF0);
            byte lower_nibble = (byte)(data & 0x0F);

            /*
             * the relevant bits in the command depends on the value of the data byte. If it is less than 0xF0 (start sysex), only the upper nibble identifies the command
             * while the lower nibble contains additional data
             */
            FirmataCommand command = (FirmataCommand)((data < ((ushort)FirmataCommand.START_SYSEX) ? upper_nibble : b));

            // determine the number of bytes remaining in the message
            int bytes_remaining = 0;
            bool isMessageSysex = false;
            switch (command)
            {
                default: // command not understood
                case FirmataCommand.END_SYSEX: // should never happen
                    return;

                // commands that require 2 additional bytes
                case FirmataCommand.DIGITAL_MESSAGE:
                case FirmataCommand.ANALOG_MESSAGE:
                case FirmataCommand.SET_PIN_MODE:
                case FirmataCommand.PROTOCOL_VERSION:
                    bytes_remaining = 2;
                    break;

                // commands that require 1 additional byte
                case FirmataCommand.REPORT_ANALOG_PIN:
                case FirmataCommand.REPORT_DIGITAL_PIN:
                    bytes_remaining = 1;
                    break;

                // commands that do not require additional bytes
                case FirmataCommand.SYSTEM_RESET:
                    // do nothing, as there is nothing to reset
                    return;

                case FirmataCommand.START_SYSEX:
                    // this is a special case with no set number of bytes remaining
                    isMessageSysex = true;
                    break;
            }

            // read the remaining message while keeping track of elapsed time to timeout in case of incomplete message
            List<byte> message = new List<byte>();
            int bytes_read = 0;
            Stopwatch timeout_start = Stopwatch.StartNew();
            while (bytes_remaining > 0 || isMessageSysex)
            {
                data = _firmataStream.ReadByte();

                // if no data was available, check for timeout
                if (data == 0xFFFF)
                {
                    // get elapsed seconds, given as a double with resolution in nanoseconds
                    var elapsed = timeout_start.Elapsed;

                    if (elapsed.TotalMilliseconds > MESSAGE_TIMEOUT_MILLIS)
                    {
                        return;
                    }

                    continue;
                }

                timeout_start.Restart();

                // if we're parsing sysex and we've just read the END_SYSEX command, we're done.
                if (isMessageSysex && (data == (short)FirmataCommand.END_SYSEX))
                {
                    break;
                }

                message.Add((byte)(data & 0xFF));
                ++bytes_read;
                --bytes_remaining;
            }

            // process the message
            switch (command)
            {
                // ignore these message types (they should not be in a reply)
                default:
                case FirmataCommand.REPORT_ANALOG_PIN:
                case FirmataCommand.REPORT_DIGITAL_PIN:
                case FirmataCommand.SET_PIN_MODE:
                case FirmataCommand.END_SYSEX:
                case FirmataCommand.SYSTEM_RESET:
                    return;
                case FirmataCommand.PROTOCOL_VERSION:
                    if (_actualFirmataProtocolMajorVersion != 0)
                    {
                        // Firmata sends this message automatically after a device reset (if you press the reset button on the arduino)
                        // If we know the version already, this is unexpected.
                        OnError?.Invoke("The device was unexpectedly reset. Please restart the communication.", null);
                    }

                    _actualFirmataProtocolMajorVersion = message[0];
                    _actualFirmataProtocolMinorVersion = message[1];
                    _dataReceived.Set();

                    return;

                case FirmataCommand.ANALOG_MESSAGE:
                    // report analog commands store the pin number in the lower nibble of the command byte, the value is split over two 7-bit bytes
                    // AnalogValueUpdated(this,
                    //    new CallbackEventArgs(lower_nibble, (ushort)(message[0] | (message[1] << 7))));
                    {
                        int pin = lower_nibble;
                        uint value = (uint)(message[0] | (message[1] << 7));
                        lock (_lastAnalogValueLock)
                        {
                            _lastAnalogValues[pin] = value;
                        }

                        AnalogPinValueUpdated?.Invoke(pin, value);
                    }

                    break;

                case FirmataCommand.DIGITAL_MESSAGE:
                    // digital messages store the port number in the lower nibble of the command byte, the port value is split over two 7-bit bytes
                    // Each port corresponds to 8 pins
                    {
                        int offset = lower_nibble * 8;
                        ushort pinValues = (ushort)(message[0] | (message[1] << 7));
                        lock (_lastPinValueLock)
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                PinValue oldValue = _lastPinValues[i + offset];
                                int mask = 1 << i;
                                PinValue newValue = (pinValues & mask) == 0 ? PinValue.Low : PinValue.High;
                                if (newValue != oldValue)
                                {
                                    _lastPinValues[i + offset] = newValue;
                                    // TODO: The callback should not be within the lock
                                    DigitalPortValueUpdated?.Invoke(i + offset, newValue);
                                }
                            }
                        }
                    }

                    break;

                case FirmataCommand.START_SYSEX:
                    // a sysex message must include at least one extended-command byte
                    if (bytes_read < 1)
                    {
                        return;
                    }

                    // retrieve the raw data array & extract the extended-command byte
                    var raw_data = message.ToArray();
                    FirmataSysexCommand sysCommand = (FirmataSysexCommand)(raw_data[0]);
                    int index = 0;
                    ++index;
                    --bytes_read;

                    switch (sysCommand)
                    {
                        case FirmataSysexCommand.REPORT_FIRMWARE:
                            // See https://github.com/firmata/protocol/blob/master/protocol.md
                            // Byte 0 is the command (0x79) and can be skipped here, as we've already interpreted it
                            {
                                _firmwareVersionMajor = raw_data[1];
                                _firmwareVersionMinor = raw_data[2];
                                int stringLength = (raw_data.Length - 3) / 2;
                                Span<byte> bytesReceived = stackalloc byte[stringLength];
                                ReassembleByteString(raw_data, 3, stringLength * 2, bytesReceived);

                                _firmwareName = Encoding.ASCII.GetString(bytesReceived);
                                _dataReceived.Set();
                            }

                            return;

                        case FirmataSysexCommand.STRING_DATA:
                            {
                                // condense back into 1-byte data
                                int stringLength = (raw_data.Length - 1) / 2;
                                Span<byte> bytesReceived = stackalloc byte[stringLength];
                                ReassembleByteString(raw_data, 1, stringLength * 2, bytesReceived);

                                string message1 = Encoding.ASCII.GetString(bytesReceived);
                                OnError?.Invoke(message1, null);
                            }

                            break;

                        case FirmataSysexCommand.CAPABILITY_RESPONSE:
                            {
                                _supportedPinConfigurations.Clear();
                                int idx = 1;
                                var currentPin = new SupportedPinConfiguration(0);
                                int pin = 0;
                                while (idx < raw_data.Length)
                                {
                                    int mode = raw_data[idx++];
                                    if (mode == 0x7F)
                                    {
                                        _supportedPinConfigurations.Add(currentPin);
                                        currentPin = new SupportedPinConfiguration(++pin);
                                        continue;
                                    }

                                    int resolution = raw_data[idx++];
                                    switch ((SupportedMode)mode)
                                    {
                                        default:
                                            currentPin.PinModes.Add((SupportedMode)mode);
                                            break;
                                        case SupportedMode.ANALOG_INPUT:
                                            currentPin.PinModes.Add(SupportedMode.ANALOG_INPUT);
                                            currentPin.AnalogInputResolutionBits = resolution;
                                            break;
                                        case SupportedMode.PWM:
                                            currentPin.PinModes.Add(SupportedMode.PWM);
                                            currentPin.PwmResolutionBits = resolution;
                                            break;
                                    }
                                }

                                // Add 8 entries, so that later we do not need to check whether a port (bank) is complete
                                _lastPinValues = new PinValue[_supportedPinConfigurations.Count + 8].ToList();
                                _dataReceived.Set();
                                // Do not add the last instance, should also be terminated by 0xF7
                            }

                            break;

                        case FirmataSysexCommand.ANALOG_MAPPING_RESPONSE:
                            {
                                // This needs to have been set up previously
                                if (_supportedPinConfigurations.Count == 0)
                                {
                                    return;
                                }

                                int idx = 1;
                                int pin = 0;
                                while (idx < raw_data.Length)
                                {
                                    if (raw_data[idx] != 127)
                                    {
                                        _supportedPinConfigurations[pin].AnalogPinNumber = raw_data[idx];
                                    }

                                    idx++;
                                    pin++;
                                }

                                _dataReceived.Set();
                            }

                            break;
                        case FirmataSysexCommand.I2C_REPLY:

                            _lastResponse = raw_data;
                            _dataReceived.Set();
                            break;

                        case FirmataSysexCommand.PIN_STATE_RESPONSE:
                            _lastResponse = raw_data; // the instance is constant, so we can just remember the pointer
                            _dataReceived.Set();
                            break;

                        default:

                            // we pass the data forward as-is for any other type of sysex command
                            break;
                    }

                    break;
            }
        }

        private void InputThread()
        {
            while (!_inputThreadShouldExit)
            {
                try
                {
                    ProcessInput();
                }
                catch (Exception ex)
                {
                    OnError?.Invoke($"Firmata protocol error: Parser exception {ex.Message}", ex);
                }
            }
        }

        public Version QueryFirmataVersion()
        {
            lock (_synchronisationLock)
            {
                _dataReceived.Reset();
                _firmataStream.WriteByte((byte)FirmataCommand.PROTOCOL_VERSION);
                _firmataStream.Flush();
                bool result = _dataReceived.WaitOne(TimeSpan.FromSeconds(FIRMATA_INIT_TIMEOUT_SECONDS));
                if (result == false)
                {
                    throw new TimeoutException("Timeout waiting for firmata version");
                }

                return new Version(_actualFirmataProtocolMajorVersion, _actualFirmataProtocolMinorVersion);
            }
        }

        public Version QuerySupportedFirmataVersion()
        {
            return new Version(FIRMATA_PROTOCOL_MAJOR_VERSION, FIRMATA_PROTOCOL_MINOR_VERSION);
        }

        public Version QueryFirmwareVersion(out string firmwareName)
        {
            lock (_synchronisationLock)
            {
                _dataReceived.Reset();
                _firmataStream.WriteByte((byte)FirmataCommand.START_SYSEX);
                _firmataStream.WriteByte((byte)FirmataSysexCommand.REPORT_FIRMWARE);
                _firmataStream.WriteByte((byte)FirmataCommand.END_SYSEX);
                bool result = _dataReceived.WaitOne(TimeSpan.FromSeconds(FIRMATA_INIT_TIMEOUT_SECONDS));
                if (result == false)
                {
                    throw new TimeoutException("Timeout waiting for firmata version");
                }

                firmwareName = _firmwareName;
                return new Version(_firmwareVersionMajor, _firmwareVersionMinor);
            }
        }

        public void QueryCapabilities()
        {
            lock (_synchronisationLock)
            {
                _dataReceived.Reset();
                _firmataStream.WriteByte((byte)FirmataCommand.START_SYSEX);
                _firmataStream.WriteByte((byte)FirmataSysexCommand.CAPABILITY_QUERY);
                _firmataStream.WriteByte((byte)FirmataCommand.END_SYSEX);
                bool result = _dataReceived.WaitOne(TimeSpan.FromSeconds(FIRMATA_INIT_TIMEOUT_SECONDS));
                if (result == false)
                {
                    throw new TimeoutException("Timeout waiting for device capabilities");
                }

                _dataReceived.Reset();
                _firmataStream.WriteByte((byte)FirmataCommand.START_SYSEX);
                _firmataStream.WriteByte((byte)FirmataSysexCommand.ANALOG_MAPPING_QUERY);
                _firmataStream.WriteByte((byte)FirmataCommand.END_SYSEX);
                result = _dataReceived.WaitOne(TimeSpan.FromSeconds(FIRMATA_INIT_TIMEOUT_SECONDS));
                if (result == false)
                {
                    throw new TimeoutException("Timeout waiting for PWM port mappings");
                }
            }
        }

        private void StopThread()
        {
            _inputThreadShouldExit = true;
            if (_inputThread != null)
            {
                _inputThread.Join();
                _inputThread = null;
            }
        }

        public void SetPinMode(int pin, SupportedMode firmataMode)
        {
            lock (_synchronisationLock)
            {
                _firmataStream.WriteByte((byte)FirmataCommand.SET_PIN_MODE);
                _firmataStream.WriteByte((byte)pin);
                _firmataStream.WriteByte((byte)firmataMode);
                _firmataStream.Flush();
            }
        }

        public PinMode GetPinMode(int pinNumber)
        {
            lock (_synchronisationLock)
            {
                _dataReceived.Reset();
                _firmataStream.WriteByte((byte)FirmataCommand.START_SYSEX);
                _firmataStream.WriteByte((byte)FirmataSysexCommand.PIN_STATE_QUERY);
                _firmataStream.WriteByte((byte)pinNumber);
                _firmataStream.WriteByte((byte)FirmataCommand.END_SYSEX);
                _firmataStream.Flush();
                bool result = _dataReceived.WaitOne(TimeSpan.FromSeconds(FIRMATA_INIT_TIMEOUT_SECONDS));
                if (result == false)
                {
                    throw new TimeoutException("Timeout waiting for pin mode.");
                }

                // The mode is byte 4
                if (_lastResponse.Count < 4)
                {
                    throw new InvalidOperationException("Not enough data in reply");
                }

                if (_lastResponse[1] != pinNumber)
                {
                    throw new InvalidOperationException(
                        "The reply didn't match the query (another port was indicated)");
                }

                SupportedMode mode = (SupportedMode)(_lastResponse[2]);
                switch (mode)
                {
                    case SupportedMode.DIGITAL_OUTPUT:
                        return PinMode.Output;
                    case SupportedMode.INPUT_PULLUP:
                        return PinMode.InputPullUp;
                    case SupportedMode.DIGITAL_INPUT:
                        return PinMode.Input;
                    default:
                        return PinMode.Input; // TODO: Return "Unknown"
                }
            }
        }

        /// <summary>
        /// Enables digital pin reporting for all ports (one port has 8 pins)
        /// </summary>
        public void EnableDigitalReporting()
        {
            int numPorts = (int)Math.Ceiling(PinConfigurations.Count / 8.0);
            lock (_synchronisationLock)
            {
                for (byte i = 0; i < numPorts; i++)
                {
                    _firmataStream.WriteByte((byte)(0xD0 + i));
                    _firmataStream.WriteByte(1);
                    _firmataStream.Flush();
                }
            }
        }

        public PinValue GetDigitalPinState(int pinNumber)
        {
            lock (_lastPinValueLock)
            {
                return _lastPinValues[pinNumber];
            }
        }

        public void WriteDigitalPin(int pin, PinValue value)
        {
            lock (_synchronisationLock)
            {
                _firmataStream.WriteByte((byte)FirmataCommand.SET_DIGITAL_VALUE);
                _firmataStream.WriteByte((byte)pin);
                _firmataStream.WriteByte((byte)(value == PinValue.High ? 1 : 0));
                _firmataStream.Flush();
            }
        }

        public void WriteReadI2cData(int slaveAddress,  ReadOnlySpan<byte> writeData, Span<byte> replyData)
        {
            // See documentation at https://github.com/firmata/protocol/blob/master/i2c.md
            lock (_synchronisationLock)
            {
                if (writeData != null && writeData.Length > 0)
                {
                    _firmataStream.WriteByte((byte)FirmataCommand.START_SYSEX);
                    _firmataStream.WriteByte((byte)FirmataSysexCommand.I2C_REQUEST);
                    _firmataStream.WriteByte((byte)slaveAddress);
                    _firmataStream.WriteByte(0); // Write flag is 0, all other bits as well
                    SendValuesAsTwo7bitBytes(writeData);
                    _firmataStream.WriteByte((byte)FirmataCommand.END_SYSEX);
                    _firmataStream.Flush();
                }

                if (replyData != null && replyData.Length > 0)
                {
                    _dataReceived.Reset();
                    _firmataStream.WriteByte((byte)FirmataCommand.START_SYSEX);
                    _firmataStream.WriteByte((byte)FirmataSysexCommand.I2C_REQUEST);
                    _firmataStream.WriteByte((byte)slaveAddress);
                    _firmataStream.WriteByte(0b1000); // Read flag is 1, all other bits are 0
                    byte length = (byte)replyData.Length;
                    // Only write the length of the expected data.
                    // We could insert the register to read here, but we assume that has been written already (the client is responsible for that)
                    _firmataStream.WriteByte((byte)(length & (uint)sbyte.MaxValue));
                    _firmataStream.WriteByte((byte)(length >> 7 & sbyte.MaxValue));
                    _firmataStream.WriteByte((byte)FirmataCommand.END_SYSEX);
                    _firmataStream.Flush();
                    bool result = _dataReceived.WaitOne(TimeSpan.FromMilliseconds(100));
                    if (result == false)
                    {
                        throw new I2cCommunicationException("Timeout waiting for device reply");
                    }

                    if (_lastResponse[0] != (byte)FirmataSysexCommand.I2C_REPLY)
                    {
                        throw new I2cCommunicationException("Firmata protocol error: received incorrect query response");
                    }

                    if (_lastResponse[1] != (byte)slaveAddress && slaveAddress != 0)
                    {
                        throw new I2cCommunicationException($"Firmata protocol error: The wrong device did answer. Expected {slaveAddress} but got {_lastResponse[1]}.");
                    }

                    // Byte 0: I2C_REPLY
                    // Bytes 1 & 2: Slave address (the MSB is always 0, since we're only supporting 7-bit addresses)
                    // Bytes 3 & 4: Register. Often 0, and probably not needed
                    // Anything after that: reply data, with 2 bytes for each byte in the data stream
                    ReassembleByteString(_lastResponse, 5, _lastResponse.Count - 5, replyData);
                }
            }
        }

        public void SetPwmChannel(int pin, double dutyCycle)
        {
            lock (_synchronisationLock)
            {
                _firmataStream.WriteByte((byte)FirmataCommand.START_SYSEX);
                _firmataStream.WriteByte((byte)FirmataSysexCommand.EXTENDED_ANALOG);
                _firmataStream.WriteByte((byte)pin);
                // The arduino expects values between 0 and 255 for PWM channels.
                // The frequency cannot be set.
                int pwmMaxValue = _supportedPinConfigurations[pin].PwmResolutionBits; // This is 8 for most arduino boards
                pwmMaxValue = (1 << pwmMaxValue) - 1;
                int value = (int)Math.Max(0, Math.Min(dutyCycle * pwmMaxValue, pwmMaxValue));
                _firmataStream.WriteByte((byte)(value & (uint)sbyte.MaxValue)); // lower 7 bits
                _firmataStream.WriteByte((byte)(value >> 7 & sbyte.MaxValue)); // top bit (rest unused)
                _firmataStream.WriteByte((byte)FirmataCommand.END_SYSEX);
                _firmataStream.Flush();
            }
        }

        /// <summary>
        /// This takes the pin number in Arduino's own Analog numbering scheme. So A0 shall be specifed as 0
        /// </summary>
        public void EnableAnalogReporting(int pinNumber)
        {
            lock (_synchronisationLock)
            {
                _lastAnalogValues[pinNumber] = 0; // to make sure this entry exists
                _firmataStream.WriteByte((byte)((int)FirmataCommand.REPORT_ANALOG_PIN + pinNumber));
                _firmataStream.WriteByte((byte)1);
            }
        }

        public void DisableAnalogReporting(int pinNumber)
        {
            lock (_synchronisationLock)
            {
                _firmataStream.WriteByte((byte)((int)FirmataCommand.REPORT_ANALOG_PIN + pinNumber));
                _firmataStream.WriteByte((byte)0);
            }
        }

        public uint GetAnalogRawValue(int pinNumber)
        {
            lock (_lastAnalogValueLock)
            {
                return _lastAnalogValues[pinNumber];
            }
        }

        private void SendValuesAsTwo7bitBytes(ReadOnlySpan<byte> values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                _firmataStream.WriteByte((byte)(values[i] & (uint)sbyte.MaxValue));
                _firmataStream.WriteByte((byte)(values[i] >> 7 & sbyte.MaxValue));
            }
        }

        private void ReassembleByteString(IList<byte> byteStream, int startIndex, int length, Span<byte> reply)
        {
            int num;
            if (reply.Length < length / 2)
            {
                length = reply.Length * 2;
            }

            for (num = 0; num < length / 2; ++num)
            {
                reply[num] = (byte)(byteStream[startIndex + (num * 2)] |
                                    byteStream[startIndex + (num * 2) + 1] << 7);
            }

        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Close();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
