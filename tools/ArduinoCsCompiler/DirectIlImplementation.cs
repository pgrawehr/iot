// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoCsCompiler
{
    /// <summary>
    /// This is a weird try: We use an exception to emit a ldstr instruction, which will be
    /// copied as-is to the output in the nano implementation, actually causing the method to
    /// be replaced with the _text_ of the message (which shall be a set of IL instructions)
    /// </summary>
    internal class DirectIlImplementation : Exception
    {
        public DirectIlImplementation(string instructions)
            : base(instructions)
        {
        }
    }
}
