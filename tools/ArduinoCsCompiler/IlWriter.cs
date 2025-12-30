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

    private List<string> _originalClassesToUse = new List<string>()
    {
        "System.Object",
        "System.ValueType",
        "System.Enum",
        "<PrivateImplementationDetails>" // What was this one about?
    };

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

            if (_originalClassesToUse.Contains(cls1.FullName!))
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

    private void WriteClasses(IndentedTextWriter tw)
    {
        foreach (var cl in _set.Classes)
        {
            var baseClass = GetClassDeclaration(cl.TheType.BaseType);
            if (cl.UseOriginalType)
            {
                continue;
            }

            // TODO: These should be written, not the concrete types
            if (cl.TheType.IsGenericType && cl.TheType.ContainsGenericParameters)
            {
                // Open generic type
                continue;
            }

            string baseName = "System.Object";
            if (baseClass != null)
            {
                var cls1 = _set.Classes.First(x => x.FullName == baseClass.FullName);
                baseName = cls1.FullName ?? string.Empty;
            }

            bool isClass = !cl.TheType.IsValueType;

            string name = cl.FullName!;

            string extends = "extends";
            if (string.IsNullOrWhiteSpace(baseName))
            {
                extends = string.Empty; // E.g open generic classes end up here
            }

            tw.WriteLine($".class public auto ansi{(cl.TheType.IsSealed ? " sealed" : string.Empty)} beforefieldinit {name} {extends} {baseName}");
            if (cl.WrappedInterfaces != null && cl.WrappedInterfaces.Any())
            {
                tw.Write("implements ");
                tw.WriteLine(String.Join(", " + Environment.NewLine + "    ", cl.WrappedInterfaces!.Select(x => x.FullName).Where(y => !string.IsNullOrWhiteSpace(y))));
            }

            tw.WriteLine("{");
            tw.Indent = 1;
            WriteMethods(tw);
            tw.Indent = 0;
            tw.WriteLine("}");
        }
    }

    private void WriteMethods(IndentedTextWriter tw)
    {
    }

    private void WriteHeader(IndentedTextWriter tw)
    {
        String moduleName = _set.CompilerSettings.ProcessName ?? "CompiledLibrary";
        tw.WriteLine($".module {moduleName}.dll");

        // This is the nanoframework core dependency
        tw.WriteLine(@".assembly extern mscorlib
{
  .publickeytoken = (C0 7D 48 1E 97 58 C7 31 )                         // .}H..X.1
  .ver 1:15:6:0
}");
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
