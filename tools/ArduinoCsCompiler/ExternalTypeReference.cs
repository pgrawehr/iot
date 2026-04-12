// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ArduinoCsCompiler
{
    internal class ExternalTypeReference
    {
        public List<EquatableMethod> Methods { get; }

        public ExternalTypeReference(string name, Type type, ExternalAssemblyReference assembly, bool requiresPrefix)
        {
            Name = name;
            Type = type;
            Assembly = assembly;
            RequiresPrefix = requiresPrefix;
            Methods = new List<EquatableMethod>();
        }

        public ExternalTypeReference(Type type, ExternalAssemblyReference assembly)
            : this(type.FullName!, type, assembly, true)
        {
        }

        public ExternalTypeReference(Type type, List<EquatableMethod> methods, ExternalAssemblyReference assembly)
            : this(type.FullName!, type, assembly, true)
        {
            Methods = methods;
        }

        public string Name { get; }

        public Type Type { get; }

        public ExternalAssemblyReference Assembly { get; }
        public bool RequiresPrefix { get; }

        public string IlName
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Assembly.Name))
                {
                    return Name;
                }

                return $"[{Assembly.Name}]{Name}";
            }
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Assembly.Name))
            {
                // The standard types int, string, object, etc, need no prefix
                return Name;
            }

            return $"[{Assembly.Name}]{Name}";
        }

        public bool TryGetMethod(EquatableMethod original, [NotNullWhen(true)]out EquatableMethod? equatableMethod)
        {
            EquatableMethod? method = Methods.Find(m => EquatableMethod.AreMethodsIdentical(m, original));
            if (method is not null)
            {
                equatableMethod = method;
                return true;
            }

            equatableMethod = null;
            return false;
        }
    }
}
