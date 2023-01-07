// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Text;
using Iot.Device.Arduino;
using Microsoft.VisualBasic;

namespace ArduinoCsCompiler.Runtime
{
    [ArduinoReplacement(typeof(TextInfo), true, IncludingPrivates = true)]
    internal class MiniTextInfo
    {
        internal static readonly MiniTextInfo Invariant = new MiniTextInfo();

        public MiniTextInfo()
        {
        }

        [ArduinoImplementation]
        public static bool NeedsTurkishCasing(string localeName)
        {
            return false;
        }

        public bool IsInvariant
        {
            [ArduinoImplementation]
            get
            {
                return true;
            }
        }

        [ArduinoImplementation]
        public unsafe void NlsChangeCase(Char* pSource, Int32 pSourceLen, Char* pResult, Int32 pResultLen, Boolean toUpper)
        {
            throw new NotImplementedException();
        }

        [ArduinoImplementation]
        public string ToLower(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            StringBuilder b = new StringBuilder(str);
            for (int i = 0; i < str.Length; i++)
            {
                b[i] = ToLowerInvariant(str[i]);
            }

            return b.ToString();
        }

        [ArduinoImplementation]
        public string ToUpper(string str)
        {
            if (str == null)
            {
                throw new ArgumentNullException(nameof(str));
            }

            StringBuilder b = new StringBuilder(str);
            for (int i = 0; i < str.Length; i++)
            {
                b[i] = ToUpperInvariant(str[i]);
            }

            return b.ToString();
        }

        [ArduinoImplementation]
        public string ToLowerInvariant(string str)
        {
            return ToLower(str);
        }

        [ArduinoImplementation]
        public string ToUpperInvariant(string str)
        {
            return ToUpper(str);
        }

        [ArduinoImplementation]
        public char ToLowerInvariant(char ch)
        {
            if (ch >= 'A' && ch <= 'Z')
            {
                return (char)(ch - 'A' + 'a');
            }

            return ch;
        }

        [ArduinoImplementation]
        public char ToUpperInvariant(char ch)
        {
            if (ch >= 'a' && ch <= 'z')
            {
                return (char)(ch - 'a' + 'A');
            }

            return ch;
        }

        [ArduinoImplementation]
        public char ToUpper(char ch)
        {
            return ToUpperInvariant(ch);
        }

        [ArduinoImplementation]
        public void ChangeCaseToUpper(System.ReadOnlySpan<Char> source, Span<Char> destination)
        {
            for (int i = 0; i < source.Length; i++)
            {
                destination[i] = ToUpperInvariant(source[i]);
            }
        }
    }
}
