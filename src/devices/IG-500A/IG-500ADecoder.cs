// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.ComponentModel;
using System.Device.I2c;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using Iot.Units;
#pragma warning disable 1591
namespace Iot.Device.Imu
{
    /// <summary>
    /// IG-500A by sbg systems - inertial measurement unit (IMU)
    /// </summary>
    public class Ig500Sensor : IDisposable
    {
        private const int PacketSize = 512;
        private const int NumberOfErrorsToKeep = 20;
        private const int InputBufferSize = 10 * 1024;
        private readonly Stream _dataStream;
        private readonly OutputDataSets _enableDataSets;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly RoundRobinBuffer _robinBuffer;
        private readonly LinkedList<string> _recentParserErrors;
        private Thread _decoderThread;
        private OutputDataSets _currentDataMask;
        private bool _dataMaskSent;
        private bool _dataMaskReceived;
        private bool _outputModeReceived;
        private byte _outputMode;

        public event Action<Vector3> OnNewData;

        private List<OutputDataOffsets> _dataFields = new List<OutputDataOffsets>()
        {
            new OutputDataOffsets(OutputDataSets.Quaternion, "Estimate of attitude in quaternion form", 16),
            new OutputDataOffsets(OutputDataSets.Euler, "Estimate of attitude in Euler angles form", 12),
            new OutputDataOffsets(OutputDataSets.Matrix, "Estimate of attitude in Matrix form", 36),
            new OutputDataOffsets(OutputDataSets.Gyroscopes, "Calibrated values of gyroscopes", 12),
            new OutputDataOffsets(OutputDataSets.Accelerometers, "Calibrated values of accelerometers", 12),
            new OutputDataOffsets(OutputDataSets.Magnetometers, "Calibrated values of magnetometers", 12),
            new OutputDataOffsets(OutputDataSets.Temperatures, "Calibrated values of temperatures", 8),
            new OutputDataOffsets(OutputDataSets.GyroscopesRaw, "Raw values of gyroscopes", 6),
            new OutputDataOffsets(OutputDataSets.AccelerometersRaw, "Raw values of accelerometers", 6),
            new OutputDataOffsets(OutputDataSets.MagnetometersRaw, "Raw values of magnetometers", 6),
            new OutputDataOffsets(OutputDataSets.TemperaturesRaw, "Raw values of temperatures", 4),
            new OutputDataOffsets(OutputDataSets.TimeSinceReset, "Time elapsed since the reset of the device", 4),
            new OutputDataOffsets(OutputDataSets.DeviceStatus, "Device status Bit field", 4),
            new OutputDataOffsets(OutputDataSets.GpsPosition, "Raw GPS position in WGS84 format", 12),
            new OutputDataOffsets(OutputDataSets.GpsNavigation, "Raw GPS velocity in NED, heading", 16),
            new OutputDataOffsets(OutputDataSets.GpsAccuracy, "Raw GPS horizontal, vertical and heading accuracies", 16),
            new OutputDataOffsets(OutputDataSets.GpsInfo, "Raw GPS information such as available satellites", 6),
            new OutputDataOffsets(OutputDataSets.BaroAltitude, "Barometric altitude referenced to a user defined value", 4),
            new OutputDataOffsets(OutputDataSets.BaroPressure, "Absolute pressure in pascals", 4),
            new OutputDataOffsets(OutputDataSets.Position, "Kalman enhanced 3d position", 24),
            new OutputDataOffsets(OutputDataSets.Velocity, "Kalman enhanced 3d velocity", 12),
            new OutputDataOffsets(OutputDataSets.AttitudeAccuracy, "Kalman estimated attitude accuracy", 4),
            new OutputDataOffsets(OutputDataSets.NavigationAccuracy, "Kalman estimated position and velocity accuracy", 8),
            new OutputDataOffsets(OutputDataSets.GyroTemperatures, "Calibrated internal gyro temperatures sensors output", 12),
            new OutputDataOffsets(OutputDataSets.GyroTemperaturesRaw, "Raw internal gyro temperatures sensors", 12),
            new OutputDataOffsets(OutputDataSets.UtcTimeReference, "UTC time reference", 10),
            new OutputDataOffsets(OutputDataSets.MagCalibData, "Enable magnetometers Calibration data output", 12),
            new OutputDataOffsets(OutputDataSets.GpsTrueHeading, "Enable raw true heading output", 8),
            new OutputDataOffsets(OutputDataSets.OdoVelocity, "Enable Odometer raw velocity output", 8),
        };

        public Ig500Sensor(Stream dataStream, OutputDataSets enableDataSets)
        {
            _dataStream = dataStream;
            if (enableDataSets == OutputDataSets.None)
            {
                throw new ArgumentOutOfRangeException(nameof(enableDataSets), "Must enable at least one data set");
            }

            _enableDataSets = enableDataSets;
            _cancellationTokenSource = new CancellationTokenSource();
            _currentDataMask = 0;
            _dataMaskReceived = false;
            _outputModeReceived = false;
            _outputMode = 0;
            _dataMaskSent = false;
            Orientation = Vector3.Zero;
            Quaternion = Vector4.Zero;
            Magnetometer = Vector3.Zero;
            Gyroscope = Vector3.Zero;
            Accelerometer = Vector3.Zero;
            Temperature = Temperature.FromCelsius(0);

            EulerAnglesDegrees = true;
            Temperature = Temperature.FromCelsius(0);
            _recentParserErrors = new LinkedList<string>();
            _robinBuffer = new RoundRobinBuffer(InputBufferSize);
            _decoderThread = new Thread(MessageParser);
            _decoderThread.Start();
        }

        public Vector3 Orientation
        {
            get;
            private set;
        }

        public Vector4 Quaternion
        {
            get;
            private set;
        }

        public Vector3 Magnetometer
        {
            get;
            private set;
        }

        public Vector3 Gyroscope
        {
            get;
            private set;
        }

        public Vector3 Accelerometer
        {
            get;
            private set;
        }

        public bool EulerAnglesDegrees
        {
            get;
            set;
        }

        public Temperature Temperature
        {
            get;
            private set;
        }

        public IEnumerable<string> RecentParserErrors
        {
            get
            {
                lock (_recentParserErrors)
                {
                    return _recentParserErrors.ToArray(); // Clone list
                }
            }
        }

        private void MessageParser()
        {
            // The maximum length of one packet
            byte[] inputBuffer = new byte[PacketSize];
            byte[] currentPacketBuffer = new byte[PacketSize];
            byte[] currentDataSection = new byte[PacketSize];
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                // We're assuming a blocking read, but the code can work with either
                int bytesReceived = 0;
                try
                {
                    bytesReceived = _dataStream.Read(inputBuffer, 0, 512);
                }
                catch (OperationCanceledException)
                {
                    // Ignore, will probably abort shortly
                    continue;
                }
                catch (ObjectDisposedException)
                {
                    continue;
                }

                if (bytesReceived == 0)
                {
                    Thread.Sleep(20);
                    continue;
                }

                try
                {
                    _robinBuffer.InsertBytes(inputBuffer, bytesReceived);
                }
                catch (InvalidOperationException x)
                {
                    // Buffer overflow?
                    AddParserError(x.Message);
                }

                if (_robinBuffer.PercentageUsed > 10)
                {
                    // This should always stay very low, or it will result in latency
                    AddParserError($"Input buffer usage is {_robinBuffer.PercentageUsed}.");
                }

                int actualBytesReceived = _robinBuffer.GetBuffer(currentPacketBuffer, PacketSize);
                if (actualBytesReceived > 8) // Minimum block size
                {
                    // Find sync bytes
                    int bytesSkipped = 0;
                    int bytesLeft = actualBytesReceived;
                    while (!(currentPacketBuffer[bytesSkipped] == 0xff &&
                             currentPacketBuffer[bytesSkipped + 1] == 0x02) && bytesLeft >= 8)
                    {
                        bytesSkipped++;
                        bytesLeft--;
                    }

                    if (bytesSkipped > 0)
                    {
                        AddParserError($"Resync required. Skipped {bytesSkipped} bytes from input");
                    }

                    // Throw away the skipped bytes
                    _robinBuffer.ConsumeBytes(bytesSkipped);
                    if (bytesLeft < 8)
                    {
                        continue; // Wait for more data
                    }

                    byte command = currentPacketBuffer[bytesSkipped + 2];
                    byte lenmsb = currentPacketBuffer[bytesSkipped + 3];
                    byte lenlsb = currentPacketBuffer[bytesSkipped + 4];
                    int length = lenmsb << 8 | lenlsb;

                    if (bytesLeft < length + 3)
                    {
                        // We need to resync first, this is a big packet (or synchronization is completely lost)
                        continue;
                    }

                    int crcCalculated = CalcCrc(currentPacketBuffer, bytesSkipped + 2, length + 3);
                    byte crcmsb = currentPacketBuffer[bytesSkipped + length + 5];
                    byte crclsb = currentPacketBuffer[bytesSkipped + length + 6];
                    int effectiveCrc = crcmsb << 8 | crclsb;

                    if (effectiveCrc != crcCalculated)
                    {
                        AddParserError($"CRC Error for received package {(CommandIds)command}. Should be 0x{crcCalculated:X}, but was 0x{effectiveCrc:X}.");
                        // Remove anyway - there's nothing we can do here
                        _robinBuffer.ConsumeBytes(length + 8);
                        continue;
                    }

                    Array.ConstrainedCopy(currentPacketBuffer, bytesSkipped + 5, currentDataSection, 0, length + 3);

                    // We have processed this many bytes
                    _robinBuffer.ConsumeBytes(length + 8);
                    DecodePacket((CommandIds)command, currentDataSection, length);
                }
            }
        }

        private void AddParserError(string message)
        {
            lock (_recentParserErrors)
            {
                _recentParserErrors.AddLast(message);
                if (_recentParserErrors.Count > NumberOfErrorsToKeep)
                {
                    _recentParserErrors.RemoveFirst();
                }
            }
        }

        private UInt16 CalcCrc(byte[] buffer, int startOffset, int bufferSize)
        {
            UInt16 poly = 0x8408;
            UInt16 crc = 0;
            byte carry;
            byte i_bits;
            UInt16 j;
            // The start bytes are not included in CRC
            for (j = 0; j < bufferSize; j++)
            {
                crc = (ushort)(crc ^ buffer[j + startOffset]);
                for (i_bits = 0; i_bits < 8; i_bits++)
                {
                    carry = (byte)(crc & 1);
                    crc = (UInt16)(crc / 2);
                    if (carry != 0)
                    {
                        crc = (UInt16)(crc ^ poly);
                    }
                }
            }

            return crc;
        }

        private void SendCommand(CommandIds command, byte[] data, int length)
        {
            byte[] sendData = new byte[length + 8];
            // Header
            sendData[0] = 0xFF;
            sendData[1] = 0x02;
            sendData[2] = (byte)command;
            sendData[3] = (byte)((length >> 8) & 0xFF);
            sendData[4] = (byte)(length & 0xFF);
            // Data
            Array.ConstrainedCopy(data, 0, sendData, 5, length);
            UInt16 crc = CalcCrc(sendData, 2, length + 3);
            // CRC (from command to end of data)
            sendData[length + 5] = (byte)((crc >> 8) & 0xFF);
            sendData[length + 6] = (byte)(crc & 0xFF);
            // Footer
            sendData[length + 7] = 0x03;
            _dataStream.Write(sendData, 0, length + 8);
        }

        private void DecodePacket(CommandIds command, byte[] currentPacketBuffer, int length)
        {
            // Ask fo the data mask if we haven't seen it yet
            if (!_outputModeReceived)
            {
                SendDefaultConfiguration();
            }
            else if (!_dataMaskReceived)
            {
                // We can only read the data mask once we are sure the sensor is set to little endian mode, otherwise the bits would be interpreted the wrong way round
                SendCommand(CommandIds.GetDefaultOutputMask, new byte[0], 0);
            }

            switch (command)
            {
                case CommandIds.Ack:
                {
                    int ackCode = currentPacketBuffer[0];
                    if (ackCode != 0)
                    {
                        AddParserError($"Got a NACK reply with error code {ackCode}");
                    }

                    break;
                }

                case CommandIds.RetDefaultOutputMask when length != 4:
                    AddParserError("Decoder Error. Payload length for Default Output Mask must be 4");
                    return;
                case CommandIds.RetDefaultOutputMask:
                {
                    UInt32 mask = BinaryPrimitives.ReadUInt32LittleEndian(currentPacketBuffer);
                    Console.WriteLine($"Configured data mask is {mask}.");
                    _currentDataMask = (OutputDataSets)mask;

                    // We are only enabling data sets, not disabling them
                    // We are also only doing this only once, because the sensor may reply with a smaller set than desired, if he's unable to handle all sets (depends on model)
                    if (_dataMaskSent == false && (_currentDataMask & _enableDataSets) != _enableDataSets)
                    {
                        mask = mask | (uint)_enableDataSets;
                        byte[] setMask = new byte[5];
                        // Ensure bits 0 and 1 (Euler angles and Quaternion angles) are on
                        setMask[0] = 0;
                        BinaryPrimitives.WriteUInt32LittleEndian(new Span<byte>(setMask, 1, 4), mask);
                        SendCommand(CommandIds.SetDefaultOutputMask, setMask, 5);
                        _dataMaskSent = true;
                    }
                    else
                    {
                        _dataMaskReceived = true;
                    }

                    break;
                }

                case CommandIds.RetOutputMode when length != 1:
                    AddParserError("Decoder Error. Payload length for Output mode must be 1");
                    return;

                case CommandIds.RetOutputMode:
                    _outputMode = currentPacketBuffer[0];
                    _outputModeReceived = true;
                    break;

                case CommandIds.ContinuousDefaultOutput when _dataMaskReceived == false:
                    AddParserError("Ignoring sentence, configuration not yet received");
                    break;

                case CommandIds.ContinuousDefaultOutput:
                {
                    ParseContinuousOutput(currentPacketBuffer);
                    break;
                }

                default:
                    AddParserError($"Found a unknown packet with command {command} of length {length}");
                    break;

            }
        }

        private void ParseContinuousOutput(byte[] currentPacketBuffer)
        {
            int quaternionOffset = CalculateOutputOffset(OutputDataSets.Quaternion);
            if (quaternionOffset >= 0)
            {
                Vector4 quaternion = new Vector4();
                quaternion.X = ExtractFloatFromPacket(currentPacketBuffer, quaternionOffset);
                quaternion.Y = ExtractFloatFromPacket(currentPacketBuffer, quaternionOffset + 4);
                quaternion.Z = ExtractFloatFromPacket(currentPacketBuffer, quaternionOffset + 8);
                quaternion.W = ExtractFloatFromPacket(currentPacketBuffer, quaternionOffset + 12);
                Quaternion = quaternion;
            }

            int eulerOffset = CalculateOutputOffset(OutputDataSets.Euler);
            if (eulerOffset >= 0)
            {
                Vector3 euler = new Vector3();
                // The orientation order is resorted, to be equal to the BNO055: Heading, roll, pitch
                euler.Y = ExtractFloatFromPacket(currentPacketBuffer, eulerOffset);
                euler.Z = ExtractFloatFromPacket(currentPacketBuffer, eulerOffset + 4);
                euler.X = ExtractFloatFromPacket(currentPacketBuffer, eulerOffset + 8);
                if (EulerAnglesDegrees)
                {
                    euler *= (float)(180.0 / Math.PI);
                }

                Orientation = euler;
            }

            // We just pick the first temperature
            int temperatureOffset = CalculateOutputOffset(OutputDataSets.Temperatures);
            if (temperatureOffset >= 0)
            {
                var temp = Temperature.FromCelsius(ExtractFloatFromPacket(currentPacketBuffer, temperatureOffset));
                Temperature = temp;
            }

            int gyroOffset = CalculateOutputOffset(OutputDataSets.Gyroscopes);
            if (gyroOffset >= 0)
            {
                Vector3 gyro = new Vector3();
                gyro.X = ExtractFloatFromPacket(currentPacketBuffer, gyroOffset);
                gyro.Y = ExtractFloatFromPacket(currentPacketBuffer, gyroOffset + 4);
                gyro.Z = ExtractFloatFromPacket(currentPacketBuffer, gyroOffset + 8);
                if (EulerAnglesDegrees)
                {
                    gyro *= (float)(180.0 / Math.PI);
                }

                Gyroscope = gyro;
            }

            int accelOffset = CalculateOutputOffset(OutputDataSets.Accelerometers);
            if (accelOffset >= 0)
            {
                // Acceleration, in m/s^2
                Vector3 accel = new Vector3();
                accel.X = ExtractFloatFromPacket(currentPacketBuffer, accelOffset);
                accel.Y = ExtractFloatFromPacket(currentPacketBuffer, accelOffset + 4);
                accel.Z = ExtractFloatFromPacket(currentPacketBuffer, accelOffset + 8);

                Accelerometer = accel;
            }

            int magOffset = CalculateOutputOffset(OutputDataSets.Magnetometers);
            if (magOffset >= 0)
            {
                // Magnetic values, arbitrary unit
                Vector3 mag = new Vector3();
                mag.X = ExtractFloatFromPacket(currentPacketBuffer, magOffset);
                mag.Y = ExtractFloatFromPacket(currentPacketBuffer, magOffset + 4);
                mag.Z = ExtractFloatFromPacket(currentPacketBuffer, magOffset + 8);

                Magnetometer = mag;
            }

            OnNewData?.Invoke(Orientation);
        }

        private int CalculateOutputOffset(OutputDataSets dataSet)
        {
            if ((_currentDataMask & dataSet) == 0)
            {
                // The requested data set is not available
                return -1;
            }

            // Iterate over all data sets from right to left
            uint currentMask = 1;
            int byteOffset = 0;
            while (currentMask != (uint)dataSet)
            {
                if ((_currentDataMask & (OutputDataSets)currentMask) != 0)
                {
                    // The active data mask contains this data set
                    var dataSetProperties = _dataFields.Find(x => x.DataSet == (OutputDataSets)currentMask);
                    byteOffset += dataSetProperties.Length;
                }

                currentMask <<= 1;
            }

            return byteOffset;
        }

        private float ExtractFloatFromPacket(byte[] buffer, int offset)
        {
            // BinaryPrimitives.ReadFloatLittleEndian() doesn't exist yet, but see https://github.com/dotnet/corefx/issues/35791
            Int32 intRepresentation = BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(buffer, offset, 4));
            return BitConverter.Int32BitsToSingle(intRepresentation);
        }

        private void SendDefaultConfiguration()
        {
            // Set the output mode to little endian, but don't make it permanent
            SendCommand(CommandIds.SetOutputMode, new byte[2] { 0, 0x1 }, 2);
            // And get it (note: Setting the mode will reply with an ack, but not return the current setting)
            SendCommand(CommandIds.GetOutputMode, new byte[0], 0);
        }

        public bool WaitForSensorReady(out string errorMessage, TimeSpan timeout)
        {
            errorMessage = string.Empty;
            DateTime endTime = DateTime.Now + timeout;
            while (endTime > DateTime.Now)
            {
                if (_outputModeReceived && _dataMaskReceived)
                {
                    break;
                }

                Thread.Sleep(10);
            }

            if (!(_outputModeReceived && _dataMaskReceived))
            {
                errorMessage = "No reply from device";
                return false;
            }

            if (_outputMode != 0x1)
            {
                errorMessage = "Could not set Output mode to little endian";
                return false;
            }

            if ((_currentDataMask & _enableDataSets) != _enableDataSets)
            {
                // Not all the elements that were requested are supported by this sensor
                errorMessage = "Not all required data sets could be enabled";
                return false;
            }

            return true;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource.Cancel();
                _dataStream.Close();
                _decoderThread?.Join();
                _decoderThread = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private sealed class OutputDataOffsets
        {
            public OutputDataOffsets(OutputDataSets dataSet, string name, int length)
            {
                DataSet = dataSet;
                Name = name;
                Length = length;
            }

            public OutputDataSets DataSet { get; }
            public string Name { get; }
            public int Length { get; }

        }
    }
}
