// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

namespace ArduinoCsCompiler.Runtime.UnitsNet
{
    [ArduinoReplacement(typeof(Quantity), false, IncludingPrivates = true)]
    public static class MiniQuantity
    {
        /// <summary>
        /// The default cctor links all unit types -> bad
        /// </summary>
        [ArduinoImplementation]
        static MiniQuantity()
        {
        }

        [ArduinoImplementation]
        public static IEnumerable<Type> GetQuantityTypes()
        {
            return new Type[] { typeof(Temperature) };
        }
    }
}
