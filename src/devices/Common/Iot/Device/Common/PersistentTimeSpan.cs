// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;

#pragma warning disable CS1591
namespace Iot.Device.Common
{
    public class PersistentTimeSpan : PersistentValue<TimeSpan>
    {
        public PersistentTimeSpan(PersistenceFile? file, string name, TimeSpan initialValue, TimeSpan saveInterval)
            : base(file, name, initialValue, saveInterval, Serializer, Deserializer)
        {
        }

        private static string Serializer(TimeSpan value)
        {
            return value.ToString("G", CultureInfo.InvariantCulture);
        }

        private static bool Deserializer(string data, out TimeSpan value)
        {
            return TimeSpan.TryParse(data, out value);
        }
    }
}
