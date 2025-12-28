// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using System.Linq;

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
    }

    /// <summary>
    /// Does all kinds of patching necessary for exporting (actually, some of the magic we did before needs
    /// to be undone)
    /// </summary>
    private void PatchAndUpdate()
    {
        // Remove "System.Object" from the list of classes
        _set.Classes.Remove(_set.Classes.First(x => x.TheType.BaseType == null));
    }

    private void WriteClasses(IndentedTextWriter tw)
    {
        foreach (var cl in _set.Classes)
        {
            var baseClass = cl.TheType.BaseType;
            string baseName = "System.Object";
            if (baseClass != null)
            {
                baseName = baseClass.FullName!;
            }

            tw.WriteLine($".class public auto ansi{(cl.TheType.IsSealed ? " sealed" : string.Empty)} beforefieldinit {cl.TheType.FullName} extends {baseName}");
            foreach (var interf in cl.Interfaces)
            {
                tw.WriteLine($"implements {interf.FullName}");
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
