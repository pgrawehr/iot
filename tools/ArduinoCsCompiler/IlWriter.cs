// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ArduinoCsCompiler.NanoGenerator;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.TypeSystem;

namespace ArduinoCsCompiler
{
    public class IlWriter
    {
        private readonly ExecutionSet _executionSet;
        private readonly IlCapabilities _ilCapabilities;

        public IlWriter(ExecutionSet executionSet, IlCapabilities ilCapabilities)
        {
            _executionSet = executionSet;
            _ilCapabilities = ilCapabilities;
        }

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

        public void Write(string sourceFile, string outFile)
        {
            var settings = new DecompilerSettings()
            {
                UseSdkStyleProjectFormat = false,
            };

            using TextWriter tw = new StreamWriter("output.cs");
            SyntaxTree tree = new SyntaxTree();
            var node = new NamespaceDeclaration("Decompiled");
            tree.AddChild(node, SyntaxTree.MemberRole);

            foreach (var cls in _executionSet.Classes)
            {
                var typeSystemAstBuilder = new TypeSystemAstBuilder();
                var wrapped = new ClassWrapper(cls, _executionSet);
                EntityDeclaration decl = typeSystemAstBuilder.ConvertEntity(wrapped);
                node.AddChild(decl, SyntaxTree.MemberRole);
                foreach (var member in cls.Members)
                {
                    if (member.Field != null)
                    {
                        var fieldWrapped = new FieldWrapper(cls, member, _executionSet);
                        EntityDeclaration fld = typeSystemAstBuilder.ConvertEntity(fieldWrapped);
                        decl.AddChild(fld, Roles.TypeMemberRole);
                    }
                }
            }

            var formatting = FormattingOptionsFactory.CreateSharpDevelop();
            tree.AcceptVisitor(new CSharpOutputVisitor(tw, formatting));
        }
    }
}
