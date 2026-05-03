// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.Intrinsics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using Mono.Cecil;

namespace ArduinoCsCompiler
{
    internal static class ExternalSystemReferences
    {
        private static List<ExternalTypeReference> _references = new List<ExternalTypeReference>();
        private static List<string> _keywords = new List<string>();

        /// <summary>
        /// This method is actually a static ctor, but it's a bit too complex to be implemented as one
        /// (if things fail, the exception ends up in a random place)
        /// </summary>
        /// <param name="logger">A logger</param>
        /// <param name="set">The execution set to update</param>
        /// <remarks>Should probably make this an extension method that does not keep state</remarks>
        public static void Init(ILogger logger, ExecutionSet set)
        {
            // Keywords can't be used as argument or class names (many of them are valid C# identifiers, though)
            _keywords = new List<string>()
            {
                "abstract",
                "add",
                "add.ovf",
                "add.ovf.un",
                "algorithm",
                "alignment",
                "and",
                "ansi",
                "any",
                "arglist",
                "array",
                "as",
                "assembly",
                "assert",
                "at",
                "auto",
                "autochar",
                "beforefieldinit",
                "beq",
                "beq.s",
                "bge",
                "bge.s",
                "bge.un",
                "bge.un.s",
                "bgt",
                "bgt.s",
                "bgt.un",
                "bgt.un.s",
                "ble",
                "ble.s",
                "ble.un",
                "ble.un.s",
                "blob",
                "blob_object",
                "blt",
                "blt.s",
                "blt.un",
                "blt.un.s",
                "bne.un",
                "bne.un.s",
                "bool",
                "box",
                "br",
                "br.s",
                "break",
                "brfalse",
                "brfalse.s",
                "brinst",
                "brinst.s",
                "brnull" +
                "brnull.s",
                "brtrue",
                "brtrue.s",
                "brzero",
                "brzero.s",
                "bstr",
                "bytearray",
                "byvalstr",
                "call",
                "calli",
                "callmostderived",
                "callvirt",
                "carray",
                "castclass",
                "catch",
                "cdecl",
                "ceq",
                "cf",
                "cgt",
                "cgt.un",
                "char",
                "cil",
                "ckfinite",
                "class",
                "clsid",
                "clt",
                "clt.un",
                "const",
                "constrained.",
                "conv.i",
                "conv.i1",
                "conv.i2",
                "conv.i4",
                "conv.i8",
                "conv.ovf.i",
                "conv.ovf.i.un",
                "conv.ovf.i1",
                "conv.ovf.i1.un," +
                "conv.ovf.i2",
                "conv.ovf.i2.un",
                "conv.ovf.i4",
                "conv.ovf.i4.un",
                "conv.ovf.i8",
                "conv.ovf.i8.un",
                "conv.ovf.u",
                "conv.ovf.u.un",
                "conv.ovf.u1",
                "conv.ovf.u1.un",
                "conv.ovf.u2",
                "conv.ovf.u2.un",
                "conv.ovf.u4",
                "conv.ovf.u4.un",
                "conv.ovf.u8",
                "conv.ovf.u8.un",
                "conv.r.un",
                "conv.r4",
                "conv.r8",
                "conv.u",
                "conv.u1",
                "conv.u2",
                "conv.u4",
                "conv.u8",
                "cpblk",
                "cpobj",
                "currency",
                "custom",
                "date",
                "decimal",
                "default",
                "default",
                "demand",
                "deny",
                "div",
                "div.un",
                "dup",
                "endfault," +
                "endfilter",
                "endfinally",
                "endmac",
                "enum",
                "error",
                "explicit",
                "extends",
                "extern",
                "false",
                "famandassem",
                "family",
                "famorassem",
                "fastcall",
                "fastcall",
                "fault",
                "field",
                "filetime",
                "filter",
                "final",
                "finally",
                "fixed",
                "flags",
                "float",
                "float32",
                "float64",
                "forwardref",
                "fromunmanaged",
                "handler",
                "hidebysig",
                "hresult",
                "idispatch",
                "il",
                "illegal",
                "implements",
                "implicitcom",
                "implicitres",
                "import",
                "in",
                "inheritcheck," +
                "init",
                "initblk",
                "initobj",
                "initonly",
                "instance",
                "int",
                "int16",
                "int32",
                "int64",
                "int8",
                "interface",
                "internalcall",
                "isinst",
                "iunknown",
                "jmp",
                "lasterr",
                "lcid",
                "ldarg",
                "ldarg.0",
                "ldarg.1",
                "ldarg.2",
                "ldarg.3",
                "ldarg.s",
                "ldarga",
                "ldarga.s",
                "ldc.i4",
                "ldc.i4.0",
                "ldc.i4.1",
                "ldc.i4.2",
                "ldc.i4.3",
                "ldc.i4.4",
                "ldc.i4.5",
                "ldc.i4.6",
                "ldc.i4.7",
                "ldc.i4.8",
                "ldc.i4.M1",
                "ldc.i4.m1",
                "ldc.i4.s",
                "ldc.i8",
                "ldc.r4",
                "ldc.r8",
                "ldelem",
                "ldelem.i",
                "ldelem.i1",
                "ldelem.i2",
                "ldelem.i4",
                "ldelem.i8",
                "ldelem.r4",
                "ldelem.r8",
                "ldelem.ref",
                "ldelem.u1",
                "ldelem.u2",
                "ldelem.u4",
                "ldelem.u8",
                "ldelema",
                "ldfld",
                "ldflda",
                "ldftn",
                "ldind.i",
                "ldind.i1",
                "ldind.i2",
                "ldind.i4",
                "ldind.i8",
                "ldind.r4",
                "ldind.r8",
                "ldind.ref",
                "ldind.u1",
                "ldind.u2",
                "ldind.u4",
                "ldind.u8",
                "ldlen",
                "ldloc",
                "ldloc.0",
                "ldloc.1",
                "ldloc.2",
                "ldloc.3" +
                "ldloc.s",
                "ldloca",
                "ldloca.s",
                "ldnull",
                "ldobj",
                "ldsfld",
                "ldsflda",
                "ldstr",
                "ldtoken",
                "ldvirtftn",
                "leave",
                "leave.s",
                "library",
                "linkcheck",
                "literal",
                "localloc",
                "lpstr",
                "lpstruct",
                "lptstr",
                "lpvoid",
                "lpwstr",
                "managed",
                "marshal",
                "method",
                "mkrefany",
                "modopt",
                "modreq",
                "mul",
                "mul.ovf",
                "mul.ovf.un",
                "native",
                "neg",
                "nested",
                "newarr",
                "newobj",
                "newslot",
                "noappdomain",
                "no.",
                "noinlining",
                "nomachine",
                "nomangle",
                "nometadata",
                "noncasdemand",
                "noncasinheritance",
                "noncaslinkdemand",
                "nop",
                "noprocess",
                "not",
                "not_in_gc_heap",
                "notremotable",
                "notserialized",
                "null",
                "nullref",
                "object",
                "objectref",
                "opt",
                "optil",
                "or",
                "out",
                "permitonly",
                "pinned",
                "pinvokeimpl",
                "pop",
                "prefix1",
                "prefix2",
                "prefix3",
                "prefix4",
                "prefix5",
                "prefix6",
                "prefix7",
                "prefixref",
                "prejitdeny",
                "prejitgrant",
                "preservesig",
                "private",
                "privatescope",
                "protected",
                "public",
                "readonly.",
                "record",
                "refany",
                "refanytype",
                "refanyval",
                "rem",
                "rem.un",
                "reqmin",
                "reqopt",
                "reqrefuse",
                "reqsecobj",
                "request",
                "ret",
                "rethrow",
                "retval",
                "rtspecialname",
                "runtime",
                "safearray",
                "sealed",
                "sequential",
                "serializable",
                "shl",
                "shr",
                "shr.un",
                "sizeof",
                "special",
                "specialname",
                "starg",
                "starg.s",
                "static",
                "stdcall",
                "stdcall",
                "stelem",
                "stelem.i",
                "stelem.i1",
                "stelem.i2",
                "stelem.i4" +
                "stelem.i8",
                "stelem.r4",
                "stelem.r8",
                "stelem.ref",
                "stfld",
                "stind.i",
                "stind.i1",
                "stind.i2",
                "stind.i4",
                "stind.i8",
                "stind.r4",
                "stind.r8",
                "stind.ref",
                "stloc",
                "stloc.0",
                "stloc.1",
                "stloc.2",
                "stloc.3",
                "stloc.s",
                "stobj",
                "storage",
                "stored_object",
                "stream",
                "streamed_object",
                "string",
                "struct",
                "stsfld",
                "sub",
                "sub.ovf",
                "sub.ovf.un",
                "switch",
                "synchronized",
                "syschar",
                "sysstring",
                "tail.",
                "tbstr",
                "thiscall",
                "thiscall," +
                "throw",
                "tls",
                "to",
                "true",
                "type",
                "typedref",
                "unaligned.",
                "unbox",
                "unbox.any",
                "unicode",
                "unmanaged",
                "unmanagedexp",
                "unsigned",
                "unused",
                "userdefined",
                "value",
                "valuetype",
                "vararg",
                "variant",
                "vector",
                "virtual",
                "void",
                "volatile.",
                "wchar",
                "winapi",
                "with",
                "wrapper",
                "xor"
            };

            _references = new List<ExternalTypeReference>();
            var builtin = new ExternalAssemblyReference(string.Empty, string.Empty, string.Empty); // for built-in types, such as object or int

            _references.AddRange(new ExternalTypeReference[]
            {
                new("int64", typeof(System.Int64), builtin, false),
                new("uint64", typeof(System.UInt64), builtin, false),
                new("int32", typeof(System.Int32), builtin, false),
                new("uint32", typeof(System.UInt32), builtin, false),
                new("int16", typeof(System.Int16), builtin, false),
                new("uint16", typeof(System.UInt16), builtin, false),
                new("int8", typeof(System.SByte), builtin, false),
                new("uint8", typeof(System.Byte), builtin, false),
                new("object", typeof(System.Object), builtin, false),
                new("string", typeof(System.String), builtin, false),
                new("bool", typeof(System.Boolean), builtin, false),
                new("float64", typeof(System.Double), builtin, false),
                new("float32", typeof(System.Single), builtin, false)
            });

            _references.AddRange(new ExternalTypeReference[]
            {
                // TODO: Needs a proper replacement
                new(typeof(System.Runtime.InteropServices.Marshal), builtin),
            });

            var streams = typeof(ExternalSystemReferences).Assembly.GetManifestResourceNames();
            foreach (var name in streams.Where(x => x.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)))
            {
                ParseAndAddFromAssembly(logger, set, name);
            }

            foreach (var r in _references)
            {
                logger.LogInformation($"Using nanoFramework type {r.Type} instead of the .NET version");
                set.AddReplacementType(r.Type, new ClassDeclaration(r.Type, r), false, true);
            }
        }

        private static void ParseAndAddFromAssembly(ILogger logger, ExecutionSet set, string name)
        {
            // Get all the types the nanoframework base library offers
            using var data = AssemblyDefinition.ReadAssembly(Assembly.GetExecutingAssembly().GetManifestResourceStream(name));
            ExternalAssemblyReference reference = new ExternalAssemblyReference(data.Name.Name, BitConverter.ToString(data.Name.PublicKeyToken),
                data.Name.Version.ToString());
            // This refers to the full framework
            var systemAssembly = typeof(string).Assembly;
            var mod = data.Modules[0];
            if (reference.Name.EndsWith("mscorlib", StringComparison.OrdinalIgnoreCase))
            {
                // this one is in mscorlib, but in a separate library on the standard BCL
                _references.Add(new(typeof(System.Console), reference));
            }

            foreach (TypeDefinition cls in mod.GetTypes())
            {
                if (!cls.IsClass && !cls.IsInterface && !cls.IsValueType)
                {
                    continue;
                }

                Type? effectiveType = systemAssembly.GetType(cls.FullName, false, false);

                if (effectiveType == null)
                {
                    logger.LogWarning($"Type {cls.FullName} does not exist in the system library of the standard BCL");
                    continue;
                }

                // Todo: We're missing static members later (error is probably not here, though)
                if (effectiveType.IsGenericType)
                {
                    var typeParams = effectiveType.GetGenericArguments();
                    if (typeParams.Any(x => !x.IsGenericParameter))
                    {
                        logger.LogError($"The assembly {systemAssembly} contains closed generic types in the definition??");
                        continue;
                    }
                }

                List<EquatableMethod> methodsInExternalClass = new List<EquatableMethod>();
                List<MethodBase> methodsInStandardBcl = effectiveType.GetMethods(BindingFlags.Instance | BindingFlags.Static |
                                                                                 BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public)
                    .Cast<MethodBase>().ToList();
                methodsInStandardBcl.AddRange(effectiveType.GetConstructors(BindingFlags.Instance |
                                                                            BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic));
                foreach (var method in cls.Methods)
                {
                    foreach (var method2 in methodsInStandardBcl)
                    {
                        var m2 = new EquatableMethod(method2, true);
                        if (m2.Name == method.Name && HaveSameParameterTypes(m2, method) &&
                            m2.IsStatic == method.IsStatic)
                        {
                            methodsInExternalClass.Add(m2);
                            break;
                        }
                    }
                }

                ExternalTypeReference? existing = _references.FirstOrDefault(x => x.Type == effectiveType);
                if (existing != null)
                {
                    // Also for System.Object etc we need to add its members, otherwise we won't be able to find them when we need to replace them
                    existing.Methods.AddRange(methodsInExternalClass);
                    continue;
                }

                _references.Add(new ExternalTypeReference(effectiveType, methodsInExternalClass, reference));
            }
        }

        private static bool HaveSameParameterTypes(EquatableMethod m1, MethodDefinition m2)
        {
            if (m1.GetParameters().Length != m2.Parameters.Count)
            {
                return false;
            }

            var m1Params = m1.GetParameters();

            for (int i = 0; i < m1Params.Length; i++)
            {
                if (m1Params[i].ParameterType.Name != m2.Parameters[i].ParameterType.Name)
                {
                    return false;
                }
            }

            return true;
        }

        public static string ReplaceInvalidFieldOrArgumentNames(string input)
        {
            if (_keywords.Contains(input))
            {
                return input + "_var";
            }

            return input;
        }

        public static bool TryGetValue(Type theType, [NotNullWhen(true)] out ExternalTypeReference? externalTypeReference)
        {
            // Todo: Remove this overload and assume true
            return TryGetValue(theType, false, out externalTypeReference);
        }

        public static bool TryGetValue(Type theType, bool supportsGenerics, [NotNullWhen(true)]out ExternalTypeReference? externalTypeReference)
        {
            string? name = theType.FullName;
            if (name != null && name.Contains("System.Func", StringComparison.Ordinal))
            {
                name = name + "1";
            }

            foreach (var e in _references)
            {
                if (e.Type == theType)
                {
                    externalTypeReference = e;
                    return true;
                }

                if (supportsGenerics && e.Type.IsGenericTypeDefinition && theType.IsGenericType)
                {
                    var typeDef = theType.GetGenericTypeDefinition();
                    if (typeDef == e.Type)
                    {
                        var args = theType.GetGenericArguments();
                        var fullNewType = e.Type.MakeGenericType(args);
                        externalTypeReference = new ExternalTypeReference(fullNewType, e.Assembly);
                        return true;
                    }
                }
            }

            externalTypeReference = null;
            return false;
        }
    }
}
