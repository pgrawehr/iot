// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS1591
namespace Iot.Device.Nmea0183.Sentences
{
    public enum FluidType
    {
        Fuel = 0,
        Water = 1,
        WasteWater = 2,
        BlackWater = 3,
        LiveWell = 4,
        Oil = 5,
    }
}
