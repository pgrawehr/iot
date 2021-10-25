﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using CommandLine;
using Iot.Device.Arduino;
using Iot.Device.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UnitsNet;

namespace ArduinoCsCompiler
{
    internal sealed class Program : IDisposable
    {
        private readonly CommandLineOptions _commandLineOptions;
        private ILogger _logger;
        private MicroCompiler _compiler;

        private Program(CommandLineOptions commandLineOptions)
        {
            _commandLineOptions = commandLineOptions;
            _logger = this.GetCurrentClassLogger();
        }

        private static int Main(string[] args)
        {
            Assembly? entry = Assembly.GetEntryAssembly();
            var version = (AssemblyFileVersionAttribute)Attribute.GetCustomAttribute(entry, typeof(AssemblyFileVersionAttribute));
            if (version == null)
            {
                throw new InvalidProgramException("Invalid program state - no version attribute");
            }

            Console.WriteLine($"ArduinoCsCompiler - Version {version.Version}");

            LogDispatcher.LoggerFactory = new SimpleConsoleLoggerFactory();
            int errorCode = 0;

            var result = Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed<CommandLineOptions>(o =>
                {
                    using var program = new Program(o);
                    errorCode = program.ConnectToBoard(o);
                });

            if (result.Tag != ParserResultType.Parsed)
            {
                Console.WriteLine("Command line parsing error");
                return 1;
            }

            return errorCode;
        }

        public void Dispose()
        {
            _compiler?.Dispose();
        }

        private int ConnectToBoard(CommandLineOptions commandLineOptions)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _logger.LogWarning("This compiler is currently supported on Windows only. The target CPU may be anything, but the compiler is only tested on Windows.");
                _logger.LogWarning("You might experience build or runtime failures otherwise.");
            }

            _logger.LogInformation("Connecting to board...");
            ArduinoBoard? board = null;
            try
            {
                List<int> usableBaudRates = ArduinoBoard.CommonBaudRates();
                if (commandLineOptions.Baudrate != 0)
                {
                    usableBaudRates.Clear();
                    usableBaudRates.Add(commandLineOptions.Baudrate);
                }

                List<string> usablePorts = SerialPort.GetPortNames().ToList();
                if (!string.IsNullOrWhiteSpace(commandLineOptions.Port))
                {
                    usablePorts.Clear();
                    usablePorts.Add(commandLineOptions.Port);
                }

                if (!string.IsNullOrWhiteSpace(commandLineOptions.NetworkAddress))
                {
                    string[] splits = commandLineOptions.NetworkAddress.Split(':', StringSplitOptions.TrimEntries);
                    int networkPort = 27016;
                    if (splits.Length > 1)
                    {
                        if (!int.TryParse(splits[1], out networkPort))
                        {
                            _logger.LogError($"Error: {splits[0]} is not a valid network port number.");
                            return 1;
                        }
                    }

                    string host = splits[0];
                    IPAddress? ip = Dns.GetHostAddresses(host).FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);
                    if (ip == null)
                    {
                        _logger.LogError($"Error: Unable to resolve host {host}.");
                        return 1;
                    }

                    if (!ArduinoBoard.TryConnectToNetworkedBoard(ip, networkPort, out board))
                    {
                        _logger.LogError($"Couldn't connect to board at {commandLineOptions.NetworkAddress}");
                        return 1;
                    }
                }
                else
                {
                    if (!TryFindBoard(usablePorts, usableBaudRates, out board))
                    {
                        _logger.LogError($"Couldn't find Arduino with Firmata firmware on any specified UART.");
                        return 1;
                    }
                }

                _logger.LogInformation($"Connected to Board with firmware {board.FirmwareName} version {board.FirmwareVersion}.");
                _compiler = new MicroCompiler(board, true);

                if (!_compiler.QueryBoardCapabilities(out var caps))
                {
                    _logger.LogError("Couldn't query board capabilities. Possibly incompatible firmware");
                    return 1;
                }

                _logger.LogInformation($"Board reports {caps.FlashSize.Kilobytes} kB of program flash memory and {caps.RamSize.Kilobytes} kB of RAM");
                _logger.LogInformation($"Recommended minimum values are 512 kB of flash and 100 kB of RAM. Some microcontrollers (e.g. ESP32) may allow configuration of the amount of flash " +
                                       $"available for code.");

                if (caps.IntSize != Information.FromBytes(4) || caps.PointerSize != Information.FromBytes(4))
                {
                    _logger.LogWarning("Board pointer and/or integer size is not 32 bits. This is untested and may lead to unpredictable behavior.");
                }

                FileInfo inputInfo = new FileInfo(commandLineOptions.InputAssembly);
                if (!inputInfo.Exists)
                {
                    _logger.LogError($"Could not find file {commandLineOptions.InputAssembly}.");
                    return 1;
                }

                // If an exe file was specified, use the matching .dll instead (this is the file containing actual .NET code in Net 5.0)
                if (inputInfo.Extension.ToUpper() == ".EXE")
                {
                    inputInfo = new FileInfo(Path.ChangeExtension(commandLineOptions.InputAssembly, ".dll"));
                }

                if (!inputInfo.Exists)
                {
                    _logger.LogError($"Could not find file {inputInfo}.");
                    return 1;
                }

                RunCompiler(inputInfo);
            }
            catch (Exception x) when (!(x is NullReferenceException))
            {
                _logger.LogError(x.Message);
                return 1;
            }
            finally
            {
                board?.Dispose();
            }

            Console.WriteLine("Exiting with code 0");
            return 0;
        }

        private void RunCompiler(FileInfo inputInfo)
        {
            var assemblyUnderTest = Assembly.LoadFrom(inputInfo.FullName);
            MethodInfo startup = LocateStartupMethod(assemblyUnderTest);
            _logger.LogDebug($"Startup method is {startup.MethodSignature(true)}");
            var settings = new CompilerSettings()
            {
                AutoRestartProgram = true,
                CreateKernelForFlashing = false,
                ForceFlashWrite = true,
                LaunchProgramFromFlash = true,
                UseFlashForProgram = true
            };

            _logger.LogInformation("Collecting method information and metadata...");
            ExecutionSet set = _compiler.CreateExecutionSet(startup, settings);

            long estimatedSize = set.EstimateRequiredMemory(out var stats);

            _logger.LogInformation($"Estimated program size in flash is {estimatedSize}. The actual size will be known after upload only");

            foreach (var stat in stats)
            {
                _logger.LogDebug($"Class {stat.Key.FullName}: {stat.Value.TotalBytes} Bytes");
            }

            if (!_commandLineOptions.CompileOnly)
            {
                set.Load();
            }
        }

        private MethodInfo LocateStartupMethod(Assembly assemblyUnderTest)
        {
            string className = string.Empty;
            Type? startupType = null;
            if (_commandLineOptions.EntryPoint.Contains(".", StringComparison.InvariantCultureIgnoreCase))
            {
                int idx = _commandLineOptions.EntryPoint.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase);
                className = _commandLineOptions.EntryPoint.Substring(0, idx);
                string methodName = _commandLineOptions.EntryPoint.Substring(idx + 1);
                startupType = assemblyUnderTest.GetType(className, true);
                var mi = startupType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (mi == null)
                {
                    _logger.LogError($"Unable to find a static method named {methodName} in {className}");
                    Abort();
                }

                return mi;
            }

            foreach (var cl in assemblyUnderTest.GetTypes())
            {
                var method = cl.GetMethod(_commandLineOptions.EntryPoint, BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    return method;
                }
            }

            _logger.LogError($"Unable to find a static method named {_commandLineOptions.EntryPoint} in any class within {assemblyUnderTest.FullName}.");
            Abort();
            return null!;
        }

        private void Abort()
        {
            throw new InvalidOperationException("Error compiling code, see previous messages");
        }

        private bool TryFindBoard(IEnumerable<string> comPorts, IEnumerable<int> baudRates,
            out ArduinoBoard? board)
        {
            // We do the iteration here ourselves, so we can write out progress, because it may be very slow in auto-detect mode.
            // TODO: Maybe improve
            foreach (var port in comPorts)
            {
                foreach (var baud in baudRates)
                {
                    _logger.LogInformation($"Trying port {port} at {baud} Baud...");
                    if (ArduinoBoard.TryFindBoard(new string[] { port }, new int[] { baud }, out board))
                    {
                        return true;
                    }
                }
            }

            board = null!;
            return false;
        }
    }
}