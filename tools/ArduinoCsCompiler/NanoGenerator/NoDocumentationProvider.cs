using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.Documentation;
using ICSharpCode.Decompiler.TypeSystem;

namespace ArduinoCsCompiler.NanoGenerator
{
    internal class NoDocumentationProvider : IDocumentationProvider
    {
        public string GetDocumentation(IEntity entity)
        {
            return $"This could be the documentation for {entity.Name}";
        }
    }
}
