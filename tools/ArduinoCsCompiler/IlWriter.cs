// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoCsCompiler
{
    public class IlWriter
    {
        public static void WriteMethodHeader(TextWriter w, ArduinoMethodDeclaration code)
        {
            string signature = code.MethodBase.MethodSignature(false, true);
            w.WriteLine($"{signature} cil managed");

        }

        public static void WriteClassHeader(TextWriter w, ClassDeclaration cls)
        {
            w.WriteLine($".class public auto ansi beforefieldinit {cls.Name} extends {cls.TheType.BaseType}");
        }

        public static void WriteClass(TextWriter w, ClassDeclaration cls, ExecutionSet executionSet)
        {
            WriteClassHeader(w, cls);
            w.WriteLine("{");
            foreach (var member in cls.Members)
            {
                if (member.Method != null)
                {
                    var method = executionSet.Methods().FirstOrDefault(x => x.MethodBase == new EquatableMethod(member.Method));
                    if (method != null)
                    {
                        WriteMethod(w, method, executionSet);
                    }
                }
            }

            w.WriteLine("}");
        }

        public static void WriteMethod(TextWriter w, ArduinoMethodDeclaration code, ExecutionSet executionSet)
        {
            WriteMethodHeader(w, code);
            w.WriteLine("{");
            w.WriteLine($".maxstack {code.MaxStack}");
            string methodBody = IlCodeParser.DecodedAssembly(code, executionSet);
            w.WriteLine(methodBody);
            w.WriteLine("}");
        }
    }
}
