using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArduinoCsCompiler
{
    /// <summary>
    /// A reference to an external assembly, when generating IL
    /// </summary>
    internal class ExternalAssemblyReference
    {
        public ExternalAssemblyReference(string name, string publicKeyToken, string version)
        {
            Name = name;
            PublicKeyToken = publicKeyToken;
            Version = version;
        }

        public string Name
        {
            get;
        }

        public string PublicKeyToken
        {
            get;
        }

        public string Version
        {
            get;
        }
    }
}
