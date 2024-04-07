// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;

namespace ArduinoCsCompiler.NanoGenerator
{
    internal sealed class ClassWrapper : IEntity, ITypeDefinition
    {
        private readonly ClassDeclaration _cls;
        private readonly ExecutionSet _executionSet;

        public ClassWrapper(ClassDeclaration cls, ExecutionSet executionSet)
        {
            _cls = cls;
            _executionSet = executionSet;

            var members = new List<IMember>();

            foreach (var member in cls.Members)
            {
                if (member.Field != null)
                {
                    var fieldWrapped = new FieldWrapper(cls, member, _executionSet);
                    members.Add(fieldWrapped);
                }
            }

            Members = members;
        }

        public SymbolKind SymbolKind => SymbolKind.TypeDefinition;

        public string FullName => _cls.Name;

        public IEnumerable<IAttribute> GetAttributes()
        {
            return new List<IAttribute>();
        }

        public bool HasAttribute(KnownAttribute attribute)
        {
            return false;
        }

        public IAttribute? GetAttribute(KnownAttribute attribute)
        {
            return null;
        }

        public EntityHandle MetadataToken => throw new NotImplementedException();

        public string Name => _cls.SimpleName;

        public ITypeDefinition? DeclaringTypeDefinition => null;

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
                if (_cls.TheType.IsValueType)
                {
                    return TypeKind.Struct;
                }

                if (_cls.TheType.IsArray)
                {
                    return TypeKind.Array;
                }

                if (_cls.TheType.IsEnum)
                {
                    return TypeKind.Enum;
                }

                if (_cls.TheType.IsInterface)
                {
                    return TypeKind.Interface;
                }

                return TypeKind.Class;
            }
        }

        public bool? IsReferenceType => _cls.TheType.IsClass;
        public bool IsByRefLike => _cls.TheType.IsByRefLike;
        public Nullability Nullability => Nullability.Oblivious;
        public IReadOnlyList<ITypeDefinition> NestedTypes { get; }
        public IReadOnlyList<IMember> Members { get; }
        public IEnumerable<IField> Fields { get; }
        public IEnumerable<IMethod> Methods { get; }
        public IEnumerable<IProperty> Properties { get; }
        public IEnumerable<IEvent> Events { get; }
        public KnownTypeCode KnownTypeCode { get; }
        public IType? EnumUnderlyingType { get; }
        public bool IsReadOnly { get; }
        public string MetadataName { get; }
        public IType? DeclaringType => null;
        public bool HasExtensionMethods { get; }
        public Nullability NullableContext { get; }
        public bool IsRecord { get; }
        public int TypeParameterCount { get; }

        public IReadOnlyList<ITypeParameter> TypeParameters
        {
            get
            {
                return new List<ITypeParameter>();
            }
        }

        public IReadOnlyList<IType> TypeArguments { get; }

        public IEnumerable<IType> DirectBaseTypes
        {
            get
            {
                List<IType> baseTypes = new List<IType>();
                var myBase = _executionSet.Classes.FirstOrDefault(x => x.TheType == _cls.TheType.BaseType);
                if (myBase != null && myBase.TheType != typeof(object))
                {
                    baseTypes.Add(new ClassWrapper(myBase, _executionSet));
                }

                var interfaces = _cls.Interfaces;
                foreach (var iface in interfaces)
                {
                    var theInterface = _executionSet.Classes.FirstOrDefault(x => x.TheType == iface);
                    if (theInterface != null)
                    {
                        baseTypes.Add(new ClassWrapper(theInterface, _executionSet));
                    }
                }

                return baseTypes;
            }
        }

        public IModule? ParentModule => null;

        public Accessibility Accessibility => Accessibility.Public;

        public bool IsStatic => false;

        public bool IsAbstract => _cls.TheType.IsAbstract;

        public bool IsSealed => _cls.TheType.IsSealed;

        public string ReflectionName => _cls.TheType.Name;

        public string Namespace => _cls.TheType.Namespace ?? string.Empty;

        public ICompilation Compilation => throw new NotImplementedException();
        public bool Equals(IType? other)
        {
            throw new NotImplementedException();
        }

        public FullTypeName FullTypeName => new FullTypeName(_cls.Name);
    }
}
