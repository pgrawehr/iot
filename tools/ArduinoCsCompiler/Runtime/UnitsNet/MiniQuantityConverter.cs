// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnitsNet;

namespace ArduinoCsCompiler.Runtime.UnitsNet
{
    [ArduinoReplacement(typeof(UnitConverter), false, IncludingPrivates = true)]
    internal class MiniQuantityConverter
    {
        /// <summary>
        /// Registers the default conversion functions in the given <see cref="UnitConverter"/> instance.
        /// </summary>
        /// <param name="unitConverter">The <see cref="UnitConverter"/> to register the default conversion functions in.</param>
        [ArduinoImplementation]
        public static void RegisterDefaultConversions(UnitConverter unitConverter)
        {
            if (unitConverter is null)
            {
                throw new ArgumentNullException(nameof(unitConverter));
            }

            foreach (var quantity in MiniQuantity.GetQuantityTypes())
            {
                MiniQuantity.RegisterDefaultConversionsInternal(quantity, unitConverter);
            }
        }
    }
}
