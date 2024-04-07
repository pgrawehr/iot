using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.TypeSystem;

namespace ArduinoCsCompiler.NanoGenerator
{
    internal class FieldWrapper : IField
    {
        private readonly ClassDeclaration _owner;
        private readonly ClassMember _memberField;
        private readonly ExecutionSet _executionSet;

        public FieldWrapper(ClassDeclaration owner, ClassMember memberField, ExecutionSet executionSet)
        {
            _owner = owner;
            _memberField = memberField;
            _executionSet = executionSet;
        }

        public SymbolKind SymbolKind => SymbolKind.Field;
        public string FullName => _memberField.Name;

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
            throw new NotImplementedException();
        }

        public EntityHandle MetadataToken => throw new NotImplementedException();

        public object? GetConstantValue(bool throwOnInvalidMetadata = false)
        {
            return _memberField.Field.GetValue(null); // Expect a constant here
        }

        public string Name => _memberField.Name;
        public bool IsReadOnly => _memberField.Field.IsInitOnly;
        public bool ReturnTypeIsRefReadOnly { get; }
        public bool IsVolatile { get; }

        public IType Type
        {
            get
            {
                return new ClassWrapper(_executionSet.GetClass(_memberField.Field.DeclaringType), _executionSet);
            }
        }

        public bool IsConst => _memberField.Field.IsLiteral;
        public ITypeDefinition? DeclaringTypeDefinition => new ClassWrapper(_owner, _executionSet);

        IType IMember.DeclaringType => Type;

        public IEnumerable<IMember> ExplicitlyImplementedInterfaceMembers { get; }
        public bool IsExplicitInterfaceImplementation { get; }
        public bool IsVirtual { get; }
        public bool IsOverride { get; }
        public bool IsOverridable { get; }
        public TypeParameterSubstitution Substitution { get; }

        public IMember Specialize(TypeParameterSubstitution substitution)
        {
            throw new NotImplementedException();
        }

        public bool Equals(IMember? obj, TypeVisitor typeNormalization)
        {
            throw new NotImplementedException();
        }

        public IMember MemberDefinition { get; }
        public IType ReturnType => Type;

        IType? IEntity.DeclaringType => Type;

        public IModule? ParentModule { get; }
        public Accessibility Accessibility { get; }
        public bool IsStatic { get; }
        public bool IsAbstract { get; }
        public bool IsSealed { get; }
        public string ReflectionName { get; }
        public string Namespace { get; }
        public ICompilation Compilation { get; }
    }
}
