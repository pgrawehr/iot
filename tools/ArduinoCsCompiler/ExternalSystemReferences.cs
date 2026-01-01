using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoCsCompiler
{
    internal static class ExternalSystemReferences
    {
        public static List<ExternalTypeReference> References;

        static ExternalSystemReferences()
        {
            References = new List<ExternalTypeReference>();
            var mscorlib = new ExternalAssemblyReference("mscorlib", "(C0 7D 48 1E 97 58 C7 31 )", "1:15:6:0");
            var builtin = new ExternalAssemblyReference(string.Empty, string.Empty, string.Empty); // for built-in types, such as object or int

            References.AddRange(new ExternalTypeReference[]
                    {
                        new("int32", typeof(System.Int32), builtin),
                        new("uint32", typeof(System.UInt32), builtin),
                        new("int16", typeof(System.Int16), builtin),
                        new("uint16", typeof(System.UInt16), builtin),
                        new("int8", typeof(System.SByte), builtin),
                        new("uint8", typeof(System.Byte), builtin),
                        new("object", typeof(System.Object), builtin),
                        new("string", typeof(System.String), builtin),
                        new("bool", typeof(System.Boolean), builtin),
                        new("float64", typeof(System.Double), builtin),
                        new("float32", typeof(System.Single), builtin)
                    });

            References.AddRange(new ExternalTypeReference[]
            {
                new("System.ValueType", typeof(System.ValueType), mscorlib),
                new("System.Enum", typeof(System.Enum), mscorlib),
            });
        }
    }
}
