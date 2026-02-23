// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

#pragma warning disable CS1591
namespace ArduinoCsCompiler
{
    public class ClassMember
    {
        public ClassMember(string name, VariableKind variableType, int token, int sizeOfField)
        {
            VariableType = variableType;
            Token = token;
            BaseTokens = null;
            SizeOfField = sizeOfField;
            Name = name;
            FieldName = name;
        }

        public ClassMember(FieldInfo field, VariableKind variableType, int token, int sizeOfField, int staticFieldSize)
        {
            VariableType = variableType;
            Token = token;
            BaseTokens = null;
            SizeOfField = sizeOfField;
            Field = field;
            StaticFieldSize = staticFieldSize;
            FieldName = SanitizeFieldName(Field.Name);
            if (!FieldName.Contains('\'', StringComparison.Ordinal))
            {
                FieldName = FieldName + $"0x{token:X8}";
            }

            Name = $"Field: {field.MemberInfoSignature()}";
        }

        private static string SanitizeFieldName(string fieldName)
        {
            if (ClassDeclaration.RemoveAnyOf(fieldName, new char[]
                {
                    '<', '>'
                }, out string changed))
            {
                fieldName = changed;
            }

            if (Char.IsDigit(fieldName[0]))
            {
                // The field name begins with a digit.
                // (This happens on some auto-generated fields that have the actual name as '<>9' or similar)
                fieldName = $"'{fieldName}'";
            }

            fieldName = ExternalSystemReferences.ReplaceInvalidFieldOrArgumentNames(fieldName);
            return fieldName;
        }

        public ClassMember(MethodBase method, VariableKind variableType, int token, List<int> baseTokens)
        {
            VariableType = variableType;
            Token = token;
            BaseTokens = baseTokens;
            SizeOfField = 0;
            Method = method;
            FieldName = string.Empty;
            Name = $"Method: {method.MethodSignature()}";
        }

        public ClassMember(ClassMember other)
        {
            VariableType = other.VariableType;
            Token = other.Token;
            BaseTokens = other.BaseTokens;
            SizeOfField = other.SizeOfField;
            StaticFieldSize = other.StaticFieldSize;
            Method = other.Method;
            Field = other.Field;
            Name = other.Name;
            FieldName = other.FieldName;
            Offset = other.Offset;
        }

        public string Name
        {
            get;
        }

        public string FieldName
        {
            get;
        }

        public MethodBase? Method
        {
            get;
        }

        public FieldInfo? Field
        {
            get;
        }

        /// <summary>
        /// This value is non-zero for static fields. The length is the size of the root field required, so it is
        /// the type length for value types but 4 (sizeof(void*)) for reference types.
        /// </summary>
        public int StaticFieldSize
        {
            get;
        }

        public VariableKind VariableType
        {
            get;
        }

        public int Token
        {
            get;
        }

        public List<int>? BaseTokens
        {
            get;
        }

        public int SizeOfField
        {
            get;
            set;
        }

        /// <summary>
        /// Field offset. Only for non-static fields.
        /// </summary>
        public int Offset
        {
            get;
            set;
        }

        public override string ToString()
        {
            return Name;
        }

        public ClassMember DeepClone()
        {
            return new ClassMember(this);
        }
    }
}
