using CommonLibrary;
using Microsoft.Win32;
using MiddleweightReflection;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;





namespace Tempo
{
    public class MRTypeViewModel : TypeViewModel
    {
        public MrType Type { get; private set; }

        private MRTypeViewModel(MrType type, TypeSet typeSet)
        {
            Type = type;
            TypeSet = typeSet;

            // Pre-populate some caches on the initial background thread
            _ = AcidInfo;
        }

        public override bool IsAssembly => IsInternal;
        public override bool IsInternal => !this.IsPublic; // bugbug: should handle nested case

        public override bool IsFamilyOrAssembly => false;

        public override bool IsFamilyAndAssembly => false;

        public override IList<ParameterViewModel> GetDelegateParameters()
        {
            return this.Type.GetInvokeMethod()
                         .GetParameters()
                         .Select(p => new MRParameterViewModel(p, this, this.TypeSet) as ParameterViewModel)
                         .ToList();
        }

        static public TypeViewModel GetFromCache(MrType type, TypeSet typeSet)
        {
            //// The MRTYpes  aren't guaranteed to be unique, sometimes  new instances are
            //// created for the same type.  So if we cache the MRTypeViewModel we leak.
            //return new MRTypeViewModel(type, typeSet);

            // Bugbug: this needs to be instance based, so there can be independent sets.
            var vm = typeSet.GetFromCacheBase(type, () => new MRTypeViewModel(type, typeSet));
            Debug.Assert(vm.FullName == type.GetFullName());

            return vm;
        }





        public override bool IsIdlSupported
        {
            get
            {
                if (this.IsWindows && GeneratedIdl.IsAvailable)
                {
                    return true;
                }

                return false;
            }
        }


        bool? _isStruct = null;
        public override bool IsStruct
        {
            get
            {
                if (_isStruct == null)
                {
                    var baseType = Type.GetBaseType();
                    if (baseType == null)
                    {
                        return false;
                    }
                    _isStruct = baseType.GetFullName() == "System.ValueType";
                }
                return _isStruct == true;
            }
        }

        readonly string[] _delegateBaseNames = new string[] 
        { 
            "System.Delegate",
            "System.MulticastDelegate" 
        };
        bool? _isDelegate = null;
        public override bool IsDelegate
        {
            get
            {
                if (_isDelegate == null)
                {
                    var baseType = Type.GetBaseType();
                    if (baseType == null)
                    {
                        return false;
                    }


                    var baseName = baseType.GetFullName();
                    _isDelegate = _delegateBaseNames.Contains(baseName);
                    if(_isDelegate == true)
                    {
                        // If this actually _is_ Delegate, it doesn't have an Invoke method, and isn't really a "delegate"
                        _isDelegate = !_delegateBaseNames.Contains(this.FullName);
                    }
                }
                return _isDelegate == true;
            }
        }

        bool? _isEventArgs = null;
        override public bool IsEventArgs
        {
            get
            {
                if (_isEventArgs == null)
                {
                    _isEventArgs = Type.GetFullName().EndsWith("EventArgs");
                }
                return _isEventArgs == true;
            }
        }

        public override bool IsNotPublic
        {
            get
            {
                return Type.Attributes.HasFlag(TypeAttributes.NotPublic);
            }
        }

        public override TypeAttributes Attributes
        {
            get
            {
                return Type.Attributes;
            }
        }

        public override bool IsVoid
        {
            get
            {
                return FullName == "System.Void";
            }
        }

        public override bool IsByRef
        {
            get
            {
                return Type.IsReference;
            }
        }

        public override GenericParameterAttributes GenericParameterAttributes
        {
            get
            {
                // bugbug
                return GenericParameterAttributes.None;
            }
        }

        public override bool IsGenericType
        {
            get
            {
                return Type.GetHasGenericParameters();
            }
        }

        public override bool IsGenericParameter
        {
            get
            {
                return Type.IsGenericParameter;
            }
        }

        bool? _isGenericTypeDefinition = null;
        public override bool IsGenericTypeDefinition
        {
            get
            {
                if (_isGenericTypeDefinition == null)
                {
                    _isGenericTypeDefinition = Type.GetHasGenericParameters();
                }
                return _isGenericTypeDefinition == true;
            }
        }


        public override string Namespace
        {
            get
            {
                var ns = Type.GetNamespace();
                if (string.IsNullOrEmpty(ns))
                {
                    // Confusing in too many places to not have a namespace,
                    // so follow C# syntax
                    return "global";
                }
                else
                {
                    return ns;
                }
            }
        }

        public override bool IsInterface => Type.IsInterface;

        public override Assembly Assembly => null;
        public override string AssemblyLocation => this.Type.AssemblyLocation;

        public override bool IsEnum
        {
            get
            {
                var baseType = Type.GetBaseType();
                if (baseType == null)
                {
                    return false;
                }
                return baseType.GetFullName() == "System.Enum";
            }
        }

        public override bool IsValueType => !Type.IsClass && !Type.IsInterface;

        public override bool IsClass => Type.IsClass;

        string _fullName;
        public override string FullName
        {
            get
            {
                if (_fullName == null)
                {
                    _fullName = Type.GetFullName();
                }

                return _fullName;
            }
        }

        public override bool IsProtected => false; // Types are either public or internal or private

        public override bool IsSealed => Type.IsSealed;

        public override TypeViewModel ReturnType => null;

        public override bool IsStatic => Type.IsStatic;

        public override bool IsPublic => Type.IsPublic;

        public override bool IsVirtual => false;

        public override bool IsAbstract => Type.IsAbstract;

        string _name;
        public override string Name
        {
            get
            {
                if (_name == null)
                {
                    _name = Type.GetName();
                }
                return _name;
            }
        }

        public override bool IsFamily => Type.IsFamily;

        public override MyMemberTypes MemberType => MyMemberTypes.TypeInfo; // Bugbug: this right?

        public override MemberKind MemberKind => MemberKind.Type; // bugbug: ??

        public override IList<TypeViewModel> CalculateInterfacesFromType(bool includeInternal = false)
        {
            // bugbug: why both Settings.InternalInterfaces and includeInternal?
            return (from iface in this.Type.GetInterfaces()
                    where iface != null 
                    where iface.IsPublic || Manager.Settings.InternalInterfaces || includeInternal
                    select MRTypeViewModel.GetFromCache(iface, this.TypeSet))
             .ToList();
        }

        IEnumerable<TypeViewModel> _allInterfaces = null;
        public override IEnumerable<TypeViewModel> GetAllInterfaces()
        {
            if (_allInterfaces == null)
            {
                var allInterfaces = from iface in Type.GetInterfaces()
                                    where iface != null // bugbug, try loading all of .net 8
                                    select GetFromCache(iface, TypeSet);
                _allInterfaces = allInterfaces.Union(GetStaticInterfaces());
            }
            return _allInterfaces;
        }

        public override IEnumerable<TypeViewModel> GetConstructorInterfaces()
        {
            return GetConstructorInterfacesFromAttributes(this.CustomAttributes);
        }

        static public IEnumerable<TypeViewModel> GetConstructorInterfacesFromAttributes(IEnumerable<CustomAttributeViewModel> attributes)
        {
            if (attributes == null)
                yield break;

            foreach (var attr in attributes)
            {
                if (attr.Name == "ActivatableAttribute")
                {
                    var args = attr.ConstructorArguments;
                    if (args != null && args.Count() > 0)
                    {
                        var i = args[0].Value as TypeViewModel;
                        if (i == null)
                        {
                            // No interface type is specified, should be returning
                            // IActivationFactory here? (bugbug)
                            continue;
                        }
                        else
                            yield return i;
                    }
                }
            }
        }


        TypeViewModel[] _genericArguments = null;
        public override TypeViewModel[] GetGenericArguments()
        {
            if (_genericArguments == null)
            {
                var mrTypes = Type.GetGenericArguments();

                _genericArguments = mrTypes
                                    .Select(a => GetFromCache(a, TypeSet))
                                    .ToArray<TypeViewModel>();
            }
            return _genericArguments;
        }

        // AcidInfo is info based on the Activation Class ID
        AcidInfo _acidInfo = null;
        public override AcidInfo AcidInfo
        {
            get
            {
                if (_acidInfo == null)
                {
                    _acidInfo = GetAcidInfo(this);
                }
                return _acidInfo;
            }
        }


        public override string DllPath => AcidInfo.DllPath;
        public override string TrustLevel => AcidInfo.TrustLevel;
        public override string Threading => AcidInfo.Threading;
        public override string ActivationType => AcidInfo.ActivationType;


        static public AcidInfo GetAcidInfo(TypeViewModel type)
        {
            AcidInfo acidInfo = null;

            if (!AcidInfo.AcidMap.TryGetValue(type, out acidInfo))
            {
                acidInfo = new AcidInfo();
                var key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\WindowsRuntime\ActivatableClassId\" + type.PrettyFullName,
                    writable: false);

                if (key != null)
                {
                    acidInfo.DllPath = key.GetValue("DllPath", "") as string;

                    var trustLevelKeyValue = key.GetValue("TrustLevel");
                    if (trustLevelKeyValue == null)
                    {
                        acidInfo.TrustLevelValue = Tempo.TrustLevel.Unset;
                        acidInfo.TrustLevel = "Unset";
                    }
                    else
                    {
                        acidInfo.TrustLevelValue = (Tempo.TrustLevel)trustLevelKeyValue;
                        switch (acidInfo.TrustLevelValue)
                        {
                            case Tempo.TrustLevel.Base:
                                acidInfo.TrustLevel = "Base";
                                break;
                            case Tempo.TrustLevel.Partial:
                                acidInfo.TrustLevel = "Partial";
                                break;
                            case Tempo.TrustLevel.Full:
                                acidInfo.TrustLevel = "Full";
                                break;
                            default:
                                acidInfo.TrustLevel = String.Format("0x{0:X}", acidInfo.TrustLevelValue);
                                break;
                        }
                    }

                    var clsid = (string)key.GetValue("CLSID", "");
                    acidInfo.Clsid = clsid;

                    var value = key.GetValue("Activationtype", "");
                    if (value is int)
                        acidInfo.ActivationType = String.Format("0x{0:X}", (int)value);
                    else if (value is string)
                        acidInfo.ActivationType = value as string;

                    value = key.GetValue("Threading", "");
                    if (value is int)
                        acidInfo.Threading = String.Format("0x{0:X}", ((int)key.GetValue("Threading", "")));
                    else if (value is string)
                        acidInfo.ActivationType = value as string;
                }

                AcidInfo.AcidMap[type] = acidInfo;
            }

            return acidInfo;

        }

        public override TypeViewModel GetGenericTypeDefinition()
        {
            if (!IsGenericType)
            {
                return this;
            }

            var mrType = Type.Assembly.LoadContext.GetType(Type.GetFullName());
            var type = GetFromCache(mrType, TypeSet);
            return type;
        }

        public override TypeViewModel GetInterface(string name)
        {
            var ifaces = GetAllInterfaces();
            return ifaces.FirstOrDefault(iface => iface.FullName == name);
        }

        protected override IList<EventViewModel> CalculateEventsFromTypeOverride(bool shouldFlatten)
        {
            // bugbug: what is shouldFlatten?

            return Type.GetEvents()
                .Select(p => new MREventViewModel(this, p) as EventViewModel)
                .OrderBy(p => p.Name)
                .ToList(); //bugbug: make this an array
        }

        protected override IList<FieldViewModel> CalculateFieldsFromTypeOverride(bool shouldFlatten)
        {
            return Type.GetFields()
                .Where(f => !f.IsSpecialName)
                .Select(f => new MRFieldViewModel(f, this) as FieldViewModel)
                .OrderBy(f => f.Name)
                .ToList(); //bugbug: make this an array
        }

        protected override void OnSettingsInternalChanged()
        {
            base.OnSettingsInternalChanged();
            _constructors = null;
            _methods = null;
        }

        public override TypeViewModel UnderlyingEnumType
        {
            get
            {
                if (!IsEnum)
                    return null;

                return GetFromCache(this.Type.GetUnderlyingEnumType(), this.TypeSet);
            }
        }

        IList<MethodViewModel> _methods;
        IList<ConstructorViewModel> _constructors;
        protected override IList<MethodViewModel> CalculateMethodsFromTypeOveride(bool shouldFlatten)
        {
            EnsureMethodsAndConstructors();
            return _methods;
        }

        void EnsureMethodsAndConstructors()
        {
            if (_methods == null)
            {
                Type.GetMethodsAndConstructors(out var methods, out var constructors);

                _methods = methods.Select(m => new MRMethodViewModel(this, m) as MethodViewModel)
                                  .OrderBy(m => m.Name)
                                  .ThenBy(m => m.Parameters.Count)
                                  .ToList(); //bugbug: make this an array

                _constructors = constructors.Select(c => new MRConstructorViewModel(c, this, TypeSet) as ConstructorViewModel)
                                            .OrderBy(c => c.Parameters.Count)
                                            .ToList(); //bugbug: make this an array

            }
        }

        protected override IList<ConstructorViewModel> CalculateConstructorsFromTypeOverride()
        {
            EnsureMethodsAndConstructors();
            return _constructors;
        }



        protected override IList<PropertyViewModel> CalculatePropertiesFromTypeOverride(bool shouldFlatten)
        {
            return Type.GetProperties()
                .Select(p => new MRPropertyViewModel(this, p) as PropertyViewModel)
                .OrderBy(p => p.Name)
                .ToList(); //bugbug: make this an array
        }

        protected override IList<CustomAttributeViewModel> CreateAttributes()
        {
            return this.Type.GetCustomAttributes()
                            .Select(a => new MRCustomAttributeViewModel(a, this) as CustomAttributeViewModel)
                            .ToList();
        }

        SdkPlatform? _sdkPlatform = null;
        public override SdkPlatform SdkPlatform
        {
            get
            {
                if (_sdkPlatform == null)
                {
                    CalculateSdkPlatform();
                }

                return _sdkPlatform.Value;
            }
        }

        void CalculateSdkPlatform()
        {
            _sdkPlatform = SdkPlatform.Any;

            //// If a type is in UAP and some other platform (i.e. Teams), assume UAP
            //var contract = this.Contract;
            //if (Tempo.ContractInformation.ContractsPerPlatform[SdkPlatform.Universal].Contains(contract))
            //{
            //    _sdkPlatform = SdkPlatform.Universal;
            //    return;
            //}

            //foreach (var platformContracts in Tempo.ContractInformation.ContractsPerPlatform)
            //{
            //    if(platformContracts.Key == SdkPlatform.Universal)
            //    {
            //        continue;
            //    }

            //    if (platformContracts.Value.Contains(contract))
            //    {
            //        _sdkPlatform = platformContracts.Key;
            //        return;
            //    }
            //}

            //_sdkPlatform = SdkPlatform.Unknown;
        }


        public override string ToWhereString()
        {
            return PrettyName;
        }

        public override bool TryGetVMProperty(string key, out object value)
        {
            return TryGetVMPropertyHelper(this, key, out value);
        }


        static public bool TryGetVMPropertyHelper(object declaringObject, string key, out object value)
        {
            value = null;

            if (declaringObject == null)
            {
                return false;
            }

            var parts = key.Split('.');
            if (parts.Length == 0)
                return false;

            foreach (var p in parts)
            {
                var part = p;

                part = NormalizePropertyNameForQueries(part);

                var propInfo = declaringObject.GetType().MyGetPropertyCaseInsensitive(part);
                if (propInfo == null)
                {
                    return false;
                }

                // If this is the last iteration through the loop we'll return `value`
                // Otherwise we'll use it in the next iteration
                value = propInfo.GetValue(declaringObject);
                declaringObject = value;

            }

            return true;
        }

        protected override TypeViewModel GetBaseType()
        {
            var baseMrType = Type.GetBaseType();
            if (baseMrType == null)
            {
                return null;
            }

            var baseTypeVM = GetFromCache(baseMrType, TypeSet);
            var baseFullName = baseTypeVM.FullName;

            if (baseFullName == "System.Object"
                || baseFullName == "System.ValueType"
                || baseFullName == "System.Enum"
                || baseFullName == "System.Attribute"
                || baseFullName == "System.Delegate"
                || baseFullName == "System.MulticastDelegate")
            {
                return null;
            }

            return baseTypeVM;
        }



        // bugbug: This is duplicated in ReflectionTypeViewModel
        MyAsyncOperation InitializeIdlAsync(bool sync)
        {

            if (sync)
            {
                GeneratedIdl.EnsureInitialized();
                return null;
            }
            else
            {
                var asyncOp = new MyAsyncOperation();
                BackgroundHelper.DoWorkAsyncOld(
                    () =>
                    {
                        GeneratedIdl.EnsureInitialized();
                    },
                    () =>
                    {
                        this.RaisePropertyChanged("Idl");
                        asyncOp.Complete();
                    });

                return asyncOp;
            }
        }

        // bugbug: This is duplicated in ReflectionTypeViewModel
        static bool _isIdlEnabled = false;
        override public MyAsyncOperation EnableIdlAsync(bool sync)
        {
            if (_isIdlEnabled)
                return null;

            _isIdlEnabled = true;
            var asyncOp = InitializeIdlAsync(sync);

            return asyncOp;
        }

        // bugbug: This is duplicated in ReflectionTypeViewModel
        string _idl = null;
        bool _idlChecked = false;
        public string Idl
        {
            get
            {
                if (!_isIdlEnabled || !GeneratedIdl.Initialized)
                    return GeneratedIdl.StatusMessage;

                if (!_idlChecked)
                {
                    _idlChecked = true;
                    _idl = GeneratedIdl.Get(this);
                }

                return _idl;
            }
        }

    }
}
