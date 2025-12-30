// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#pragma warning disable CS1591
namespace ArduinoCsCompiler
{
    public class ClassDeclaration : IEquatable<ClassDeclaration>
    {
        public static readonly Dictionary<Type, string> ShortTypeNames;
        private readonly List<ClassMember> _members;
        private readonly List<Type> _interfaces;
        private string? _fullNameSet;

        /// <summary>
        /// For some types, we need to use the short names in IL, or the compiler will complain about a syntax error.
        /// </summary>
        static ClassDeclaration()
        {
            ShortTypeNames = new Dictionary<Type, string>()
            {
                {
                    typeof(System.String), "string"
                },
                {
                    typeof(System.Int32), "int32"
                },
                {
                    typeof(System.UInt32), "uint32"
                },
                {
                    typeof(System.Byte), "uint8"
                },
                {
                    typeof(System.UInt16), "uint16"
                },
                {
                    typeof(System.Int16), "int16"
                },
                {
                    typeof(System.SByte), "int8"
                }
            };
        }

        public ClassDeclaration(Type type, int dynamicSize, int staticSize, int newToken,
            List<ClassMember> members, List<Type> interfaces)
        {
            TheType = type;
            DynamicSize = dynamicSize;
            StaticSize = staticSize;
            _members = members;
            NewToken = newToken;
            _interfaces = interfaces;
            Name = type.ClassSignature(true);
            ReadOnly = false;
            UseOriginalType = false;
        }

        public ClassDeclaration(ClassDeclaration other)
        {
            TheType = other.TheType;
            DynamicSize = other.DynamicSize;
            StaticSize = other.StaticSize;
            NewToken = other.NewToken;
            Name = TheType.ClassSignature(true);
            ReadOnly = other.ReadOnly;
            UseOriginalType = other.UseOriginalType;
            _interfaces = other._interfaces;
            _members = new List<ClassMember>(other.Members.Count);
            foreach (var m in other.Members)
            {
                _members.Add(m.DeepClone());
            }
        }

        public Type TheType
        {
            get;
        }

        /// <summary>
        /// Allows overriding the original name of this class
        /// </summary>
        public string? FullName
        {
            get
            {
                return _fullNameSet ?? TheType.FullName;
            }
            private set
            {
                _fullNameSet = value;
            }
        }

        /// <summary>
        /// This is set to true if the class is made read-only (i.e copied to flash). No further members can be added in this case
        /// </summary>
        public bool ReadOnly
        {
            get;
            internal set;
        }

        public int NewToken
        {
            get;
        }

        public string Name
        {
            get;
        }

        public bool UseOriginalType
        {
            get;
            set;
        }

        public int DynamicSize { get; }
        public int StaticSize { get; }

        public IList<ClassMember> Members => _members.AsReadOnly();

        public IEnumerable<Type> RawInterfaces => _interfaces;

        public List<ClassDeclaration>? WrappedInterfaces
        {
            get;
            set;
        }

        public bool SuppressInit
        {
            get
            {
                // Type initializers of open generic types are pointless to execute
                if (TheType.ContainsGenericParameters)
                {
                    return true;
                }

                // Don't run these init functions, to complicated or depend on native functions
                return TheType.FullName == "System.SR";
            }
        }

        public bool Equals(ClassDeclaration? other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            // Here, Type and MiniType must be distinct.
            return NewToken == other.NewToken && Name == other.Name;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((ClassDeclaration)obj);
        }

        public override int GetHashCode()
        {
            return NewToken;
        }

        public static bool operator ==(ClassDeclaration? left, ClassDeclaration? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ClassDeclaration? left, ClassDeclaration? right)
        {
            return !Equals(left, right);
        }

        public void AddClassMember(ClassMember member)
        {
            if (ReadOnly)
            {
                throw new NotSupportedException($"Cannot update class {Name}, as it is read-only");
            }

            _members.Add(member);
        }

        public void RemoveMemberAt(int index)
        {
            _members.RemoveAt(index);
        }

        public override string ToString()
        {
            return Name;
        }

        public ClassDeclaration DeepClone()
        {
            return new ClassDeclaration(this);
        }

        public void ConstructName(ExecutionSet set)
        {
            if (FullName == null)
            {
                return;
            }

            if (ShortTypeNames.TryGetValue(TheType, out string? name1))
            {
                FullName = name1;
                return;
            }

            if (TheType.IsGenericType && TheType.IsConstructedGenericType)
            {
                // We will later remove these alltogether, as the runtime now really supports generics.
                // However, for now we just make sure they have a valid name (can be anything, actually, just not their full name)

                // String looks like
                // System.Nullable`1[[System.Int32, System.Private.CoreLib, Version=8.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
                // and in a nested form, so replace from inside to outside
                int idx = FullName.LastIndexOf("[[", StringComparison.Ordinal);
                while (idx != -1)
                {
                    int idxEnd = FullName.IndexOf("]]", idx + 1, StringComparison.Ordinal);
                    int idxEndName = FullName.IndexOf(",", idx + 1, StringComparison.Ordinal);
                    string name = FullName.Substring(idx + 2, idxEndName - idx - 2);
                    name = name.Replace("[]", "Arr"); // When the type parameter is an array, note that, but not using []
                    FullName = FullName.Remove(idx, idxEnd - idx + 2);
                    FullName = FullName.Insert(idx, $"_arg_{name}_");
                    idx = FullName.LastIndexOf("[[", StringComparison.Ordinal);
                }
            }

            if (FullName.Contains('+'))
            {
                FullName = FullName.Replace("+", "_sub_");
            }

            if (RemoveAnyOf(FullName, new[] { '<', '>', '/' }, out string changed))
            {
                FullName = changed;
            }
        }

        private bool RemoveAnyOf(string input, char[] forbiddenChars, out string changed)
        {
            changed = input;
            var idx = input.IndexOfAny(forbiddenChars);
            if (idx == -1)
            {
                return false;
            }

            while (idx != -1)
            {
                changed = changed.Remove(idx, 1);
                idx = changed.IndexOfAny(forbiddenChars);
            }

            return true;
        }

        public static ClassDeclaration? GetClassDeclaration(ExecutionSet set, Type? ofClass)
        {
            if (ofClass == null)
            {
                return null;
            }

            int tk = set.GetOrAddClassToken(ofClass.GetTypeInfo());
            ClassDeclaration? ret = set.Classes.FirstOrDefault(y => y.NewToken == tk);
            if (ret != null)
            {
                ret.ConstructName(set);
            }

            return ret;
        }
    }
}
