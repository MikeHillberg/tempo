using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Tempo;

namespace Tempo
{

    public class MRTypeSet : DesktopTypeSet
    {
        static public string WindowsCppName = "Windows (C++)";
        static public string WindowsCSName = "Windows (C#)";

        static public string CustomMRName = "Custom (MR)";

        public MRTypeSet(string name, bool usesWinRTProjections) : base(name, usesWinRTProjections) { }

        protected override string GetXmlFileName(Assembly a)
        {
            return Environment.ExpandEnvironmentVariables("%ProgramFiles%")
                + @"\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\"
                + a.FullName.Substring(0, a.FullName.IndexOf(","))
                + ".xml";
        }

        bool _isWinMD = false;
        public override bool IsWinmd => _isWinMD;

        public void SetIsWinmd(bool v)
        {
            _isWinMD = v;
        }
    }

}
