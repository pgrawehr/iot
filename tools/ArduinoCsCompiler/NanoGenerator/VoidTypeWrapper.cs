// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ArduinoCsCompiler.NanoGenerator
{
    internal class VoidTypeWrapper : IEntity, ITypeDefinition
    {
        private readonly string _name = "void";
        private readonly bool _isPointer;

        public VoidTypeWrapper(SymbolKind symbolKind, bool isPointer)
        {
            SymbolKind = symbolKind;
            _isPointer = isPointer;
        }

        public SymbolKind SymbolKind { get; }

        string IEntity.Name => _name;

        public ITypeDefinition? DeclaringTypeDefinition => null;

        IType? ITypeDefinition.DeclaringType => null;

        public bool HasExtensionMethods => false;
        public Nullability NullableContext => Nullability.NotNullable;
        public bool IsRecord => false;

        public IType ChangeNullability(Nullability newNullability)
        {
            throw new NotImplementedException();
        }

        public ITypeDefinition? GetDefinition()
        {
            return this;
        }

        public ITypeDefinitionOrUnknown? GetDefinitionOrUnknown()
        {
            throw new NotImplementedException();
        }

        public IType AcceptVisitor(TypeVisitor visitor)
        {
            throw new NotImplementedException();
        }

        public IType VisitChildren(TypeVisitor visitor)
        {
            throw new NotImplementedException();
        }

        public TypeParameterSubstitution GetSubstitution()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IType> GetNestedTypes(Predicate<ITypeDefinition>? filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IType> GetNestedTypes(IReadOnlyList<IType> typeArguments, Predicate<ITypeDefinition>? filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IMethod> GetConstructors(Predicate<IMethod>? filter = null, GetMemberOptions options = GetMemberOptions.IgnoreInheritedMembers)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IMethod> GetMethods(Predicate<IMethod>? filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IMethod> GetMethods(IReadOnlyList<IType> typeArguments, Predicate<IMethod>? filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IProperty> GetProperties(Predicate<IProperty>? filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IField> GetFields(Predicate<IField>? filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IEvent> GetEvents(Predicate<IEvent>? filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IMember> GetMembers(Predicate<IMember>? filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IMethod> GetAccessors(Predicate<IMethod>? filter = null, GetMemberOptions options = GetMemberOptions.None)
        {
            throw new NotImplementedException();
        }

        public TypeKind Kind
        {
            get
            {
                if (_isPointer)
                {
                    return TypeKind.Pointer;
                }

                return TypeKind.Void;
            }
        }

        public bool? IsReferenceType => false;
        public bool IsByRefLike => false;
        public Nullability Nullability => Nullability.NotNullable;
        public IReadOnlyList<ITypeDefinition> NestedTypes { get; }
        public IReadOnlyList<IMember> Members { get; }
        public IEnumerable<IField> Fields { get; }
        public IEnumerable<IMethod> Methods { get; }
        public IEnumerable<IProperty> Properties { get; }
        public IEnumerable<IEvent> Events { get; }
        public KnownTypeCode KnownTypeCode => KnownTypeCode.Void;
        public IType? EnumUnderlyingType { get; }
        public bool IsReadOnly { get; }
        public string MetadataName { get; }

        IType? IType.DeclaringType => null;

        public int TypeParameterCount { get; }
        public IReadOnlyList<ITypeParameter> TypeParameters { get; }
        public IReadOnlyList<IType> TypeArguments { get; }
        public IEnumerable<IType> DirectBaseTypes { get; }

        IType? IEntity.DeclaringType => null;

        public IModule? ParentModule { get; }
        public Accessibility Accessibility { get; }
        public bool IsStatic { get; }
        public bool IsAbstract { get; }
        public bool IsSealed { get; }
        public string FullName => _name;
        public IEnumerable<IAttribute> GetAttributes()
        {
            throw new NotImplementedException();
        }

        public bool HasAttribute(KnownAttribute attribute)
        {
            throw new NotImplementedException();
        }

        public IAttribute? GetAttribute(KnownAttribute attribute)
        {
            throw new NotImplementedException();
        }

        public EntityHandle MetadataToken { get; }

        string INamedElement.Name => _name;

        public string ReflectionName => _name;
        public string Namespace => "System";

        string ISymbol.Name => _name;

        public ICompilation Compilation { get; }
        public bool Equals(IType? other)
        {
            throw new NotImplementedException();
        }

        public FullTypeName FullTypeName
        {
            get
            {
                if (_isPointer)
                {
                    return new FullTypeName("System.Void*");
                }

                return new FullTypeName("System.Void");
            }
        }
    }
}
