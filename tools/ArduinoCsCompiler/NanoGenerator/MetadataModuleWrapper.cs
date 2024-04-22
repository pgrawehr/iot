// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ArduinoCsCompiler.NanoGenerator
{
    internal class MetadataModuleWrapper : IMetadataModule
    {
        public MetadataModuleWrapper()
        {
        }

        public SymbolKind SymbolKind => SymbolKind.Module;
        public string Name => "InMemoryModule";
        public ICompilation Compilation { get; }
        public IEnumerable<IAttribute> GetAssemblyAttributes()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IAttribute> GetModuleAttributes()
        {
            throw new NotImplementedException();
        }

        public bool InternalsVisibleTo(IModule module)
        {
            throw new NotImplementedException();
        }

        public ITypeDefinition? GetTypeDefinition(TopLevelTypeName topLevelTypeName)
        {
            throw new NotImplementedException();
        }

        public MetadataFile? MetadataFile { get; }
        public bool IsMainModule { get; }
        public string AssemblyName { get; }
        public Version AssemblyVersion { get; }
        public string FullAssemblyName { get; }
        public INamespace RootNamespace { get; }
        public IEnumerable<ITypeDefinition> TopLevelTypeDefinitions { get; }
        public IEnumerable<ITypeDefinition> TypeDefinitions { get; }
        public IMethod ResolveMethod(EntityHandle methodReference, GenericContext genericContext)
        {
            throw new NotImplementedException();
        }

        public ITypeDefinition GetDefinition(TypeDefinitionHandle handle)
        {
            throw new NotImplementedException();
        }

        public IField GetDefinition(FieldDefinitionHandle handle)
        {
            throw new NotImplementedException();
        }

        public IMethod GetDefinition(MethodDefinitionHandle handle)
        {
            throw new NotImplementedException();
        }

        public IProperty GetDefinition(PropertyDefinitionHandle handle)
        {
            throw new NotImplementedException();
        }

        public IEvent GetDefinition(EventDefinitionHandle handle)
        {
            throw new NotImplementedException();
        }

        public IModule ResolveModule(AssemblyReferenceHandle handle)
        {
            throw new NotImplementedException();
        }

        public IModule ResolveModule(ModuleReferenceHandle handle)
        {
            throw new NotImplementedException();
        }

        public IModule GetDeclaringModule(TypeReferenceHandle handle)
        {
            throw new NotImplementedException();
        }

        public IType ResolveType(EntityHandle typeRefDefSpec, GenericContext context, CustomAttributeHandleCollection? typeAttributes = null, Nullability nullableContext = Nullability.Oblivious)
        {
            throw new NotImplementedException();
        }

        public IType ResolveType(EntityHandle typeRefDefSpec, GenericContext context, TypeSystemOptions customOptions, CustomAttributeHandleCollection? typeAttributes = null, Nullability nullableContext = Nullability.Oblivious)
        {
            throw new NotImplementedException();
        }

        public IEntity ResolveEntity(EntityHandle entityHandle, GenericContext context = new GenericContext())
        {
            throw new NotImplementedException();
        }

        public ImmutableArray<IType> DecodeLocalSignature(StandaloneSignatureHandle handle, GenericContext genericContext)
        {
            throw new NotImplementedException();
        }

        public (SignatureHeader, FunctionPointerType) DecodeMethodSignature(StandaloneSignatureHandle handle, GenericContext genericContext)
        {
            throw new NotImplementedException();
        }

        public TypeSystemOptions TypeSystemOptions { get; }
        public MetadataReader MetadataReader { get; }
    }
}
