using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace Tempo
{

    public class DesktopTypeSet : TypeSet
    {
        public DesktopTypeSet(string name, bool usesWinRTProjections = false, string cacheDirectoryPath = null) 
            : base(name, usesWinRTProjections, cacheDirectoryPath)
        { }

        protected virtual string GetXmlFileName(Assembly a)
        {
            int index = a.Location.LastIndexOf('.');
            return a.Location.Substring(0, index) + ".xml";
        }
        public Dictionary<Assembly, XElement> Xml { get; private set; }

        public override IEnumerable<XElement> GetXmls(TypeViewModel type)
        {
            XElement xml = null;
            if (Xml != null)
                Xml.TryGetValue(type.Assembly, out xml);
            return new XElement[] { xml };
        }

        public override void LoadHelpCore(bool wpf = false, bool winmd = false)
        {
            LoadHelpWorker();
        }

        bool? _isWinmd = null;
        public override bool IsWinmd
        {
            get
            {
                if (_isWinmd == null)
                {
                    if (Assemblies == null || Assemblies.Count == 0)
                    {
                        _isWinmd = false;
                    }
                    else if (Assemblies[0].Location.EndsWith(".winmd", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _isWinmd = true;
                    }
                    else
                        _isWinmd = false;
                }
                return _isWinmd == true;
            }
        }

        void LoadHelpWorker()
        {
            // 39ms for Phone

            foreach (var a in Assemblies)
            {
                var name = GetXmlFileName(a);

                if (File.Exists(name))
                {
                    if (Xml == null)
                        Xml = new Dictionary<Assembly, XElement>();

                    //Xml[a] = XElement.Load(new StreamReader(name));
                    XElement element = null;
                    element = XElement.Load(new StreamReader(name));
                    Xml.Add(a, element);
                }
            }

        }
    }



    public class WpfTypeSet : DesktopTypeSet
    {
        public static string StaticName = "Wpf";
        public WpfTypeSet() : base(StaticName) { }

        protected override string GetXmlFileName(Assembly a)
        {
            return Environment.ExpandEnvironmentVariables("%ProgramFiles%")
                + @"\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\"
                + a.FullName.Substring(0, a.FullName.IndexOf(","))
                + ".xml";
        }
    }


    public class WinFormsTypeSet : DesktopTypeSet
    {
        public static string StaticName = "WinForms";
        public WinFormsTypeSet() : base(StaticName) { }

        protected override string GetXmlFileName(Assembly a)
        {
            return null;
            //return Environment.ExpandEnvironmentVariables("%ProgramFiles%")
            //    + @"\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\"
            //    + a.FullName.Substring(0, a.FullName.IndexOf(","))
            //    + ".xml";
        }
    }

    public class XamFormsTypeSet : DesktopTypeSet
    {
        public static string StaticName = "XamForms";
        public XamFormsTypeSet() : base(StaticName) { }

        protected override string GetXmlFileName(Assembly a)
        {
            return null;
            //return Environment.ExpandEnvironmentVariables("%ProgramFiles%")
            //    + @"\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\"
            //    + a.FullName.Substring(0, a.FullName.IndexOf(","))
            //    + ".xml";
        }
    }


    public class CardsTypeSet : DesktopTypeSet
    {
        public static string StaticName = "Cards";
        public CardsTypeSet() : base(StaticName) { }

        protected override string GetXmlFileName(Assembly a)
        {
            return null;
        }
    }

    public class WinUI2TypeSet : MRTypeSet
    {
        public static string StaticName = "WinUI2";
        public WinUI2TypeSet() : base(StaticName, true) { }

        protected override string GetXmlFileName(Assembly a)
        {
            return null;
        }
    }

    public class WinAppTypeSet : MRTypeSet
    {
        public static string StaticName = "WinAppSDK";
        public static string PackageName = "Microsoft.WindowsAppSDK";

        public WinAppTypeSet(bool useWinRTProjections) : base(StaticName, useWinRTProjections) { }

        protected override string GetXmlFileName(Assembly a)
        {
            return null;
        }
    }

    public class WebView2TypeSet : MRTypeSet
    {
        internal static readonly string PackageName = "Microsoft.Web.WebView2";
        internal static readonly string StaticName = "WebView2";
        public WebView2TypeSet(bool useWinRTProjections) : base(StaticName, useWinRTProjections) { }

        protected override string GetXmlFileName(Assembly a)
        {
            return null;
        }
    }

    public class Win32TypeSet : MRTypeSet
    {
        public static string StaticName = "Win32";
        public static string PackageName = "Microsoft.Windows.SDK.Win32Metadata";

        public Win32TypeSet(bool useWinRTProjections) : base(StaticName, useWinRTProjections) { }

        protected override string GetXmlFileName(Assembly a)
        {
            return null;
        }
    }

    // .Net type set for Tempo1
    public class DotNetTypeSet : DesktopTypeSet
    {
        public static string StaticName = "DotNet";
        public DotNetTypeSet() : base(StaticName) { }

        static public string DotNetCoreVersion;

    }

    // .Net type set for Tempo2
    public class DotNetTypeSet2 : MRTypeSet
    {
        public static string StaticName = "DotNetApp";
        public DotNetTypeSet2(bool useWinRTProjections) : base(StaticName, useWinRTProjections) { }
    }

    public class DotNetWindowsTypeSet : MRTypeSet
    {
        public static string StaticName = "DotNetWindows";
        public DotNetWindowsTypeSet(bool useWinRTProjections) : base(StaticName, useWinRTProjections) { }
    }



    public class CustomTypeSet : WinPlatTypeSet
    {
        new static public string StaticName = "SD";
        public CustomTypeSet() : base(StaticName) { }
    }

    public class WinPlatTypeSet : DesktopTypeSet
    {
        static public string StaticName = "Windows";

        public WinPlatTypeSet() : base(StaticName) { }
        protected WinPlatTypeSet(string name) : base(name) { }

        public override void LoadHelpCore(bool wpf = false, bool winmd = false)
        {
            return;
        }

        public override IEnumerable<XElement> GetXmls(TypeViewModel type)
        {
            return _xmls;
        }

        List<XElement> _xmls = new List<XElement>();
    }

    public class SilverlightTypeSet : DesktopTypeSet
    {
        public SilverlightTypeSet(string name) : base(name) { }

        protected override string GetXmlFileName(Assembly a)
        {
            var name = base.GetXmlFileName(a);

            if (!File.Exists(name) && a.Location.Contains("Silverlight"))
            {
                name = Environment.ExpandEnvironmentVariables("%ProgramFiles%")
                    + @"\Reference Assemblies\Microsoft\Framework\Silverlight\v4.0\"
                    + a.FullName.Substring(0, a.FullName.IndexOf(","))
                    + ".xml";
            }

            return name;
        }
    }

    public class WPTypeSet : DesktopTypeSet
    {
        public static string StaticName = "WP";
        public WPTypeSet() : base(StaticName) { }

        public static string ToolkitPath { get; set; }
    }
}
