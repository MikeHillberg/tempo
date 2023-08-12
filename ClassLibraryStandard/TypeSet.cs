using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Xml.Linq;
using System.Runtime.CompilerServices;
using System.Data;
using System.Diagnostics;
using MiddleweightReflection;

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

                OnTypesUpdated();
            }
        }

        async void OnTypesUpdated()
        {
            // Update the AllNames property based on these types
            await CalculateAllNamesAsync();

            // Set IsInTypes for all of the types
            foreach (var type in Types)
            {
                type.IsInTypes = true;
            }

            // Calculate type.ReturnedByAsync property
            await CalculateReturnedByAsync();
        }


        public virtual bool IsWinmd
        {
            get { return false; }
        }

        public List<Assembly> Assemblies { get; set; } = new List<Assembly>();

        // bugbug: Need an AssemblyViewModel, because with MR there's no System.Assembly type available
        public List<string> AssemblyLocations { get; } = new List<string>();

        public IEnumerable<Object> Namespaces { get; set; }

        KeyValuePair<string, string>[] _allNames;
        public KeyValuePair<string, string>[] AllNames
        {
            get
            {
                return _allNames;
            }

            private set
            {
                _allNames = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Get anything that could be a search name, to be used in auto-suggest
        /// </summary>
        async Task CalculateAllNamesAsync()
        {
            var names1 = new Dictionary<string, string>(_types.Count);
            KeyValuePair<string, string>[] names2 = null;

            var thread = new Thread(() =>
            {
                foreach (var type in _types)
                {
                    // Only public types
                    if (!type.IsPublic)
                    {
                        continue;
                    }

                    // Add the type name
                    if (type.IsGenericType)
                    {
                        // For example, use IVector rather than IVector`1
                        AddName(names1, type.GenericTypeName);
                    }
                    else
                    {
                        AddName(names1, type.Name);
                    }

                    // Add all the member names
                    foreach (var member in type.Members)
                    {
                        // Only public-ish members
                        if (!member.IsPublic && !member.IsProtected)
                        {
                            continue;
                        }

                        var constructorVM = member as ConstructorViewModel;
                        var methodVM = member as MethodViewModel;

                        IList<ParameterViewModel> parameters = null;

                        // Add the member name for everthing but constructors
                        // (because we already added the type name)
                        if (constructorVM == null)
                        {
                            AddName(names1, member.Name);
                        }
                        else
                        {
                            parameters = constructorVM.Parameters;
                        }

                        if (methodVM != null)
                        {
                            parameters = methodVM.Parameters;
                        }

                        // If this is a constructor or a method, add the parameter names
                        if (parameters != null)
                        {
                            foreach (var parameter in parameters)
                            {
                                AddName(names1, parameter.Name);
                            }
                        }
                    }
                }

                names2 = names1.ToArray();

            });

            thread.Priority = ThreadPriority.BelowNormal;
            thread.Start();

            await Task.Run(() =>
            {
                thread.Join();
            });

            AllNames = names2;
        }

        static void AddName(Dictionary<string, string> names, string name)
        {
            if (names.ContainsKey(name))
            {
                return;
            }

            names[name.ToUpper()] = name[0].ToString().ToUpper() + name.Substring(1);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string name = null)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public event EventHandler<object> ReturnedByCalculationCompleted;

        /// <summary>
        /// Calculate the type.ReturnedByAsync properties (off thread)
        /// </summary>
        /// <returns></returns>
        async Task CalculateReturnedByAsync()
        {
            await Task.Run(() =>
            {
                // Walk all the types and find out what types each returns
                // The result is stored in a list on each of those returning types
                foreach (var type in Types)
                {
                    // Look at each member of type and find the types that each 
                    // member outputs (returns, out parameter, etc),
                    // calling the lambda for each one to add a back reference.
                    // For example, say 'type' is Border, we'll add Border.Child to UIElement's ReturnedBy list
                    _ = TypeReferenceHelper.AllMembersWhereForType(
                                            type,
                                            checkOutOnly: true,
                                            (returnedType, member) =>
                                            {
                                                // If the returned type isn't itself, and is in the TypeSet
                                                // (so not a fake type), then 
                                                if (returnedType != type && returnedType.IsInTypes)
                                                {
                                                    returnedType.AddMemberToReturnedBy(member);
                                                }
                                                return true;
                                            }
                                            ).ToList(); // Have to call ToList to get the enumerator to run
                }
            });

            // Back on the UI thread
            // It's now OK to use the ReturnedByAsync property
            ReturnedByCalculated = true;
            ReturnedByCalculationCompleted?.Invoke(this, new EventArgs());
            ReturnedByCalculationCompleted = null;

        }
        public bool ReturnedByCalculated = false;

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

        // A cache of TypeVMs and a list of weak references. The list is necessary becuse you can't
        // enumerate members of a ConditionalWeakTable
        //private ConditionalWeakTable<object, TypeViewModel> _typeVMCache = new ConditionalWeakTable<object, TypeViewModel>();

        Dictionary<object, TypeViewModel> _typeVMCache = new Dictionary<object, TypeViewModel>();
        private List<WeakReference<TypeViewModel>> _typeVMWeakList = new List<WeakReference<TypeViewModel>>();

        //public void AddToCache(string name, TypeViewModel vm, bool checkForDup = false)
        //{
        //    // bugbug: couldn't this have already been added to the cache since we don't take the lock until there?
        //    lock (_typeVMCache)
        //    {
        //        // bugbug: getting dups in .Native builds?
        //        // (Probably because of the above AddToCache() method)
        //        if (checkForDup && _typeVMCache.TryGetValue(name, out var type))
        //        {
        //            return;
        //        }

        //        _typeVMCache.Add(name, vm);
        //        _typeVMWeakList.Add(new WeakReference<TypeViewModel>(vm));
        //    }
        //}

        public TypeViewModel LookupByNameSlowly(string typeName)
        {
            foreach (var typeWeakRef in _typeVMWeakList)
            {
                if (typeWeakRef.TryGetTarget(out var type))
                {
                    if (type.FullName == typeName)
                    {
                        return type;
                    }
                }
            }

            return null;
        }

        object _async = null;
        public TypeViewModel GetFromCacheBase(object t, Func<TypeViewModel> create)
        {
            TypeViewModel vm = null;

            if (t == null)
                return null;

            if (!_typeVMCache.TryGetValue(t, out vm))
            {
                lock (_typeVMCache)
                {
                    if (!_typeVMCache.TryGetValue(t, out vm))
                    {
                        vm = create();

                        _typeVMCache.Add(t, vm);

#if DEBUG
                        var found = _typeVMCache.TryGetValue(t, out var vm2);
                        Debug.Assert(found);
                        Debug.Assert(t.Equals((vm2 as MRTypeViewModel).Type));
#endif
                        // This is so that we can do enumeration
                        _typeVMWeakList.Add(new WeakReference<TypeViewModel>(vm));
                    }
                }
            }

            //if (_async != null)
            //{
            //    var found = _typeVMCache.TryGetValue(_async, out var vm3);
            //    Debug.Assert(found);
            //    Debug.Assert(vm3.PrettyFullName == "Windows.Foundation.IAsyncOperation<TResult>");
            //}

            //if(!t.Equals((vm as MRTypeViewModel).Type))
            //{
            //    _typeVMCache.TryGetValue(t, out var vm4);
            //    Debug.Assert(vm4 == vm);
            //    Debug.WriteLine(vm.ToString());
            //    Debug.WriteLine((vm as MRTypeViewModel).Type);

            //    var list = _typeVMCache.Values
            //                    .Where(type => type.Name.StartsWith("IAsync"))
            //                    .OrderBy((k) => k.Name)
            //                    .Select(type => new
            //                    {
            //                        Type = type,
            //                    }).ToList();
            //    Debug.Assert(false);
            //}

            //var b = t.Equals((vm as MRTypeViewModel).Type);

            return vm;
        }

    }
}
