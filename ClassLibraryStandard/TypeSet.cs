using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Tempo
{
    public class TypeSet : INotifyPropertyChanged
    {
        public TypeSet(string name, bool usesWinRTProjections)
        {
            Name = name;
            _usesWinRTProjections = usesWinRTProjections;
        }

        bool _usesWinRTProjections = false;
        public bool UsesWinRTProjections
        {
            get { return _usesWinRTProjections; }
            private set { _usesWinRTProjections = value; }
        }

        public string Version;

        public bool IsCurrent => this == Manager.CurrentTypeSet;


        IEnumerable<string> _contracts = null;
        public IEnumerable<string> Contracts
        {
            get { return _contracts; }
            set { _contracts = value; RaisePropertyChanged("Contracts"); }
        }

        public void LoadHelp(bool wpf = false, bool winmd = false)
        {
            try
            {
                LoadHelpCore(wpf, winmd);
            }
            catch (Exception e)
            {
                UnhandledExceptionManager.ProcessException(e);
            }
        }

        public virtual void LoadHelpCore(bool wpf = false, bool winmd = false)
        {

        }
        public virtual IEnumerable<XElement> GetXmls(TypeViewModel type)
        {
            return null;
        }
        public string Name { get; private set; }

        public int TypeCount { get; private set; }

        IList<TypeViewModel> _types;
        public IList<TypeViewModel> Types
        {
            get { return _types; }
            set
            {
                _types = value;
                TypeCount = value == null ? 0 : value.Count;
            }
        }

        public virtual bool IsWinmd
        {
            get { return false; }
        }

        public List<Assembly> Assemblies { get; set; } = new List<Assembly>();

        // bugbug: Need an AssemblyViewModel, because with MR there's no System.Assembly type available
        public List<string> AssemblyLocations { get; } = new List<string>();

        public IEnumerable<Object> Namespaces { get; set; }

        Dictionary<string, string> _allNames;
        public Dictionary<string, string> AllNames
        {
            get
            {
                return _allNames;
            }

            set
            {
                _allNames = value;
                RaisePropertyChanged(nameof(AllNames));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        async public void LoadContracts()
        {
            //var dispatcher = Dispatcher.CurrentDispatcher;

            var typeContracts = await BackgroundHelper2.DoWorkAsync(() =>
                {
                    var contracts = new List<string>();
                    foreach (var t in this.Types)
                    {
                        if (string.IsNullOrEmpty(t.Contract))
                        {
                            continue;
                        }

                        if (contracts.Contains(t.Contract))
                        {
                            continue;
                        }

                        contracts.Add(t.Contract);

                    }
                    return contracts;
                });

            typeContracts.Sort();
            this.Contracts = typeContracts;
        }

        List<string> _fullNamespaces = null;
        public IList<string> FullNamespaces
        {
            get
            {
                if (_fullNamespaces != null)
                {
                    return _fullNamespaces;
                }

                //_fullNamespaces = (from t in Types select t.Namespace).Distinct().OrderBy(t => t).ToList<string>();

                var namespaces = (from t in Types select t.Namespace).Distinct();
                var namespaceList = new List<string>();

                foreach (var ns in namespaces)
                {
                    var ns2 = ns;

                    while (true)
                    {
                        if (!namespaceList.Contains(ns2))
                        {
                            namespaceList.Add(ns2);
                        }

                        if (!ns2.Contains("."))
                        {
                            break;
                        }

                        ns2 = ns2.Substring(0, ns2.LastIndexOf('.'));
                    }

                }

                _fullNamespaces = namespaceList.Distinct().OrderBy(t => t).ToList<string>();

                return _fullNamespaces;
            }
        }
    }
}
