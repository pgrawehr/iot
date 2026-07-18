// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net.Sockets;
using System.Threading;
using Iot.Device.Common;
using Iot.Device.Nmea0183;
using Iot.Device.Nmea0183.Sentences;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace Iot.Device.Gps.NeoM8Samples
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            LogDispatcher.LoggerFactory = new SimpleConsoleLoggerFactory(LogLevel.Trace);
            // UsingNeoM8Serial();
            // UsingNetwork();
            UsingNmea2000RawNetwork();
        }

        private static void UsingSerial()
        {
            DateTimeOffset lastMessageTime = DateTimeOffset.UtcNow;
            using (var sp = new SerialPort("/dev/ttyS0"))
            {
                sp.NewLine = "\r\n";
                sp.Open();

                // Device streams continuously and therefore most of the time we would end up in the middle of the line
                // therefore ignore first line so that we align correctly
                sp.ReadLine();

                bool gotRmc = false;
                while (!gotRmc)
                {
                    string line = sp.ReadLine();
                    TalkerSentence? sentence = TalkerSentence.FromSentenceString(line, out _);

                    if (sentence == null)
                    {
                        continue;
                    }

                    object? typed = sentence.TryGetTypedValue(ref lastMessageTime);
                    if (typed == null)
                    {
                        Console.WriteLine($"Sentence identifier `{sentence.Id}` is not known.");
                    }
                    else if (typed is RecommendedMinimumNavigationInformation rmc)
                    {
                        gotRmc = true;

                        if (rmc.Position.ContainsValidPosition())
                        {
                            Console.WriteLine($"Your location: {rmc.Position}");
                        }
                        else
                        {
                            Console.WriteLine($"You cannot be located.");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Sentence of type `{typed.GetType().FullName}` not handled.");
                    }
                }
            }
        }

        private static void UsingNetwork()
        {
            try
            {
                // using (TcpClient client = new TcpClient("192.168.1.43", 10110))
                using (TcpClient client = new TcpClient("127.0.0.1", 10110))
                {
                    Console.WriteLine("Connected!");
                    var stream = client.GetStream();
                    bool closed = false;
                    using (NmeaParser parser = new Nmea0183Parser("Test", stream, stream))
                    {
                        parser.OnParserError += (source, msg, error) =>
                        {
                            Console.WriteLine($"Error while parsing message '{msg}': {error}");
                            if (error == NmeaError.PortClosed)
                            {
                                closed = true;
                            }
                        };
                        parser.OnNewSequence += ParserOnNewSequence;
                        parser.StartDecode();
                        while (!Console.KeyAvailable && !closed)
                        {
                            Thread.Sleep(1000);
                        }
                    }
                }
            }
            catch (SocketException x)
            {
                Console.WriteLine($"Error connecting to host: {x}");
            }
        }

        private static void UsingNmea2000RawNetwork()
        {
            try
            {
                // using (TcpClient client = new TcpClient("192.168.1.43", 10110))
                using (NmeaTcpClient client = new NmeaTcpClient("Test", "192.168.121.50", 1457, new Nmea2000YdwgParserFactory()))
                {
                    bool closed = false;
                    Console.WriteLine("Connected!");
                    {
                        client.OnParserError += (source, msg, error) =>
                        {
                            Console.WriteLine($"Error while parsing message '{msg}': {error}");
                            if (error == NmeaError.PortClosed)
                            {
                                closed = true;
                            }
                        };
                        client.OnNewSequence += ParserOnNewSequence;
                        client.StartDecode();

                        AutopilotStatus statusNow = AutopilotStatus.Offline;
                        bool exit = false;
                        while (!exit && !closed)
                        {
                            Thread.Sleep(100);

                            if (Console.KeyAvailable)
                            {
                                var k = Console.ReadKey(true);
                                switch (k.Key)
                                {
                                    case ConsoleKey.Q:
                                        exit = true;
                                        break;
                                    case ConsoleKey.S:
                                        statusNow = AutopilotStatus.Standby;
                                        break;
                                    case ConsoleKey.A:
                                        statusNow = AutopilotStatus.Auto;
                                        break;
                                    case ConsoleKey.W:
                                        statusNow = AutopilotStatus.Wind;
                                        break;
                                    case ConsoleKey.O:
                                        statusNow = AutopilotStatus.Offline;
                                        break;
                                }

                                Console.WriteLine($"New status: {statusNow}!");
                            }

                            if (statusNow != AutopilotStatus.Offline)
                            {
                                SeatalkNgPilotStatus status = new SeatalkNgPilotStatus(statusNow);
                                client.SendSentence(status);
                                var heading = new SeatalkNgPilotHeading(null, Angle.FromDegrees(105.2));
                                client.SendSentence(heading);
                                var heading2 =
                                    new SeatalkNgPilotLockedHeading(null, Angle.FromDegrees(221));
                                client.SendSentence(heading2);
                            }
                        }
                    }
                }
            }
            catch (SocketException x)
            {
                Console.WriteLine($"Error connecting to host: {x}");
            }
        }

        private static void ParserOnNewSequence(NmeaSinkAndSource parser, NmeaSentence sentence)
        {
            Console.WriteLine(sentence.ToReadableContent());
        }
    }
}
