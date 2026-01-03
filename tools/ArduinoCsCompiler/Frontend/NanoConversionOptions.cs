// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using CommandLine;

namespace ArduinoCsCompiler
{
    [Verb("nano", HelpText = "Compile code, output IL (intermediate language) to be fed to another compiler, eg. the nanoFramework metadata processor.")]
    internal class NanoConversionOptions : OptionsBase
    {
        public NanoConversionOptions()
        {
            EntryPoint = string.Empty;
            InputAssembly = string.Empty;
            IlOutputFile = string.Empty;
        }

        [Option("ilout", HelpText = "IL output file", Required = true, Default = "")]
        public string IlOutputFile { get; set; }

        [Value(0, HelpText = "Input file/assembly. A dll or exe file containing the startup code", Required = true, MetaName = "StartupAssembly")]
        public string InputAssembly { get; set; }

        [Option('e', "entrypoint", HelpText = "Entry point of program. Must be the name of a static method taking no arguments or a single string[] array.", Default = "Main")]
        public string EntryPoint { get; set; }

        [Option('c', "culture", HelpText = "The name of the culture to use for 'CultureInfo.CurrentCulture'. Must be a valid culture name such as 'de-CH' or 'Invariant'. " +
                                           "Defaults to the current culture during compile.")]
        public string? CultureName { get; set; }
    }
}
