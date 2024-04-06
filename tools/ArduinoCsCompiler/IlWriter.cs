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
            string returnType = string.Empty;
            string methodName = code.Name;
            if (code.Flags.HasFlag(MethodFlags.Ctor) == false) // Otherwise, there is no return type
            {
                returnType = code.MethodInfo.ReturnType.ToString();
            }
            else
            {
                methodName = "specialname rtspecialname instance void .ctor";
            }

            w.WriteLine($".method public hidebysig {(code.Flags.HasFlag(MethodFlags.Static) ? "static" : string.Empty)} {returnType} {methodName}() cil managed");

        }

        public static void WriteMethod(TextWriter w, ArduinoMethodDeclaration code, ExecutionSet executionSet)
        {
            WriteMethodHeader(w, code);
            w.WriteLine("{");
            string methodBody = IlCodeParser.DecodedAssembly(code, executionSet, 0, String.Empty);
            w.WriteLine(methodBody);
            w.WriteLine("}");
        }
    }
}
