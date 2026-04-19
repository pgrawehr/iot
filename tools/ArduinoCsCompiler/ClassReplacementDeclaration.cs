// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ArduinoCsCompiler
{
    public record ClassReplacementDeclaration
    {
        public ClassReplacementDeclaration(Type original, ClassDeclaration replacement, bool subclasses)
        {
            Original = original;
            Replacement = replacement;
            Subclasses = subclasses;
            AssemblyQualifiedName = original.AssemblyQualifiedName ?? string.Empty;
        }

        public Type Original { get; }
        public ClassDeclaration Replacement { get; }
        public bool Subclasses { get; }

        public string AssemblyQualifiedName
        {
            get;
        }
    }
}
