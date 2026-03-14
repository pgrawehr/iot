// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ArduinoCsCompiler
{
    internal class ExternalTypeReference
    {
        public ExternalTypeReference(string name, Type type, ExternalAssemblyReference assembly, bool requiresPrefix)
        {
            Name = name;
            Type = type;
            Assembly = assembly;
            RequiresPrefix = requiresPrefix;
        }

        public ExternalTypeReference(Type type, ExternalAssemblyReference assembly)
            : this(type.FullName!, type, assembly, true)
        {
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
            return $"[{Assembly.Name}]{Name}";
        }
    }
}
