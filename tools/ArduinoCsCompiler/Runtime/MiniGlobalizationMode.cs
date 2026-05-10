// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoCsCompiler.Runtime
{
    [ArduinoReplacement("System.Globalization.GlobalizationMode", null, true, TargetFramework = TargetFramework.Nano)]
    internal static class MiniGlobalizationMode
    {
        // Note: Invariant=true and Invariant=false are substituted at different levels in the ILLink.Substitutions file.
        // This allows for the whole Settings nested class to be trimmed when Invariant=true, and allows for the Settings
        // static cctor (on Unix) to be preserved when Invariant=false.
        internal static bool Invariant => true;

        internal static bool PredefinedCulturesOnly => true;

        private static bool TryGetAppLocalIcuSwitchValue([NotNullWhen(true)] out string? value) =>
            TryGetStringValue("System.Globalization.AppLocalIcu", "DOTNET_SYSTEM_GLOBALIZATION_APPLOCALICU", out value);
        private static bool TryGetStringValue(string switchName, string envVariable, [NotNullWhen(true)] out string? value)
        {
            value = Environment.GetEnvironmentVariable(envVariable);
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            return true;
        }

        private static void LoadAppLocalIcu(string icuSuffixAndVersion)
        {
            // Nothing to do here
        }

        internal static bool UseNls { get; } = !Invariant &&
                                                !LoadIcu();

        private static bool LoadIcu()
        {
            return false;
        }
    }
}
