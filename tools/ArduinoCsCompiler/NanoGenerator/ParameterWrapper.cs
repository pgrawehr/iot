// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.TypeSystem;

namespace ArduinoCsCompiler.NanoGenerator
{
    internal class ParameterWrapper : IParameter
    {
        private readonly ClassDeclaration _parameterType;
        private readonly int _argNo;
        private readonly ExecutionSet _executionSet;
        private string _name;

        public ParameterWrapper(ClassDeclaration parameterType, int argNo, ExecutionSet executionSet)
        {
            _parameterType = parameterType;
            _argNo = argNo;
            _executionSet = executionSet;
            _name = $"Arg_{argNo}";
        }

        public SymbolKind SymbolKind => SymbolKind.Parameter;

        public object? GetConstantValue(bool throwOnInvalidMetadata = false)
        {
            throw new NotImplementedException();
        }

        string IVariable.Name => _name;

        public IType Type => new ClassWrapper(_parameterType, _executionSet);

        public bool IsConst => false;

        string ISymbol.Name => _name;

        public IEnumerable<IAttribute> GetAttributes()
        {
            throw new NotImplementedException();
        }

        public ReferenceKind ReferenceKind { get; }
        public LifetimeAnnotation Lifetime { get; }
        public bool IsRef { get; }
        public bool IsOut { get; }
        public bool IsIn { get; }
        public bool IsParams { get; }
        public bool IsOptional { get; }
        public bool HasConstantValueInSignature { get; }
        public IParameterizedMember? Owner { get; }
    }
}
