// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Reflection;

namespace ArduinoCsCompiler;

/// <summary>
/// Writes out the IL, so it can be reused as library in a nanoFramework project
/// </summary>
public class IlWriter
{
    private readonly ExecutionSet _set;
    private readonly string _outputFile;

    public IlWriter(ExecutionSet set, string outputFile)
    {
        _set = new ExecutionSet(set, null!, set.CompilerSettings);
        _outputFile = outputFile;
    }

    public void Write()
    {
        PatchAndUpdate();
        using FileStream fs = new FileStream(_outputFile, FileMode.Create, FileAccess.ReadWrite);
        IndentedTextWriter tw = new IndentedTextWriter(new StreamWriter(fs, Encoding.UTF8));

        WriteHeader(tw);
        WriteClasses(tw);
        tw.Flush();
    }

    /// <summary>
    /// Does all kinds of patching necessary for exporting (actually, some of the magic we did before needs
    /// to be undone)
    /// </summary>
    private void PatchAndUpdate()
    {
        var ordered = _set.Classes.ToList();
        ordered.Sort(new ByNestingSorter(_set));
        foreach (var cls1 in ordered)
        {
            // Fill the list (interfaces as ClassDeclarations, so we later get their fixed names as well)
            // Strip any interfaces that are not found from the declaration (these include those earlier suppressed, such
            // as IConvertible)
            cls1.WrappedInterfaces = cls1.RawInterfaces.Select(GetClassDeclaration).Where(z => z != null).ToList()!;

            string? n = cls1.FullName;

            if (cls1.FullName == null)
            {
                continue;
            }

            if (cls1.TheType.IsArray)
            {
                // We do not need to write out array types, they're rebuilt automatically again by the compiler
                cls1.UseOriginalType = true;
            }

            if (ExternalSystemReferences.TryGetValue(cls1.TheType, out var reference))
            {
                cls1.UseOriginalType = true;
            }

            cls1.ConstructName(_set);
        }
    }

    public ClassDeclaration? GetClassDeclaration(Type? ofClass)
    {
        return ClassDeclaration.GetClassDeclaration(_set, ofClass);
    }

    private long GetFieldValue(Type classType, FieldInfo field)
    {
        var fieldValue = field.GetValue(null)!;
        long token = 0;
        Type underlyingType = Enum.GetUnderlyingType(classType);
        if (underlyingType == typeof(int))
        {
            unchecked
            {
                int v = (int)fieldValue;
                token = v;
            }
        }
        else if (underlyingType == typeof(UInt32))
        {
            unchecked
            {
                uint v = (UInt32)fieldValue;
                token = (int)v;
            }
        }
        else if (underlyingType == typeof(byte))
        {
            unchecked
            {
                byte v = (byte)fieldValue;
                token = v;
            }
        }
        else if (underlyingType == typeof(UInt16))
        {
            unchecked
            {
                UInt16 v = (UInt16)fieldValue;
                token = v;
            }
        }
        else if (underlyingType == typeof(Int16))
        {
            unchecked
            {
                Int16 v = (Int16)fieldValue;
                token = v;
            }
        }
        else
        {
            throw new NotSupportedException($"Unable to cast {fieldValue} to a constant, when trying to read constant {field.Name} of {classType.MemberInfoSignature()}");
        }

        return token;
    }

    private void WriteClasses(IndentedTextWriter tw)
    {
        foreach (var cl in _set.Classes)
        {
            var baseClass = GetClassDeclaration(cl.TheType.BaseType);
            if (cl.UseOriginalType)
            {
                continue;
            }

            if (cl.TheType.IsEnum)
            {
                tw.WriteLine($".class public auto ansi sealed {cl.FullName} extends [mscorlib]System.Enum");
                tw.WriteLine("{");
                var declared = cl.TheType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                tw.Indent++;

                if (!ExternalSystemReferences.TryGetValue(declared[0].FieldType, out var enumTypeName))
                {
                    throw new InvalidOperationException($"{declared[0].FieldType} is not a known enum base type");
                }

                tw.WriteLine($".field public specialname rtspecialname {enumTypeName.Name} {declared[0].Name}");
                for (int i = 1; i < declared.Length; i++)
                {
                    long value = GetFieldValue(cl.TheType, declared[i]);
                    tw.WriteLine($".field public static literal valuetype {cl.FullName} {declared[i].Name} = {enumTypeName.Name}({value})");
                }

                tw.Indent--;
                tw.WriteLine("}");
                continue;
            }

            // TODO: These should be written, not the concrete types
            if (cl.TheType.IsGenericType && cl.TheType.ContainsGenericParameters)
            {
                // Open generic type
                continue;
            }

            string baseName = "object";
            if (baseClass != null)
            {
                var cls1 = _set.Classes.First(x => x.FullName == baseClass.FullName);
                baseName = cls1.FullName ?? string.Empty;
            }

            string name = cl.FullName!;

            if (string.IsNullOrWhiteSpace(name))
            {
                continue; // Things like {T} itself
            }

            string extends = "extends";
            if (string.IsNullOrWhiteSpace(baseName))
            {
                extends = string.Empty; // E.g open generic classes end up here
            }

            if (cl.TheType.IsInterface)
            {
                tw.WriteLine($".class interface public abstract auto ansi beforefieldinit {name}"); // No "extends System.Object"
            }
            else
            {
                tw.WriteLine($".class public auto ansi{(cl.TheType.IsSealed ? " sealed" : string.Empty)} beforefieldinit {name} {extends} {baseName}");
            }

            if (cl.WrappedInterfaces != null && cl.WrappedInterfaces.Any())
            {
                tw.Write("implements ");
                tw.WriteLine(String.Join(", " + Environment.NewLine + "    ", cl.WrappedInterfaces!.Select(x => x.FullName).Where(y => !string.IsNullOrWhiteSpace(y))));
            }

            tw.WriteLine("{");
            tw.Indent = 1;
            WriteFields(tw, cl);
            WriteMethods(tw, cl);
            tw.Indent = 0;
            tw.WriteLine("}");
        }
    }

    /// <summary>
    /// Prefixes the type with "class" when it's a reference type and not a short name
    /// </summary>
    private string PrefixWithClassKeyword(ClassDeclaration ty, string name)
    {
        // TODO: That's a bit of a hack to detect a short name (which is not to be prefixed with 'class' or 'valuetype')
        if (!name.Contains(".", StringComparison.Ordinal))
        {
            return name;
        }

        if (!ty.TheType.IsValueType)
        {
            return $"class {name}";
        }
        else
        {
            return $"valuetype {name}";
        }
    }

    private void WriteFields(IndentedTextWriter tw, ClassDeclaration cl)
    {
        foreach (ClassMember f in cl.Members.Where(x => x.Field != null))
        {
            bool isStatic = f.Field!.IsStatic;
            String fieldTypeName;
            if (f.Field.FieldType.IsArray)
            {
                Type baseType = f.Field.FieldType.GetElementType()!;
                int arrayLevel = 1;
                // Stagged arrays
                while (baseType.IsArray)
                {
                    arrayLevel++;
                    baseType = baseType.GetElementType()!;
                }

                var t = GetClassDeclaration(baseType);
                if (t != null)
                {
                    fieldTypeName = $"{t.FullName}";
                    for (int i = 0; i < arrayLevel; i++)
                    {
                        fieldTypeName += "[]";
                    }

                    fieldTypeName = PrefixWithClassKeyword(t, fieldTypeName);
                }
                else if (f.Field.FieldType.IsValueType)
                {
                    fieldTypeName = $"int32 /* Unknown enum type {f.Field.FieldType} */";
                }
                else
                {
                    fieldTypeName = $"object /* unknown type {f.Field.FieldType} */";
                }
            }
            else
            {
                var t = GetClassDeclaration(f.Field.FieldType);
                if (t != null)
                {
                    fieldTypeName = t.FullName!;
                    // TODO: That's a bit of a hack to detect a short name (which is not to be prefixed with 'class')
                    fieldTypeName = PrefixWithClassKeyword(t, fieldTypeName);
                }
                else
                {
                    fieldTypeName = $"object /* Unknown type {f.Field.FieldType} */";
                }
            }

            tw.WriteLine($".field public {(isStatic ? "static " : string.Empty)}{fieldTypeName} {f.FieldName}");
        }
    }

    private void WriteMethods(IndentedTextWriter tw, ClassDeclaration cl)
    {
    }

    private void WriteHeader(IndentedTextWriter tw)
    {
        String moduleName = _set.CompilerSettings.ProcessName ?? "CompiledLibrary";
        moduleName = moduleName.Replace(".dll", string.Empty);
        tw.WriteLine($".module {moduleName}.dll");

        // This is the nanoframework core dependency
        // TODO: Replace with contents of ExternalAssemblyReference
        tw.WriteLine("""
                     .assembly extern mscorlib
                     {
                       .publickeytoken = (C0 7D 48 1E 97 58 C7 31 )                         // .}H..X.1
                       .ver 1:15:6:0
                     }
                     
                     """ +

                     $".assembly {moduleName}" +
                     """
                     
                     {
                       .custom instance void [mscorlib]System.Runtime.Versioning.TargetFrameworkAttribute::.ctor(string) = ( 01 00 1E 2E 4E 45 54 6E 61 6E 6F 46 72 61 6D 65   // ....NETnanoFrame
                                                                                                                             77 6F 72 6B 2C 56 65 72 73 69 6F 6E 3D 76 31 2E   // work,Version=v1.
                                                                                                                             30 01 00 54 0E 14 46 72 61 6D 65 77 6F 72 6B 44   // 0..T..FrameworkD
                                                                                                                             69 73 70 6C 61 79 4E 61 6D 65 16 2E 4E 45 54 20   // isplayName..NET 
                                                                                                                             6E 61 6E 6F 46 72 61 6D 65 77 6F 72 6B 20 31 2E   // nanoFramework 1.
                                                                                                                             30 )
                       .ver 1:0:0:0
                     }
                     """);
    }

    /// <summary>
    /// Sort types according to their generic nesting levels (e.g 'int' comes before 'Nullable{int}' and this before 'List{Nullable{int}}')
    /// </summary>
    private class ByNestingSorter : IComparer<ClassDeclaration>
    {
        private ExecutionSet _set;

        public ByNestingSorter(ExecutionSet set)
        {
            _set = set;
        }

        public int Compare(ClassDeclaration? x, ClassDeclaration? y)
        {
            if (ReferenceEquals(x, y))
            {
                return 0;
            }

            if (x == null || y == null)
            {
                return 0;
            }

            int leftNesting = GenericNesting(x);
            int rightNesting = GenericNesting(y);

            return leftNesting.CompareTo(rightNesting);
        }

        private int GenericNesting(ClassDeclaration x)
        {
            int ret = 0;
            GenericNestingRecursion(ref ret, x);
            return ret;
        }

        private void GenericNestingRecursion(ref int value, ClassDeclaration? x)
        {
            if (x == null)
            {
                return;
            }

            if (x.TheType.IsGenericType == false)
            {
                return;
            }

            value++;
            int max = value;
            foreach (var genericArg in x.TheType.GenericTypeArguments)
            {
                int c = value;
                GenericNestingRecursion(ref c, ClassDeclaration.GetClassDeclaration(_set, genericArg));
                if (c > max)
                {
                    max = c;
                }
            }

            value = max;
        }
    }
}
