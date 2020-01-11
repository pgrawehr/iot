// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers.Binary;
using System.ComponentModel;
using System.Device.I2c;
using System.IO;
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
        private readonly Stream _dataStream;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly RoundRobinBuffer _robinBuffer;
        private Thread _decoderThread;
        private UInt32 _currentDataMask;
        private bool _dataMaskReceived;
        private bool _outputModeReceived;
        private byte _outputMode;

        public Ig500Sensor(Stream dataStream)
        {
            _dataStream = dataStream;
            _cancellationTokenSource = new CancellationTokenSource();
            _currentDataMask = 0;
            _dataMaskReceived = false;
            _outputModeReceived = false;
            _outputMode = 0;
            _robinBuffer = new RoundRobinBuffer(4 * 1024 * 1024);
            _decoderThread = new Thread(MessageParser);
            _decoderThread.Start();
        }

        public Vector3 Orientation
        {
            get;
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

                if (bytesReceived == 0)
                {
                    Thread.Sleep(20);
                    continue;
                }

                _robinBuffer.InsertBytes(inputBuffer, bytesReceived);
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

                    Array.ConstrainedCopy(currentPacketBuffer, bytesSkipped + 5, currentDataSection, 0, length + 3);

                    // We have processed this many bytes
                    _robinBuffer.ConsumeBytes(length + 8);
                    DecodePacket((CommandIds)command, currentDataSection, length);
                }
            }
        }

        private UInt16 CalcCrc(byte[] buffer, ushort startOffset, int bufferSize)
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
            Array.ConstrainedCopy(data, 0, sendData, 4, length);
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
            if (_currentDataMask == 0)
            {
                SendCommand(CommandIds.GetDefaultOutputMask, new byte[0], 0);
            }

            Console.WriteLine($"Found a packet with command {command} of length {length}");
            if (command == CommandIds.Ack)
            {
                int ackCode = currentPacketBuffer[0];
                if (ackCode != 0)
                {
                    Console.WriteLine($"Got a NACK reply with error code {ackCode}");
                }
            }

            if (command == CommandIds.RetDefaultOutputMask)
            {
                if (length != 4)
                {
                    Console.WriteLine("Decoder Error. Payload length for Default Output Mask must be 4");
                    return;
                }

                UInt32 mask = BinaryPrimitives.ReadUInt32LittleEndian(currentPacketBuffer);
                Console.WriteLine($"Configured data mask is {mask}.");
                _currentDataMask = mask;
                _dataMaskReceived = true;
            }

            if (command == CommandIds.RetOutputMode)
            {
                _outputMode = currentPacketBuffer[0];
                _outputModeReceived = true;
            }
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
    }
}
