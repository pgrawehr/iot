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
        foreach (var cls1 in _set.Classes)
        {
            // Fill the list (interfaces as ClassDeclarations, so we later get their fixed names as well)
            // Strip any interfaces that are not found from the declaration (these include those earlier suppressed, such
            // as IConvertible)
            cls1.WrappedInterfaces = cls1.RawInterfaces.Select(GetClassDeclaration).Where(z => z != null).ToList()!;

            if (cls1.FullName == null)
            {
                continue;
            }

            if (cls1.TheType.IsArray)
            {
                // We do not need to write out array types, they're rebuilt automatically again by the compiler
                cls1.UseOriginalType = true;
            }

            if (cls1.TheType.IsGenericType && cls1.TheType.IsConstructedGenericType)
            {
                // We will later remove these alltogether, as the runtime now really supports generics.
                // However, for now we just make sure they have a valid name (can be anything, actually, just not their full name)
                string newName = cls1.FullName.Substring(0, cls1.FullName.IndexOf("[[", StringComparison.Ordinal));
                newName += "_with_";
                newName += string.Join("_and_", cls1.TheType.GenericTypeArguments.Select(x => x.Name));
                cls1.FullName = newName;
            }

            if (cls1.FullName.Contains('+'))
            {
                cls1.FullName = cls1.FullName.Replace("+", "_in_");
            }

            if (_originalClassesToUse.Contains(cls1.FullName!))
            {
                cls1.UseOriginalType = true;
            }
        }
    }

    private ClassDeclaration? GetClassDeclaration(Type? ofClass)
    {
        if (ofClass == null)
        {
            return null;
        }

        int tk = _set.GetOrAddClassToken(ofClass.GetTypeInfo());
        return _set.Classes.FirstOrDefault(y => y.NewToken == tk);
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

            string baseName = "System.Object";
            if (baseClass != null)
            {
                var cls1 = _set.Classes.First(x => x.FullName == baseClass.FullName);
                baseName = cls1.FullName ?? string.Empty;
            }

            bool isClass = !cl.TheType.IsValueType;

            string name = cl.FullName!;

            tw.WriteLine($".class public auto ansi{(cl.TheType.IsSealed ? " sealed" : string.Empty)} beforefieldinit {name} extends {baseName}");
            if (cl.WrappedInterfaces != null && cl.WrappedInterfaces.Any())
            {
                tw.Write("implements ");
                tw.WriteLine(String.Join(", " + Environment.NewLine, cl.WrappedInterfaces!.Select(x => x.FullName)));
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
}
