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
    public delegate void RegisterDefaultConversionsDelegate(UnitConverter converter);

    public delegate void MapGeneratedLocalizations(UnitAbbreviationsCache cache);

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
                MapGeneratedLocalizations c = GenerateLocalizationMethod<Temperature>();
                c.Invoke(MiniUnsafe.As<UnitAbbreviationsCache>(cache));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        [ArduinoCompileTimeConstant]
        public static MapGeneratedLocalizations GenerateLocalizationMethod<T>()
        {
            var method = typeof(T).GetMethod("MapGeneratedLocalizations", BindingFlags.NonPublic | BindingFlags.Static);
            if (method == null)
            {
                throw new NotImplementedException();
            }

            var del = method.CreateDelegate(typeof(MapGeneratedLocalizations));

            return (MapGeneratedLocalizations)del;
        }

        /// <summary>
        /// Just test code below, to see what code we need to generate
        /// </summary>
        public static MapGeneratedLocalizations ReturnDelegate()
        {
            return MethodToReturn;
        }

        public static void MethodToReturn(UnitAbbreviationsCache cache)
        {
        }
    }
}
