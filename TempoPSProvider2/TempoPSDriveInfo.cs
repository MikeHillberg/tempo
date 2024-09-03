using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace TempoPSProvider
{
    internal class TempoPSDriveInfo : PSDriveInfo
    {
        public TempoPSDriveInfo(PSDriveInfo driveInfo)
              : base(driveInfo)
        {
        }

        // All namespaces, including internal namespaces that have no types
        public HashSet<string> Namespaces { get; set; }

    }
}
