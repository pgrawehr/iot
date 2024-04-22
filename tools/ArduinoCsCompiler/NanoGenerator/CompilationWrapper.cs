// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.Decompiler.Util;

namespace ArduinoCsCompiler.NanoGenerator
{
    internal class CompilationWrapper : ICompilation
    {
        public INamespace? GetNamespaceForExternAlias(string? alias)
        {
            throw new NotImplementedException();
        }

        public IType FindType(KnownTypeCode typeCode)
        {
            throw new NotImplementedException();
        }

        public IModule MainModule { get; }
        public IReadOnlyList<IModule> Modules { get; }
        public IReadOnlyList<IModule> ReferencedModules { get; }
        public INamespace RootNamespace { get; }
        public StringComparer NameComparer { get; }
        public CacheManager CacheManager { get; }
    }
}
