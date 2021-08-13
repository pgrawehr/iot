using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.Nmea0183.Sentences;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
{
    /// <summary>
    /// This server distributes all incoming messages via UDP.
    /// </summary>
    public class NmeaUdpServer : NmeaSinkAndSource
    {
        private readonly int _port;
        private UdpClient? _server;
        private NmeaParser? _parser;
        private UdpClientStream? _clientStream;

        public NmeaUdpServer(string name)
        : this(name, 10110)
        {
        }

        public NmeaUdpServer(string name, int port)
        : base(name)
        {
            _port = port;
        }

        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }

            return IPAddress.Loopback;
        }

        public override void StartDecode()
        {
            if (_server != null)
            {
                throw new InvalidOperationException("Server already started");
            }

            _server = new UdpClient(_port);
            _server.DontFragment = true;
            _clientStream = new UdpClientStream(_server, _port);
            _parser = new NmeaParser($"{InterfaceName} (Port {_port})", _clientStream, _clientStream);
            _parser.OnNewSequence += OnSentenceReceivedFromClient;
            _parser.OnParserError += ParserOnParserError;
            _parser.StartDecode();
        }

        private void ParserOnParserError(NmeaSinkAndSource source, string message, NmeaError errorCode)
        {
            FireOnParserError(message, errorCode);
        }

        private void OnSentenceReceivedFromClient(NmeaSinkAndSource source, NmeaSentence sentence)
        {
            DispatchSentenceEvents(sentence);
        }

        /// <summary>
        /// Sends the sentence to all our clients.
        /// If it is needed to make distinctions for what needs to be sent to which client, create
        /// multiple server instances. This will allow for proper filtering.
        /// </summary>
        /// <param name="source">The original source of the message, used i.e. for logging</param>
        /// <param name="sentence">The sentence to send</param>
        public override void SendSentence(NmeaSinkAndSource source, NmeaSentence sentence)
        {
            if (_parser == null)
            {
                return;
            }

            try
            {
                _parser.SendSentence(source, sentence);
            }
            catch (IOException x)
            {
                FireOnParserError($"Error sending message to {_parser.InterfaceName}: {x.Message}",
                    NmeaError.PortClosed);
            }
        }

        public override void StopDecode()
        {
            if (_parser != null)
            {
                _parser.StopDecode();
                _parser.Dispose();
            }

            if (_server != null && _clientStream != null)
            {
                _server.Dispose();
                _clientStream.Dispose();
                _server = null;
                _clientStream = null;
            }

            _parser = null;
        }

        private sealed class UdpClientStream : Stream, IDisposable
        {
            private readonly UdpClient _client;
            private readonly int _port;
            private readonly Queue<byte> _data;

            private Stopwatch _lastUnsuccessfulSend;
            private Dictionary<IPAddress, bool> _knownSenders;

            public UdpClientStream(UdpClient client, int port)
            {
                _client = client;
                _port = port;
                _data = new Queue<byte>();
                _knownSenders = new();
                _lastUnsuccessfulSend = new Stopwatch();
            }

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int bytesRemaining = count;
                int bytesAdded = 0;
                while (_data.Count > 0 && bytesRemaining > 0)
                {
                    buffer[offset++] = _data.Dequeue();
                    bytesAdded++;
                    bytesRemaining--;
                }

                if (bytesAdded > 0)
                {
                    return bytesAdded;
                }

                IPEndPoint pt;
                byte[] datagram;
                bool isself;
                while (true)
                {
                    pt = new IPEndPoint(IPAddress.Any, _port);
                    try
                    {
                        datagram = _client.Receive(ref pt);
                    }
                    catch (SocketException)
                    {
                        return 0;
                    }

                    if (_knownSenders.TryGetValue(pt.Address, out isself))
                    {
                        if (isself)
                        {
                            continue;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // Check whether the given address is ours (new IPs can be added at runtime, if interfaces go up)
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    if (host.AddressList.Contains(pt.Address))
                    {
                        _knownSenders.Add(pt.Address, true);
                    }
                    else
                    {
                        _knownSenders.Add(pt.Address, false);
                    }
                }

                // Does the whole message fit in the buffer?
                if (bytesRemaining >= datagram.Length)
                {
                    Array.Copy(datagram, 0, buffer, offset, datagram.Length);
                    return datagram.Length;
                }

                foreach (var b in datagram)
                {
                    _data.Enqueue(b);
                }

                // Shouldn't normally happen here
                if (_data.Count == 0)
                {
                    return 0;
                }

                // Recurse to execute the first part of this method
                return Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotSupportedException("Cannot seek on a Udp Stream");
            }

            public override void SetLength(long value)
            {
                throw new NotSupportedException("Cannot set length");
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                if (_lastUnsuccessfulSend.IsRunning && _lastUnsuccessfulSend.Elapsed < TimeSpan.FromMinutes(1))
                {
                    return;
                }

                byte[] tempBuf = buffer;
                if (offset != 0)
                {
                    tempBuf = new byte[count];
                    Array.Copy(buffer, offset, tempBuf, 0, count);
                }

                try
                {
                    IPEndPoint pt = new IPEndPoint(IPAddress.Broadcast, _port);
                    _client.Send(tempBuf, count, pt);
                    _lastUnsuccessfulSend.Stop();
                }
                catch (SocketException x)
                {
                    Console.WriteLine($"Exception sending to UDP port: {x.Message}");
                    _lastUnsuccessfulSend.Reset();
                    _lastUnsuccessfulSend.Start();
                }
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => true;
            public override long Length => 0;
            public override long Position { get; set; }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _client.Dispose();
                }

                base.Dispose(disposing);
            }
        }
    }
}
