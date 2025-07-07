// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Iot.Device.Common
{
/// <summary>
/// Persists an integer value (it will always keep its last set value).
/// Can also be used to persist an enum.
/// </summary>
public class PersistentInt : PersistentValue<int>
{
    /// <summary>
    /// Creates a new instance of this type.
    /// </summary>
    /// <param name="file">Name of the persistence file</param>
    /// <param name="name">Value name</param>
    /// <param name="initialValue">Initial value</param>
    public PersistentInt(PersistenceFile file, string name, int initialValue)
        : base(file, name, initialValue, TimeSpan.Zero, Serializer, Deserializer)
    {
    }

    private static bool Deserializer(string data, out int value)
    {
        return Int32.TryParse(data, out value);
    }

    private static string Serializer(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }
}
}
