// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    internal class MethodWrapper : IEntity, IMethod
    {
        private readonly ClassMember _memberField;
        private readonly ExecutionSet _executionSet;
        private readonly ArduinoMethodDeclaration? _arduinoMethod;
        private string _name;
        private IType _declaringType;

        public MethodWrapper(ClassDeclaration owner, ClassMember memberField, ExecutionSet executionSet)
        {
            _memberField = memberField;
            _executionSet = executionSet;
            _declaringType = new ClassWrapper(owner, executionSet);
            _name = $"Method_{memberField.Token:X8}";
            _arduinoMethod = executionSet.GetMethod(memberField.Method, false);
        }

        public SymbolKind SymbolKind
        {
            get
            {
                if (IsConstructor)
                {
                    return SymbolKind.Constructor;
                }

                if (_memberField.Method is MethodInfo mf)
                {
                    if (mf.IsSpecialName)
                    {
                        if (mf.Name.StartsWith("get_") || mf.Name.StartsWith("set_"))
                        {
                            return SymbolKind.Accessor;
                        }

                        // TODO: Operators, Destructors, etc.
                    }

                    return SymbolKind.Method;
                }

                throw new NotSupportedException("Invalid type detected");
            }
        }

        string IEntity.Name => _name;

        public ITypeDefinition? DeclaringTypeDefinition { get; }

        IType IMember.DeclaringType => _declaringType;

        public IEnumerable<IMember> ExplicitlyImplementedInterfaceMembers { get; }
        public bool IsExplicitInterfaceImplementation { get; }

        public bool IsVirtual
        {
            get => _memberField.Method.IsVirtual;
        }

        public bool IsOverride => false;
        public bool IsOverridable => IsVirtual;
        public TypeParameterSubstitution Substitution { get; }
        public IEnumerable<IAttribute> GetReturnTypeAttributes()
        {
            throw new NotImplementedException();
        }

        public IMethod Specialize(TypeParameterSubstitution substitution)
        {
            throw new NotImplementedException();
        }

        public bool ReturnTypeIsRefReadOnly { get; }
        public bool IsInitOnly { get; }
        public bool ThisIsRefReadOnly { get; }
        public IReadOnlyList<ITypeParameter> TypeParameters { get; }
        public IReadOnlyList<IType> TypeArguments { get; }
        public bool IsExtensionMethod { get; }
        public bool IsLocalFunction { get; }
        public bool IsConstructor => _memberField.Method.IsConstructor;
        public bool IsDestructor { get; }
        public bool IsOperator { get; }
        public bool HasBody => _arduinoMethod != null && _arduinoMethod.HasBody;
        public bool IsAccessor { get; }
        public IMember? AccessorOwner { get; }
        public MethodSemanticsAttributes AccessorKind { get; }
        public IMethod? ReducedFrom { get; }

        IMember IMember.Specialize(TypeParameterSubstitution substitution)
        {
            return Specialize(substitution);
        }

        public bool Equals(IMember? obj, TypeVisitor typeNormalization)
        {
            throw new NotImplementedException();
        }

        public IMember MemberDefinition { get; }
        public IType ReturnType { get; }

        IType? IEntity.DeclaringType => _declaringType;

        public IModule? ParentModule { get; }
        public Accessibility Accessibility { get; }
        public bool IsStatic { get; }
        public bool IsAbstract { get; }
        public bool IsSealed { get; }
        public string FullName { get; }
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

        public string ReflectionName { get; }
        public string Namespace => string.Empty;

        string ISymbol.Name => _name;

        public ICompilation Compilation { get; }

        public IReadOnlyList<IParameter> Parameters
        {
            get
            {
                List<IParameter> ret = new List<IParameter>();
                if (_arduinoMethod != null) // this can be null on non-existing methods (but why do we try to syntesize those?)
                {
                    var input = _executionSet.GetArgumentTypes(_arduinoMethod.MethodBase);
                    for (int i = 0; i < input.Count; i++)
                    {
                        ret.Add(new ParameterWrapper(input[i], i, _executionSet));
                    }
                }

                return ret;
            }
        }
    }
}
