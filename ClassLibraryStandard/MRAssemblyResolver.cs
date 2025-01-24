using CommonLibrary;
using MiddleweightReflection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Forms.VisualStyles;

namespace Tempo
{
    // This is used by MrLoadContext to map an assembly name to a file location
    // This implementation assumes that the assembly name is a file in this.DirectoryName
    public class MRAssemblyResolver
    {
        public string DirectoryName { get; set; }

        public string ResolveCustomAssembly(string assemblyName)
        {
            var location = $"{DirectoryName}\\{assemblyName}";

            if (File.Exists(location))
            {
                DebugLog.Append("Loading " + location);
                return location;
            }
            else
            {
                DebugLog.Append($"Couldn't find assembly for namespace '{assemblyName}'");
                location = null;
                return location;
            }
        }
    }

}
