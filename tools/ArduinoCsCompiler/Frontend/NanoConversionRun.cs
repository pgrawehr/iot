// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Iot.Device.Arduino;
using Microsoft.Extensions.Logging;
using UnitsNet;

namespace ArduinoCsCompiler
{
    internal class NanoConversionRun : Run<NanoConversionOptions>
    {
        private MicroCompiler? _compiler;

        public NanoConversionRun(NanoConversionOptions commandLineOptions)
        : base(commandLineOptions)
        {
        }

        protected override void Dispose(bool disposing)
        {
            _compiler?.Dispose();
            _compiler = null;
            base.Dispose(disposing);
        }

        public override bool RunCommand()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ErrorManager.AddWarning("ACS0002", "This compiler is currently supported on Windows only. The target CPU may be anything, but the compiler is only tested on Windows. " +
                                        "You might experience build or runtime failures otherwise.");
            }

            try
            {
                _compiler = new MicroCompiler(null, false, true);

                FileInfo inputInfo = new FileInfo(CommandLineOptions.InputAssembly);
                if (!inputInfo.Exists)
                {
                    ErrorManager.AddError("ACS0003", $"Could not find file {CommandLineOptions.InputAssembly}. (Looking at absolute path {inputInfo.FullName})");
                    return false;
                }

                // If an exe file was specified, use the matching .dll instead (this is the file containing actual .NET code in Net 5.0+)
                if (inputInfo.Extension.ToUpper() == ".EXE")
                {
                    inputInfo = new FileInfo(Path.ChangeExtension(CommandLineOptions.InputAssembly, ".dll"));
                }

                if (!inputInfo.Exists)
                {
                    Logger.LogError($"Could not find file {inputInfo}.");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(CommandLineOptions.IlOutputFile))
                {
                    ErrorManager.AddError("ACS0100", $"No output file argument provided");
                    return false;
                }

                if (!string.IsNullOrWhiteSpace(CommandLineOptions.CultureName))
                {
                    // Let this throw for now if it is invalid
                    CultureInfo c;
                    if (CommandLineOptions.CultureName.Equals("Invariant", StringComparison.OrdinalIgnoreCase))
                    {
                        c = CultureInfo.InvariantCulture;
                    }
                    else
                    {
                        c = new CultureInfo(CommandLineOptions.CultureName);
                    }

                    // We're running single-threaded, so just set the default culture.
                    Console.WriteLine($"Setting compiler culture to {c.DisplayName}");
                    Thread.CurrentThread.CurrentCulture = c;
                }

                RunCompiler(inputInfo);
            }
#if !DEBUG
            catch (Exception x) when (!(x is NullReferenceException))
            {
                Logger.LogError(x.Message);
                return false;
            }
#endif
            finally
            {
            }

            Console.WriteLine("Exiting with code 0");
            return true;
        }

        private void RunCompiler(FileInfo inputInfo)
        {
            if (_compiler == null)
            {
                throw new InvalidProgramException("Internal error - compiler not ready");
            }

            var assemblyUnderTest = Assembly.LoadFrom(inputInfo.FullName);
            MethodInfo startup = LocateStartupMethod(assemblyUnderTest);
            Logger.LogDebug($"Startup method is {startup.MethodSignature(true)}");
            var settings = new CompilerSettings()
            {
                AutoRestartProgram = true,
                CreateKernelForFlashing = false,
                ForceFlashWrite = false,
                LaunchProgramFromFlash = false,
                UseFlashForProgram = true,
                UsePreviewFeatures = true,
                AdditionalSuppressions = new List<string>(),
                ProcessName = inputInfo.Name,
                SupportGenerics = true,
            };

            Logger.LogInformation("Collecting method information and metadata...");
            ExecutionSet set = _compiler.PrepareProgram(startup, settings);

            Logger.LogInformation("Done processing input. Now writing output file...");
            if (!string.IsNullOrWhiteSpace(CommandLineOptions.IlOutputFile))
            {
                var ilWriter = new IlWriter(set, CommandLineOptions.IlOutputFile);
                ilWriter.Write();
            }

            Logger.LogInformation($"Compile successful. {ErrorManager.NumErrors} Errors, {ErrorManager.NumWarnings} Warnings");
        }

        private MethodInfo LocateStartupMethod(Assembly assemblyUnderTest)
        {
            string className = string.Empty;
            Type? startupType = null;
            if (CommandLineOptions.EntryPoint.Contains(".", StringComparison.InvariantCultureIgnoreCase))
            {
                int idx = CommandLineOptions.EntryPoint.LastIndexOf(".", StringComparison.InvariantCultureIgnoreCase);
                className = CommandLineOptions.EntryPoint.Substring(0, idx);
                string methodName = CommandLineOptions.EntryPoint.Substring(idx + 1);
                startupType = assemblyUnderTest.GetType(className, true);

                if (startupType == null)
                {
                    Logger.LogError($"Unable to locate startup class {className}");
                    Abort();
                }

                var mi = startupType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                if (mi == null)
                {
                    Logger.LogError($"Unable to find a static method named {methodName} in {className}");
                    Abort();
                }

                return mi;
            }

            foreach (var cl in assemblyUnderTest.GetTypes())
            {
                var method = cl.GetMethod(CommandLineOptions.EntryPoint, BindingFlags.Public | BindingFlags.Static);
                if (method != null)
                {
                    return method;
                }
            }

            Logger.LogError($"Unable to find a static method named {CommandLineOptions.EntryPoint} in any class within {assemblyUnderTest.FullName}.");
            Abort();
            return null!;
        }

        [DoesNotReturn]
        private void Abort()
        {
            throw new InvalidOperationException("Error compiling or running code, see previous messages");
        }
    }
}
