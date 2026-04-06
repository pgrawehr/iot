// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace ArduinoCsCompiler.Runtime
{
    [ArduinoReplacement(typeof(Microsoft.Win32.RegistryKey), true, TargetFramework = TargetFramework.Nano)]
    public sealed class MiniRegistryKeyInternal2 : IDisposable
    {
        private string _name;

        public MiniRegistryKeyInternal2(string name)
        {
            _name = name;
        }

        public string Name => _name;

        public static MiniRegistryKeyInternal2 OpenBaseKey(RegistryHive hive, RegistryView view)
        {
            return new MiniRegistryKeyInternal2(hive.ToString());
        }

        public MiniRegistryKeyInternal2 CreateSubKey(string name)
        {
            return new MiniRegistryKeyInternal2(_name + "/" + name);
        }

        public MiniRegistryKeyInternal2 OpenSubKey(string name)
        {
            return new MiniRegistryKeyInternal2(_name + "/" + name);
        }

        public void SetValue(string? name, object value, RegistryValueKind kind)
        {
            // Ignore for now
        }

        public object? GetValue(string? name, object? defaultValue)
        {
            return defaultValue;
        }

        public string[] GetValueNames()
        {
            return new string[0];
        }

        public object? GetValue(string? name)
        {
            return null;
        }

        public void Dispose()
        {
        }
    }
}
