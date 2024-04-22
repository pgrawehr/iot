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
using ICSharpCode.Decompiler.Documentation;
using ICSharpCode.Decompiler.IL;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using Microsoft.CodeAnalysis;
using SyntaxTree = ICSharpCode.Decompiler.CSharp.Syntax.SyntaxTree;

namespace ArduinoCsCompiler
{
    public class IlWriter
    {
        public const string GENERATED_NAMESPACE = "NanoInput";
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

        public static bool ClassToIgnoreAsParent(ClassDeclaration t)
        {
            if (t.TheType == typeof(object))
            {
                return true;
            }

            if (t.TheType == typeof(ValueType))
            {
                return true;
            }

            return false;
        }

        public unsafe void Write(string sourceFile, string outFile)
        {
            var settings = new DecompilerSettings()
            {
                UseSdkStyleProjectFormat = false,
                DecompileMemberBodies = true,
            };

            using TextWriter tw = new StreamWriter("C:\\projects\\iot4\\tools\\ArduinoCsCompiler\\samples\\BlinkingLedNano\\generated.cs");
            SyntaxTree tree = new SyntaxTree();
            var node = new NamespaceDeclaration(GENERATED_NAMESPACE);
            tree.AddChild(node, SyntaxTree.MemberRole);

            foreach (var cls in _executionSet.Classes)
            {
                // Don't write the definition of System.Object.
                if (cls.NewToken == (int)KnownTypeTokens.Object)
                {
                    continue;
                }

                if (cls.NewToken == (int)KnownTypeTokens.ValueType)
                {
                    continue;
                }

                var typeSystemAstBuilder = new TypeSystemAstBuilder();
                typeSystemAstBuilder.GenerateBody = true;
                var wrapped = new ClassWrapper(cls, _executionSet);
                EntityDeclaration decl = typeSystemAstBuilder.ConvertEntity(wrapped);

                node.AddChild(new Comment($"<summary>{cls.Name}</summary>", CommentType.Documentation), Roles.Comment);
                node.AddChild(decl, SyntaxTree.MemberRole);
                foreach (var member in cls.Members)
                {
                    if (member.Field != null)
                    {
                        var fieldWrapped = new FieldWrapper(cls, member, _executionSet);
                        decl.AddChild(new Comment($"<summary>{member.Name}</summary>", CommentType.Documentation), Roles.Comment);
                        EntityDeclaration fld = typeSystemAstBuilder.ConvertEntity(fieldWrapped);
                        decl.AddChild(fld, Roles.TypeMemberRole);
                    }

                    if (member.Method != null)
                    {
                        var arduinoMethod = _executionSet.GetMethod(member.Method, false);
                        if (arduinoMethod == null)
                        {
                            var rep = _executionSet.GetReplacement(member.Method, member.Method);
                            if (rep != null)
                            {
                                arduinoMethod = _executionSet.GetMethod(rep, false);
                            }

                            // arduinoMethod can still be null, e.g. for implicit ctors.
                        }

                        // Assume we don't need to output generated implicit methods
                        if (arduinoMethod != null)
                        {
                            var ctx = new GenericContext();
                            var methodWrapped = new MethodWrapper(cls, member, arduinoMethod, _executionSet);
                            decl.AddChild(new Comment($"<summary>{member.Name}, Token {member.Token:X8}</summary>", CommentType.Documentation), Roles.Comment);
                            EntityDeclaration method = typeSystemAstBuilder.ConvertEntity(methodWrapped);
                            fixed (byte* ptr = arduinoMethod.Code.IlBytes)
                            {
                                CSharpDecompiler decomp = new CSharpDecompiler(new DecompilerTypeSystemWrapper(), settings);
                                decomp.DocumentationProvider = new NoDocumentationProvider();
                                decomp.Decompile(arduinoMethod.Code.IlBytes, HandleKind.MethodDefinition, methodWrapped);
                                decl.AddChild(method, Roles.TypeMemberRole);
                            }
                        }
                    }
                }
            }

            var formatting = FormattingOptionsFactory.CreateSharpDevelop();
            tree.AcceptVisitor(new CSharpOutputVisitor(tw, formatting));
        }
    }
}
