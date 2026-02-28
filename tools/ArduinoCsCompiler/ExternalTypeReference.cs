// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ArduinoCsCompiler
{
    internal class ExternalTypeReference
    {
        public ExternalTypeReference(string name, Type type, ExternalAssemblyReference assembly)
        {
            Name = name;
            Type = type;
            Assembly = assembly;
        }

        public ExternalTypeReference(Type type, ExternalAssemblyReference assembly)
            : this(type.FullName!, type, assembly)
        {
        }

        public string Name { get; }

        public Type Type { get; }

        public ExternalAssemblyReference Assembly { get; }

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
    }
}
