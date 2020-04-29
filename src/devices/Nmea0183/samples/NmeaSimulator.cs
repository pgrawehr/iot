// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Iot.Device.Nmea0183;
using Iot.Device.Nmea0183.Sentences;
using Units;

namespace Nmea.Simulator
{
    internal class Simulator
    {
        private static readonly TimeSpan UpdateRate = TimeSpan.FromMilliseconds(500);
        private object _lock;
        private List<ParserData> _activeParsers;
        private Thread _simulatorThread;
        private bool _terminate;
        private SimulatorData _activeData;

        public Simulator()
        {
            _lock = new object();
            _activeParsers = new List<ParserData>();
            _activeData = new SimulatorData();
        }

        public static void Main(string[] args)
        {
            var sim = new Simulator();
            sim.StartServer();
        }

        private void StartServer()
        {
            TcpListener server = null;
            try
            {
                _terminate = false;
                _simulatorThread = new Thread(MainSimulator);
                _simulatorThread.Start();
                server = new TcpListener(IPAddress.Any, 10110);
                server.Start();
                bool exit = false;
                Console.WriteLine("Waiting for connections. Press x to quit");
                while (!exit)
                {
                    if (server.Pending())
                    {
                        TcpClient client = server.AcceptTcpClient();
                        NmeaParser parser = new NmeaParser(client.GetStream(), client.GetStream());
                        Thread t = new Thread(SenderThread);
                        ParserData pd = new ParserData(client, parser, t);
                        parser.OnNewSequence += (source, sentence) =>
                        {
                            if (sentence is RawSentence)
                            {
                                Console.WriteLine($"Received message: {sentence.ToReadableContent()}");
                            }
                        };

                        parser.StartDecode();
                        lock (_lock)
                        {
                            _activeParsers.Add(pd);
                            t.Start(pd);
                        }
                    }

                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        if (key.KeyChar == 'x')
                        {
                            break;
                        }
                    }

                    // Console.WriteLine($"Number of active clients: {_activeParsers.Count}");
                    Thread.Sleep(100);
                }
            }
            catch (SocketException x)
            {
                Console.WriteLine($"There was a socket exception listening on the network: {x}");
            }
            finally
            {
                server?.Stop();
                if (_simulatorThread != null)
                {
                    _terminate = true;
                    _simulatorThread?.Join();
                }

                foreach (var p in _activeParsers)
                {
                    p.Dispose();
                }

                _activeParsers.Clear();
            }
        }

        private void SenderThread(object obj)
        {
            ParserData myThreadData = (ParserData)obj;
            NmeaParser parser = myThreadData.Parser;
            while (!myThreadData.TerminateThread)
            {
                if (!myThreadData.TcpClient.Connected)
                {
                    Console.WriteLine("Thread exiting - connection lost");
                    break;
                }

                // This lock keeps the code simple - we do not have to worry about the data being manipulated while we collect it
                // and send it out.
                try
                {
                    lock (_lock)
                    {
                        var data = _activeData;
                        RecommendedMinimumNavigationInformation rmc = new RecommendedMinimumNavigationInformation(DateTimeOffset.UtcNow,
                            RecommendedMinimumNavigationInformation.NavigationStatus.Valid, data.Position.Latitude,
                            data.Position.Longitude, data.Speed.Knots, data.Course, null);
                        SendSentence(parser, rmc);

                        GlobalPositioningSystemFixData gga = new GlobalPositioningSystemFixData(
                            DateTimeOffset.UtcNow, GpsQuality.DifferentialFix, data.Position, data.Position.EllipsoidalHeight - 54,
                            2.5, 10);
                        SendSentence(parser, gga);

                        TimeDate zda = new TimeDate(DateTimeOffset.UtcNow);
                        SendSentence(parser, zda);
                    }
                }
                catch (IOException x)
                {
                    Console.WriteLine($"Error writing to the output stream: {x.Message}. Connection lost.");
                    break;
                }

                Thread.Sleep(UpdateRate);
            }
        }

        private void SendSentence(NmeaParser parser, NmeaSentence sentence)
        {
            Console.WriteLine($"Sending {sentence.ToReadableContent()}");
            parser.SendSentence(sentence);
        }

        private void MainSimulator()
        {
            while (!_terminate)
            {
                var newData = _activeData.Clone();
                GeographicPosition newPosition = GreatCircle.CalcCoords(newData.Position, _activeData.Course.Degrees,
                    _activeData.Speed.MetersPerSecond * UpdateRate.TotalSeconds);
                newData.Position = newPosition;
                lock (_lock)
                {
                    _activeData = newData;
                }

                Thread.Sleep(UpdateRate);
            }
        }

        private sealed class ParserData : IDisposable
        {
            public ParserData(TcpClient tcpClient, NmeaParser parser, Thread thread)
            {
                TcpClient = tcpClient;
                Parser = parser;
                Thread = thread;
                TerminateThread = false;
            }

            public TcpClient TcpClient { get; }
            public NmeaParser Parser { get; }
            public Thread Thread { get; }

            public bool TerminateThread
            {
                get;
                set;
            }

            public void Dispose()
            {
                TcpClient.Close();
                TcpClient.Dispose();
                TerminateThread = true;
                Parser.Dispose();
                Thread.Join();
            }
        }
    }
}
