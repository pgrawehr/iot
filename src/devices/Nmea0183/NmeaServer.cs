using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.Nmea0183.Sentences;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183
{
    public class NmeaServer : NmeaSinkAndSource
    {
        private readonly IPAddress _bindTo;
        private readonly int _port;
        private readonly List<NmeaSinkAndSource> _activeParsers;
        private readonly object _lock;
        private TcpListener _server;
        private Thread _serverThread;
        private Thread _serverControlThread;
        private AutoResetEvent _serverControlEvent;
        private ConcurrentQueue<Task> _serverTasks;
        private bool _terminated;

        public NmeaServer(string name)
        : this(name, IPAddress.Any, 10110)
        {
        }

        public NmeaServer(string name, IPAddress bindTo, int port)
        : base(name)
        {
            _bindTo = bindTo;
            _port = port;
            _activeParsers = new List<NmeaSinkAndSource>();
            _lock = new object();
            _serverControlEvent = new AutoResetEvent(false);
            _serverTasks = new ConcurrentQueue<Task>();
        }

        public override void StartDecode()
        {
            if (_serverThread != null)
            {
                throw new InvalidOperationException("Server already started");
            }

            _terminated = false;
            _server = new TcpListener(_bindTo, _port);
            _server.Start();
            _serverThread = new Thread(ConnectionWatcher);
            _serverThread.Start();

            _serverControlThread = new Thread(ServerControl);
            _serverControlThread.Start();
        }

        private void ConnectionWatcher()
        {
            while (!_terminated)
            {
                try
                {
                    var client = _server.AcceptTcpClient();
                    lock (_lock)
                    {
                        NmeaParser parser = new NmeaParser(_activeParsers.Count.ToString(), client.GetStream(), client.GetStream());
                        parser.OnNewSequence += OnSentenceReceivedFromClient;
                        parser.OnParserError += ParserOnParserError;
                        parser.StartDecode();

                        _activeParsers.Add(parser);
                    }
                }
                catch (SocketException)
                {
                    // Ignore (probably going to close the socket)
                }
            }
        }

        private void ServerControl()
        {
            while (!_terminated)
            {
                _serverControlEvent.WaitOne();
                if (_serverTasks.TryDequeue(out Task task))
                {
                    // Just wait for this to terminate
                    task.Wait();
                }
            }
        }

        private void ParserOnParserError(NmeaSinkAndSource source, string message, NmeaError errorCode)
        {
            if (errorCode == NmeaError.PortClosed)
            {
                Task t = Task.Run(() =>
                {
                    lock (_lock)
                    {
                        _activeParsers.Remove(source);
                    }

                    // Can't do this synchronously, as it would cause a deadlock
                    source.StopDecode();
                });
                _serverTasks.Enqueue(t);
                _serverControlEvent.Set();
            }

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
            lock (_activeParsers)
            {
                foreach (var parser in _activeParsers)
                {
                    parser.SendSentence(source, sentence);
                }
            }
        }

        public override void StopDecode()
        {
            _terminated = true;
            if (_server != null)
            {
                _server.Stop();
                _serverThread.Join();
                _serverControlEvent.Set();
                _serverControlThread.Join();
            }

            while (_serverTasks.TryDequeue(out Task task))
            {
                // Just wait for this to terminate
                task.Wait();
            }

            lock (_lock)
            {
                foreach (var parser in _activeParsers)
                {
                    parser.StopDecode();
                    parser.Dispose();
                }

                _activeParsers.Clear();
            }

            _serverThread = null;
            _server = null;
        }
    }
}
