// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.Metadata;

namespace ArduinoCsCompiler.NanoGenerator
{
    internal class MetadataFileWrapper : MetadataFile
    {
        public MetadataFileWrapper(MetadataFileKind kind, string fileName, MetadataReaderProvider metadata, MetadataReaderOptions metadataOptions = MetadataReaderOptions.Default, int metadataOffset = 0, bool isEmbedded = false)
            : base(kind, fileName, metadata, metadataOptions, metadataOffset, isEmbedded)
        {
        }

    }
}
