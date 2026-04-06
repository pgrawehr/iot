// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

#pragma warning disable CA1416 // Location is reachable on all platforms
namespace ArduinoCsCompiler.Runtime
{
    /// <summary>
    /// Replaces the more elaborate Microsoft.Win32.Registry. Not to be confused with a similar, but simpler implementation
    /// in the core (which is internal there)
    /// </summary>
    [ArduinoReplacement(typeof(Microsoft.Win32.Registry), true, TargetFramework = TargetFramework.Nano)]
    internal static class MiniRegistry2
    {
        /// <summary>Current User Key. This key should be used as the root for all user specific settings.</summary>
        public static readonly MiniRegistryKeyInternal2 CurrentUser = MiniRegistryKeyInternal2.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Default);

        /// <summary>Local Machine key. This key should be used as the root for all machine specific settings.</summary>
        public static readonly MiniRegistryKeyInternal2 LocalMachine = MiniRegistryKeyInternal2.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default);

        /// <summary>Classes Root Key. This is the root key of class information.</summary>
        public static readonly MiniRegistryKeyInternal2 ClassesRoot = MiniRegistryKeyInternal2.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default);

        /// <summary>Users Root Key. This is the root of users.</summary>
        public static readonly MiniRegistryKeyInternal2 Users = MiniRegistryKeyInternal2.OpenBaseKey(RegistryHive.Users, RegistryView.Default);

        /// <summary>Performance Root Key. This is where dynamic performance data is stored on NT.</summary>
        public static readonly MiniRegistryKeyInternal2 PerformanceData = MiniRegistryKeyInternal2.OpenBaseKey(RegistryHive.PerformanceData, RegistryView.Default);

        /// <summary>Current Config Root Key. This is where current configuration information is stored.</summary>
        public static readonly MiniRegistryKeyInternal2 CurrentConfig = MiniRegistryKeyInternal2.OpenBaseKey(RegistryHive.CurrentConfig, RegistryView.Default);

        /// <summary>
        /// Parse a keyName and returns the basekey for it.
        /// It will also store the subkey name in the out parameter.
        /// If the keyName is not valid, we will throw ArgumentException.
        /// The return value shouldn't be null.
        /// </summary>
        private static MiniRegistryKeyInternal2 GetBaseKeyFromKeyName(string keyName, out string subKeyName)
        {
            ArgumentNullException.ThrowIfNull(keyName);

            int i = keyName.IndexOf('\\');
            int length = i != -1 ? i : keyName.Length;

            // Determine the potential base key from the length.
            MiniRegistryKeyInternal2? baseKey = null;
            switch (length)
            {
                case 10:
                    baseKey = Users;
                    break; // HKEY_USERS
                case 17:
                    baseKey = char.ToUpperInvariant(keyName[6]) == 'L' ? ClassesRoot : CurrentUser;
                    break; // HKEY_C[L]ASSES_ROOT, otherwise HKEY_CURRENT_USER
                case 18:
                    baseKey = LocalMachine;
                    break; // HKEY_LOCAL_MACHINE
                case 19:
                    baseKey = CurrentConfig;
                    break; // HKEY_CURRENT_CONFIG
                case 21:
                    baseKey = PerformanceData;
                    break; // HKEY_PERFORMANCE_DATA
            }

            // If a potential base key was found, see if keyName actually starts with the potential base key's name.
            if (baseKey != null && keyName.StartsWith(baseKey.Name, StringComparison.OrdinalIgnoreCase))
            {
                subKeyName = (i == -1 || i == keyName.Length) ?
                    string.Empty :
                    keyName.Substring(i + 1);

                return baseKey;
            }

            throw new ArgumentException("Invalid key name", nameof(keyName));
        }

        public static object? GetValue(string keyName, string? valueName, object? defaultValue)
        {
            MiniRegistryKeyInternal2 basekey = GetBaseKeyFromKeyName(keyName, out string subKeyName);

            using (MiniRegistryKeyInternal2? key = basekey.OpenSubKey(subKeyName))
            {
                return key?.GetValue(valueName, defaultValue);
            }
        }

        public static void SetValue(string keyName, string? valueName, object value)
        {
            SetValue(keyName, valueName, value, RegistryValueKind.Unknown);
        }

        public static void SetValue(string keyName, string? valueName, object value, RegistryValueKind valueKind)
        {
            MiniRegistryKeyInternal2 basekey = GetBaseKeyFromKeyName(keyName, out string subKeyName);

            using (MiniRegistryKeyInternal2? key = basekey.CreateSubKey(subKeyName))
            {
                Debug.Assert(key != null, "An exception should be thrown if failed!");
                key.SetValue(valueName, value, valueKind);
            }
        }
    }
}
