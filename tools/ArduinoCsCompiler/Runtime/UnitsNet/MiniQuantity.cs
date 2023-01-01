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
        public delegate void RegisterDefaultConversionsDelegate(UnitConverter converter);

        public delegate void MapGeneratedLocalizations(MiniUnitAbbreviationsCache cache);

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

        public static void RegisterDefaultConversionsInternal(Type forType, UnitConverter converter)
        {
            if (forType == typeof(Temperature))
            {
                RegisterDefaultConversionsDelegate c = RegisterDefaultConversionsTemplate;
                c.Invoke(converter);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static void RegisterDefaultConversionsTemplate(UnitConverter converter)
        {
        }

        internal static void AddUnitAbbreviations(Type forType, MiniUnitAbbreviationsCache cache)
        {
            if (forType == typeof(Temperature))
            {
                MapGeneratedLocalizations c = MapGeneratedLocalizationsTemplate;
                c.Invoke(cache);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public static void MapGeneratedLocalizationsTemplate(MiniUnitAbbreviationsCache cache)
        {
        }
    }
}
