// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace ArduinoCsCompiler
{
    [Flags]
    internal enum InstructionKind
    {
        None,
        Normal,
        Branch = 2,
        BranchTarget = 4,
    }

    internal class IlInstruction
    {
        private readonly byte[] _codeStream;
        private int _argumentAddress;
        private int _argumentSize;

        public IlInstruction(OpCode instruction, int pc, int size, byte[] codeStream)
        {
            _codeStream = codeStream;
            OpCode = instruction;
            Pc = pc;
            PreviousInstructions = new List<IlInstruction>();
            IsReachable = false;
            Size = size;
        }

        /// <summary>
        /// The opcode of the instruction
        /// </summary>
        public OpCode OpCode
        {
            get;
        }

        /// <summary>
        ///  The PC where the instruction is (the offset from the beginning of the method body)
        /// </summary>
        public int Pc
        {
            get;
        }

        /// <summary>
        /// Size of the instruction in bytes, including opcode and argument. This is used to calculate the PC of the next instruction,
        /// and for branch instructions to calculate the target address.
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Regularly next instruction. False case for a branch instruction
        /// </summary>
        public IlInstruction? NextInstruction
        {
            get;
            set;
        }

        public IlInstruction? BranchTarget
        {
            get;
            set;
        }

        public int BranchTargetPc
        {
            get;
            set;
        }

        public List<IlInstruction> PreviousInstructions
        {
            get;
        }

        public bool IsReachable
        {
            get;
            set;
        }

        public Span<byte> ArgumentAddress
        {
            get
            {
                return new Span<byte>(_codeStream, _argumentAddress, _argumentSize);
            }
        }

        public string Name
        {
            get
            {
                return OpCodeDefinitions.OpcodeDef[(int)OpCode].Name;
            }
        }

        public OpCodeType OpcodeType
        {
            get
            {
                return OpCodeDefinitions.OpcodeDef[(int)OpCode].Type;
            }
        }

        public void SetArgument(int argumentOffset, int argumentSize)
        {
            _argumentAddress = argumentOffset;
            _argumentSize = argumentSize;
        }

        private int DecodeIntegerArgument()
        {
            if (ArgumentAddress.Length == 1)
            {
                // A single-byte argument
                uint a = ArgumentAddress[0];
                if ((a & 0x80) == 0x80)
                {
                    // Manual sign-extension
                    a = a | 0xFFFFFF00;
                }

                return (int)a;
            }
            else
            {
                return ArgumentAddress[0] | ArgumentAddress[1] << 8 | ArgumentAddress[2] << 16 | ArgumentAddress[3] << 24;
            }
        }

        private long DecodeLongArgument()
        {
            return BitConverter.ToInt64(ArgumentAddress);
        }

        private double DecodeDoubleArgument()
        {
            return BitConverter.ToDouble(ArgumentAddress);
        }

        private float DecodeFloatArgument()
        {
            return BitConverter.ToSingle(ArgumentAddress);
        }

        /// <summary>
        /// Escapes the provided string, so it can be used as argument to ldstr
        /// </summary>
        private string EscapeString(string input)
        {
            return Microsoft.CodeAnalysis.CSharp.SymbolDisplay.FormatLiteral(input, true);
        }

        public string? DecodeArgument(ExecutionSet set, Func<ExecutionSet, IlInstruction, int, string>? tokenDecoder)
        {
            switch (OpcodeType)
            {
                case OpCodeType.InlineI:
                case OpCodeType.ShortInlineI:
                    {
                        int arg = DecodeIntegerArgument();
                        return $"{arg} // (0x{arg:X})";
                    }

                case OpCodeType.ShortInlineVar:
                case OpCodeType.InlineVar:
                    {
                        int arg = DecodeIntegerArgument();
                        return $"{arg}";
                    }

                case OpCodeType.ShortInlineBrTarget:
                case OpCodeType.InlineBrTarget:
                    {
                        int offset = DecodeIntegerArgument();
                        if (tokenDecoder == null)
                        {
                            return $"Offset {offset}, --> 0x{(offset + Pc + Size):X}"; // Offset is from beginning of next instruction
                        }
                        else
                        {
                            return $"IL_{(offset + Pc + Size):X4} // Offset: {offset}";
                        }
                    }

                case OpCodeType.InlineField:
                    {
                        int token = DecodeIntegerArgument();
                        string? fieldName = tokenDecoder?.Invoke(set, this, token) ?? $"0x{token:X8} // Field token";
                        return $"{fieldName} // Token 0x{token:X}";
                    }

                case OpCodeType.InlineMethod:
                    {
                        int token = DecodeIntegerArgument();
                        var method = set.InverseResolveToken(token);

                        if (method == null)
                        {
                            return $"{token} - Unable to resolve";
                        }

                        if (tokenDecoder != null)
                        {
                            string? me = tokenDecoder.Invoke(set, this, token);
                            return $"{me} // {method.MemberInfoSignature(false)}";
                        }

                        return $"{token} - {method.MemberInfoSignature(false)}";
                    }

                case OpCodeType.InlineString:
                    {
                        int token = DecodeIntegerArgument();
                        string value = set.GetString(token);
                        value = EscapeString(value);
                        int cntSymbols = value.Count(x => x == '\"');
                        string ret = $"{value} // Token {token}";
                        if (cntSymbols % 2 == 1)
                        {
                            ret += "\" (an odd number of quotes, fix it to avoid problems in the editor)";
                        }

                        return ret;
                    }

                case OpCodeType.InlineI8:
                    {
                        long value = DecodeLongArgument();
                        return value.ToString(CultureInfo.InvariantCulture);
                    }

                case OpCodeType.ShortInlineR:
                    {
                        return DecodeFloatArgument().ToString(CultureInfo.InvariantCulture);
                    }

                case OpCodeType.InlineR:
                    {
                        return DecodeDoubleArgument().ToString(CultureInfo.InvariantCulture);
                    }

                case OpCodeType.InlineTok:
                case OpCodeType.InlineType:
                    {
                        int token = DecodeIntegerArgument();
                        string typeName;
                        if (tokenDecoder != null)
                        {
                            typeName = tokenDecoder.Invoke(set, this, token);
                        }
                        else
                        {
                            typeName = $"0x{token:X8} // Type token";
                        }

                        return $"{typeName}";
                    }

                case OpCodeType.InlineSwitch:
                    {
                        // Length is officially an uint, but in practice it is always a small number, so we can decode it as int for simplicity
                        int count = DecodeIntegerArgument();
                        if (count <= 0)
                        {
                            return $"() // Switch without cases"; // ???
                        }

                        List<string> cases = new List<string>();
                        Span<int> addresses = MemoryMarshal.Cast<byte, int>(ArgumentAddress.Slice(4));
                        foreach (var element in addresses)
                        {
                            int targetAddress = element + Pc + 5 + (count * 4);
                            cases.Add($"IL_{targetAddress:X4}");
                        }

                        return $"({string.Join(", ", cases)})";
                    }
            }

            return null;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
