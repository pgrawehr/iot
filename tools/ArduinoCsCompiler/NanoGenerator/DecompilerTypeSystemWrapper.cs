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
    internal class DecompilerTypeSystemWrapper : IDecompilerTypeSystem
    {
        public DecompilerTypeSystemWrapper()
        {
            MainModule = new MetadataModuleWrapper();
        }

        public INamespace? GetNamespaceForExternAlias(string? alias)
        {
            throw new NotImplementedException();
        }

        public IType FindType(KnownTypeCode typeCode)
        {
            throw new NotImplementedException();
        }

        public IMetadataModule MainModule { get; }

        IModule ICompilation.MainModule => MainModule;

        public IReadOnlyList<IModule> Modules => new List<IModule>();
        public IReadOnlyList<IModule> ReferencedModules => new List<IModule>();
        public INamespace RootNamespace { get; }
        public StringComparer NameComparer { get; }
        public CacheManager CacheManager { get; }
    }
}
