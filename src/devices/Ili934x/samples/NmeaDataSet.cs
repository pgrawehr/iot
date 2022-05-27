// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Iot.Device.Nmea0183;
using UnitsNet;

namespace Iot.Device.Ili934x.Samples
{
    internal abstract class NmeaDataSet
    {
        public NmeaDataSet(string name)
        {
            Name = name;
        }

        public string Name
        {
            get;
        }

        public abstract string Value
        {
            get;
        }

        public abstract string Unit
        {
            get;
        }

        public abstract void Update(SentenceCache cache);
    }
}
