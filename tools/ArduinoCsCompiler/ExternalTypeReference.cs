using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoCsCompiler
{
    internal class ExternalTypeReference
    {
        public ExternalTypeReference(string name, Type type, ExternalAssemblyReference assembly)
        {
            Name = name;
            Type = type;
            Assembly = assembly;
        }

        public string Name { get; }

        public Type Type { get; }

        public ExternalAssemblyReference Assembly { get; }
    }
}
