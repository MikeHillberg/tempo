using CommonLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
//using System.Windows.Threading;

namespace Tempo
{
    // Windows.Foundation.Metadata.MarshalingType
    public enum TempoMarshalingType
    {
        InvalidMarshaling = 0,
        None = 1,
        Agile = 2,
        Standard = 3,

        Unspecified = -1 // Not part of MarshalingType
    }

    abstract public class TypeViewModel : MemberOrTypeViewModelBase
    {

        public override bool IsProtected => false; // Only members have protected access

        public virtual string DllPath => null;
        public virtual string TrustLevel => null;
        public virtual string Threading => null;
        public virtual string ActivationType => null;



        public bool HasProperties { get { return Properties != null && Properties.Count != 0; } }
        public bool HasMethods { get { return Methods != null && Methods.Count != 0; } }
        public bool HasEvents { get { return Events != null && Events.Count != 0; } }

        bool? _isFullyOpenGenericType = null;
        public bool IsFullyOpenGenericType
        {
            get
            {
                if (_isFullyOpenGenericType == null)
                {
                    _isFullyOpenGenericType = true;

                    if (IsGenericType)
                    {
                        var parms = GetGenericArguments();
                        foreach (var parm in parms)
                        {
                            if (!parm.IsGenericParameter)
                            {
                                _isFullyOpenGenericType = false;
                                break;
                            }
                        }
                    }
                }

                return _isFullyOpenGenericType == true;
            }
        }

        public bool IsInCurrentTypeSet
        {
            get
            {
                var t = this;

                if (t.IsGenericType)
                {
                    t = this.GetGenericTypeDefinition();
                }

                return Manager.CurrentTypeSet.Types.Contains(t);
            }
        }

        internal void SetIsFullyOpenGenericType(string v)
        {
            _isFullyOpenGenericType = bool.Parse(v);
        }

        public bool IsArray { get { return BaseType != null && BaseType.FullName == "System.Array"; } }

        public override string MsdnRelativePath
        {
            get
            {
                return FullName;
            }
        }

        IList<TypeViewModel> _referencedBy = null;
        public IList<TypeViewModel> ReferencedByAsync
        {
            get
            {
                if (_referencedBy == null)
                {
                    UpdateReferendedByAsync();
                    return null;
                }

                return _referencedBy;
            }
        }
        async void UpdateReferendedByAsync()
        {
            int referenceIndex = ++Manager._referenceIndex;
            IList<TypeViewModel> types = null;

            types = await BackgroundHelper2.DoWorkAsync(() =>
            {
                return TypeReferenceHelper.FindReferencingTypes(this, referenceIndex).ToList();
            });


            if (_referencedBy == null && referenceIndex == Manager._referenceIndex)
            {
                _referencedBy = types;

                RaisePropertyChanged("ReferencedByAsync");
            }
        }

        IList<MemberOrTypeViewModelBase> _returnedBy = new List<MemberOrTypeViewModelBase>();

        /// <summary>
        /// List of members that output instances of this type
        /// </summary>
        public IList<MemberOrTypeViewModelBase> ReturnedByAsync
        {
            get
            {
                if (!TypeSet.ReturnedByCalculated)
                {
                    // _returnedBy hasn't been calculated yet. When it is, raise INPC
                    TypeSet.ReturnedByCalculationCompleted += (s, e) =>
                    {
                        // This event is raised off thread
                        Manager.PostToUIThread(() => RaisePropertyChanged(nameof(ReturnedByAsync)));
                    };

                    return null;
                }

                return _returnedBy;
            }
        }

        // This is called by TypeSet when it finds a member that outputs this type
        public void AddMemberToReturnedBy(MemberOrTypeViewModelBase m)
        {
            _returnedBy.Add(m);
        }

        public bool IsDotNetType
        {
            get
            {
                return Assembly == typeof(Object).GetTypeInfo().Assembly;
            }
        }

        public void Serialize(ExportContext context)
        {
            context.WriteTypeEntry(ExportIndex);
            context.WriteTypeEntry(Namespace);

            context.WriteTypeEntry(Name);

            context.WriteTypeEntry(this.Version);
            context.WriteTypeEntry(this.Contract);

            context.WriteTypeEntry(this.BaseType == null ? -1 : this.BaseType.ExportIndex);
            context.WriteTypeEntry(this.Interfaces);

        }

        public bool HasFields { get { return Fields != null && Fields.Count != 0; } }


        GroupedTypeMembers _groupedMembers = null;
        public GroupedTypeMembers GroupedMembers
        {
            get
            {
                if (_groupedMembers == null)
                {
                    _groupedMembers = new GroupedTypeMembers();

                    var properties = from p in Properties where !p.IsDependencyPropertyField select p;
                    if (properties.Count() != 0)
                    {
                        _groupedMembers.Add(
                            new MemberList(properties)
                            {
                                Heading = "Properties",
                            });
                    }

                    if (Methods.Count != 0)
                    {
                        _groupedMembers.Add(
                            new MemberList(Methods)
                            {
                                Heading = "Methods",
                            });
                    }

                    if (Events.Count != 0)
                    {
                        _groupedMembers.Add(
                            new MemberList(Events)
                            {
                                Heading = "Events",
                            });
                    }

                    if (Constructors.Count != 0)
                    {
                        _groupedMembers.Add(
                            new MemberList(Constructors)
                            {
                                Heading = "Constructors",
                            });
                    }

                    if (Fields.Count != 0)
                    {
                        _groupedMembers.Add(
                            new MemberList(Fields)
                            {
                                Heading = "Fields",
                            });
                    }

                    var dps = from p in Properties where p.IsDependencyPropertyField select p;
                    if (dps.Count() != 0)
                    {
                        _groupedMembers.Add(
                            new MemberList(dps)
                            {
                                Heading = "Dependency Properties",
                            });
                    }

                }

                return _groupedMembers;

            }
        }

        virtual public IList<ParameterViewModel> GetDelegateParameters()
        {
            if (IsDelegate)
            {
                return Methods[0].Parameters;
            }
            else
            {
                return new List<ParameterViewModel>();
            }
        }

        virtual public MethodViewModel DelegateInvoker
        {
            get
            {
                if (IsDelegate && Methods.Count != 0) // Count == 0 for System.Delegate itself
                {
                    return Methods[0];
                }

                return null;
            }
        }

        string _csName = null;
        public string CSharpName
        {
            get
            {
                if (_csName == null)
                {
                    _csName = PrettyName;
                    string fullName = FullName;

                    bool isNullable = Name.StartsWith("IReference`1");
                    if (isNullable)
                    {
                        _csName = this.GetGenericArguments()[0].Name;
                    }

                    bool isReference = Name.EndsWith("&");
                    if (isReference)
                    {
                        fullName = fullName.Substring(0, fullName.LastIndexOf('&'));
                        _csName = Name.Substring(0, Name.LastIndexOf('&'));
                    }

                    if (fullName == typeof(string).FullName)
                        _csName = "string";
                    else if (fullName == typeof(int).FullName)
                        _csName = "int";
                    else if (fullName == typeof(uint).FullName)
                        _csName = "uint";
                    else if (fullName == typeof(long).FullName)
                        _csName = "long";
                    else if (fullName == typeof(ulong).FullName)
                        _csName = "ulong";
                    else if (fullName == typeof(bool).FullName)
                        _csName = "bool";
                    else if (fullName == typeof(void).FullName)
                        _csName = "void";
                    else if (fullName == typeof(float).FullName)
                        _csName = "float";
                    else if (fullName == typeof(void).FullName)
                        _csName = "void";

                    if (isNullable)
                    {
                        _csName = $"{_csName}?";
                    }

                    if (isReference)
                    {
                        _csName = _csName + "&";
                    }
                }

                return _csName;
            }
        }




        public override string ToString()
        {
            //return "TempoType: " + this.PrettyName;

            // Query depends on name
            return PrettyName;
        }


        TempoMarshalingType? _marshalingType = null;
        public virtual TempoMarshalingType MarshalingType
        {
            get
            {
                if (_marshalingType == null)
                {
                    _marshalingType = TempoMarshalingType.Unspecified;

                    // bugbug
                    if (TypeViewModel.IsPhoneContractHack(this))
                        return (TempoMarshalingType)_marshalingType;

                    var attrs = this.CustomAttributes;
                    if (attrs != null)
                    {
                        foreach (var attr in attrs)
                        {
                            var attributeClassName = attr.Name;
                            if (attributeClassName != "MarshalingBehaviorAttribute")
                                continue;

                            // There won't be any arguments if the type is being faked by MR
                            if (attr.ConstructorArguments.Count > 0)
                            {
                                var argument = attr.ConstructorArguments[0];
                                _marshalingType = (TempoMarshalingType)argument.Value;
                            }
                        }
                    }
                }

                return (TempoMarshalingType)_marshalingType;
            }
        }


        public virtual MyAsyncOperation EnableIdlAsync(bool sync) { return null; }


        bool? _isActivatable = null;
        public bool IsActivatable
        {
            get
            {
                if (_isActivatable == null)
                {
                    _isActivatable = false;

                    // bugbug
                    if (TypeViewModel.IsPhoneContractHack(this))
                    {
                        // guess
                        if (this.IsClass)
                        {
                            _isActivatable = true;
                            return true;
                        }
                        else
                            return false;
                    }

                    if (CustomAttributes != null)
                    {
                        foreach (var a in CustomAttributes)
                        {
                            if (a.Name == "ActivatableAttribute")
                            {
                                _isActivatable = true;
                                break;
                            }
                            else if (a.Name == "ComposableAttribute" && Constructors != null)
                            {
                                foreach (var c in Constructors)
                                {
                                    if (c.IsPublic)
                                    {
                                        _isActivatable = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }

                return _isActivatable == true;

            }
        }

        int? _threadingModelValue = null;

        public int ThreadingModelValue
        {
            get
            {
                if (_threadingModelValue == null)
                {
                    _threadingModelValue = -1; // "(Unspecified)"

                    // bugbug
                    if (IsPhoneContractHack(this))
                        return (int)_threadingModelValue;

                    if (CustomAttributes != null)
                    {
                        foreach (var attr in CustomAttributes)
                        {
                            if (attr.Name == "ThreadingAttribute" && attr.ConstructorArguments.Count > 0)
                            {
                                var argValue = attr.ConstructorArguments[0].Value;
                                if (argValue is uint?)
                                {
                                    _threadingModelValue = (int)(uint)argValue;
                                }
                                else
                                {
                                    _threadingModelValue = (int)attr.ConstructorArguments[0].Value;
                                }
                            }
                        }
                    }

                }

                return (int)_threadingModelValue;
            }
        }



        bool? _isWebHostHidden = null;
        public bool IsWebHostHidden
        {
            get
            {
                if (_isWebHostHidden == null)
                {
                    _isWebHostHidden = false;

                    if (IsPhoneContractHack(this))
                        return false;


                    if (CustomAttributes != null)
                    {
                        foreach (var attr in CustomAttributes)
                        {
                            if (attr.FullName == "Windows.Foundation.Metadata.WebHostHiddenAttribute")
                            {
                                _isWebHostHidden = true;
                                break;
                            }
                        }
                    }
                }

                return _isWebHostHidden == true;
            }
        }

        bool? _isMuse = null;
        public bool IsMuse
        {
            get
            {
                if (_isMuse == null)
                {
                    // bugbug
                    if (IsPhoneContractHack(this))
                        return false;

                    if (CustomAttributes != null)
                    {
                        foreach (var attr in CustomAttributes)
                        {
                            var attributeClassName = attr.Name;// attr.Constructor.DeclaringType.Name;
                            if (attributeClassName == "MuseAttribute")
                            {
                                _isMuse = true;
                                break;
                            }
                        }
                    }
                }

                return _isMuse == true;
            }
        }



        bool? _isDualApi = null;
        public bool IsDualApi
        {
            get
            {
                if (_isDualApi == null)
                {

                    // Bugbug: Remove when winmd is fixed (or below will throw)
                    if (Name == "IPhoneCallBlockedTriggerDetails"
                        || Name == "IPhoneCallOriginDataRequestTriggerDetails"
                        || Name == "IPhoneLineChangedTriggerDetails"
                        || Name == "PhoneCallBlockedReason")
                    {
                        if (Namespace == "Windows.ApplicationModel.Calls.Background")
                        {
                            _isDualApi = false;
                            return false;
                        }
                    }


                    _isDualApi = false;
                    var attrs = this.CustomAttributes;// CustomAttributesData;

                    if (attrs != null)
                    {
                        foreach (var attr in attrs)
                        {
                            var attributeClassName = attr.Name;// attr.Constructor.DeclaringType.Name;
                            if (attributeClassName == "DualApiPartitionAttribute")
                            {
                                _isDualApi = true;
                                break;
                            }
                        }
                    }
                }

                return _isDualApi == true;
            }
        }



        bool? _isMutable = null;
        public bool IsMutable
        {
            get
            {
                if (_isMutable == null)
                {
                    if (IsValueType
                        || FullName == "System.String"
                        || FullName == "System.Uri"
                        || FullName == "Windows.Storage.IStorageFile") // bugbug: huh?
                    {
                        _isMutable = false;
                    }

                    if (_isMutable == null && Methods != null && Methods.Count != 0)
                    {
                        foreach (var method in Methods)
                        {
                            if (!method.IsStatic)
                            {
                                _isMutable = true;
                                break;
                            }
                        }
                    }

                    if (_isMutable == null && Properties != null && Properties.Count != 0)
                    {
                        foreach (var prop in Properties)
                        {
                            if (!prop.IsStatic && prop.CanWrite)
                            {
                                _isMutable = true;
                                break;
                            }
                        }
                    }

                    if (_isMutable == null)
                        _isMutable = false;
                }

                return _isMutable == true;
            }
        }


        public bool IsAttribute
        {
            get
            {
                if (CustomAttributes != null)
                {
                    foreach (var attribute in CustomAttributes)
                    {
                        if (attribute.Name == "AttributeUsageAttribute")
                            return true;
                    }
                }

                return false;
            }
        }


        bool? _isUac = null;
        public override bool IsUac
        {
            get
            {
                if (_isUac == null)
                {
                    _isUac = Contract.StartsWith("UniversalApiContract,") || Contract == "UniversalApiContract";
                }

                return _isUac == true;
            }
        }

        // UAP, Desktop, etc
        public virtual SdkPlatform SdkPlatform => SdkPlatform.Unknown;

        public static bool IsPhoneContractHack(TypeViewModel type)
        {
            // Bugbug: Remove when winmd is fixed (or below will throw)
            if ((type.Name == "IPhoneCallBlockedTriggerDetails"
                    || type.Name == "PhoneCallBlockedTriggerDetails"
                    || type.Name == "IPhoneCallOriginDataRequestTriggerDetails"
                    || type.Name == "PhoneCallOriginDataRequestTriggerDetails"
                    || type.Name == "IPhoneLineChangedTriggerDetails"
                    || type.Name == "PhoneLineChangedTriggerDetails"
                    || type.Name == "PhoneCallBlockedTriggerDetails"
                    || type.Name == "PhoneCallBlockedReason"
                    || type.Name == "PhoneLineChangeKind"
                    || type.Name == "PhoneLineProperties"
                    || type.Name == "PhoneTrigger"
                    || type.Name == "IPhoneTrigger"
                    || type.Name == "PhoneTriggerType"
                    || type.Name == "IPhoneNewVoicemailMessageTriggerDetails"
                    || type.Name == "PhoneNewVoicemailMessageTriggerDetails")
                  && type.Namespace == "Windows.ApplicationModel.Calls.Background"

                 ||
                 (type.Name == "IPhoneTrigger"
                    || type.Name == "PhoneTrigger"
                    || type.Name == "IPhoneTriggerFactory")
                  && type.Namespace == "Windows.ApplicationModel.Background"
                  )
            {
                return true;
            }

            return false;

        }



        static int _contractCount = 0;

        string _contract = null;
        override public string Contract
        {
            get
            {
                if (_contract == null)
                {
                    ++_contractCount;

                    // Bugbug: Remove when winmd is fixed (or below will throw)
                    if (IsPhoneContractHack(this))
                    {
                        return _contract = "CallsBackgroundContract, 1";
                    }

                    _contract = GetContractFromHacks();

                    if (_contract == null)
                    {
                        //_contract = GetContractFromAttributesWithNetReflectionHack(CustomAttributes);
                        _contract = GetContractFromAttributes(CustomAttributes);
                    }

                }

                return _contract;

            }
        }

        // Return a Guid if this type has a [Guid] attribute set
        // String.Empty otherwise
        string _guid = null;
        public string Guid
        {
            get
            {
                if (_guid == null)
                {
                    _guid = string.Empty;

                    foreach (var attr in CustomAttributes)
                    {
                        _guid = attr.TryParseGuidAttribute();
                        if (_guid != string.Empty)
                        {
                            break;
                        }
                    }
                }

                return _guid;
            }
        }

        public string ContractName
        {
            get
            {
                var contract = this.Contract;
                if (string.IsNullOrEmpty(contract))
                {
                    return null;
                }

                var parts = contract.Split(',');
                if (parts.Length == 2)
                {
                    return parts[0];
                }
                else
                {
                    return null;
                }
            }
        }

        protected virtual string GetContractFromHacks()
        {
            return null;
        }


        // .Net Reflection hides the Contract attribute for some reason. It's in there, though, it's just
        // not getting exposed by the public API. This a hack to dig it out.
        static public string GetContractFromAttributesWithNetReflectionHack(IEnumerable<CustomAttributeViewModel> attributes)
        {
            string contract = "";

            if (attributes != null)
            {
                foreach (var attr in attributes)
                {
                    if (attr.Name == "ContractVersionAttribute" && attr.ConstructorArguments != null)
                    {
                        if (attr.ConstructorArguments.Count > 0)
                        {
                            var value = attr.ConstructorArguments[0].Value;
                            if (value is string)
                            {
                                contract = value as string;
                                var offset = contract.LastIndexOf('.');
                                contract = contract.Substring(offset + 1);

                                // Pull out the version too
                                contract = GetContractWithVersion(contract, attr);
                            }
                            else if (value is Type)
                                contract = (value as Type).Name;
                            else if (value is TypeViewModel)
                            {
                                contract = (value as TypeViewModel).Name;
                                contract = GetContractWithVersion(contract, attr);
                            }

                            if (contract == null)
                            {
                                contract = String.Empty;
                            }
                        }
                    }
                }
            }

            return contract;
        }

        private static string GetContractWithVersion(string contract, CustomAttributeViewModel attr)
        {
            if (attr.ConstructorArguments.Count <= 1)
                return contract;

            int ver;
            if (attr.ConstructorArguments[1].Value is int) // desktop
                ver = (int)attr.ConstructorArguments[1].Value;
            else if (attr.ConstructorArguments[1].Value is uint) // GetTypeInfo
                ver = (int)(uint)attr.ConstructorArguments[1].Value;
            else
                return contract;

            var major = (UInt16)(ver / 0x10000);
            var minor = (UInt16)ver;

            contract = contract + ", " + major.ToString();

            if (minor != 0)
                contract = contract + ", " + major.ToString() + "." + minor.ToString();

            return contract;
        }

        public int TotalMembers
        {
            get
            {
                int count = 0;
                var type = this;
                while (type != null)
                {
                    if (!type.ShouldIgnore)
                    {
                        count += type.Properties.Count + type.Methods.Count + type.Events.Count + type.Fields.Count + type.Constructors.Count
                            ;
                    }

                    type = type.BaseType;
                }

                return count;
            }
        }


        public ExportTypeViewModelFlags GetFlags()
        {
            ExportTypeViewModelFlags flags = 0;

            if (IsStruct) flags |= ExportTypeViewModelFlags.Struct;
            if (IsDelegate) flags |= ExportTypeViewModelFlags.Delegate;
            if (IsNotPublic) flags |= ExportTypeViewModelFlags.NotPublic;
            if (IsVoid) flags |= ExportTypeViewModelFlags.Void;
            if (IsByRef) flags |= ExportTypeViewModelFlags.ByRef;
            if (IsGenericType) flags |= ExportTypeViewModelFlags.GenericType;
            if (IsGenericParameter) flags |= ExportTypeViewModelFlags.GenericParameter;
            if (IsGenericTypeDefinition) flags |= ExportTypeViewModelFlags.GenericTypeDefinition;
            if (IsClass) flags |= ExportTypeViewModelFlags.Class;
            if (IsInterface) flags |= ExportTypeViewModelFlags.Interface;
            if (IsEnum) flags |= ExportTypeViewModelFlags.Enum;
            if (IsValueType) flags |= ExportTypeViewModelFlags.ValueType;
            if (IsSealed) flags |= ExportTypeViewModelFlags.Sealed;
            if (IsStatic) flags |= ExportTypeViewModelFlags.Static;
            //if( IsVirtual ) flags |= ExportTypeViewModelFlags.IsVirtual;
            if (IsAbstract) flags |= ExportTypeViewModelFlags.Abstract;
            //if( IsFamily ) flags |= ExportTypeViewModelFlags.IsFamily;   bugbug

            return flags;
        }

        string _prettyName = null;
        override public string PrettyName
        {
            get
            {
                if (_prettyName == null)
                {
                    var sb = new StringBuilder();
                    GenerateTypeName(this, sb, true);
                    _prettyName = sb.ToString();
                }

                return _prettyName;
            }
        }

        string _xamFormsUrlName = null;
        public string XamFormsUrlName
        {
            get
            {
                if (_xamFormsUrlName == null)
                {
                    var sb = new StringBuilder();
                    GenerateTypeName(this, sb, true, true);
                    sb.Replace("<", "%7B");
                    sb.Replace(">", "%7D");
                    _xamFormsUrlName = sb.ToString();
                }

                return _xamFormsUrlName;


            }
        }

        public string PrettyFullName
        {
            get
            {
                return Namespace + "." + PrettyName;
            }
        }

        public string GenericTypeName
        {
            get
            {
                if (IsGenericType)
                    return Name.Split(new char[] { '`' })[0];
                else
                    return null;
            }
        }

        static public void GenerateTypeName(TypeViewModel type, StringBuilder sb, bool first,
            bool fullNames = false)
        {
            string typeNameBase;
            if (type.IsGenericType)
                typeNameBase = type.GenericTypeName;// type.Name.Split(new char[] { '`' })[0];
            else
                typeNameBase = type.Name;


            if (!first)
                sb.Append(",");

            if (fullNames)
                sb.Append($"{type.Namespace}.{typeNameBase}");
            else
                sb.Append(typeNameBase);

            if (!type.IsGenericType)
                return;

            sb.Append("<");

            first = true;
            foreach (var ta in type.GetGenericArguments())
            {
                // Don't do full names for type parameters (e.g. "T")
                GenerateTypeName(ta, sb, first, fullNames && !ta.IsGenericParameter);
                first = false;
            }

            sb.Append(">");
        }



        //void Foo()
        //{
        //    var parameters = Invoker.Parameters;
        //    if (parameters.Count < 2)
        //        return null;
        //    _argsType = GetTypeFromCache(parameters[1].ParameterType);

        //}

        public IList<ParameterViewModel> DelegateParameters
        {
            get
            {
                if (!IsDelegate)
                    return null;

                return this.GetDelegateParameters();
            }
        }


        public string CodeColon
        {
            get
            {
                if (CodeBaseType != null || HasInterfaces)
                    return " : ";
                else
                    return "";
            }
        }

        public TypeViewModel CodeBaseType
        {
            get
            {
                if (BaseType != null && !BaseType.ShouldIgnore)
                    return BaseType;
                else if (IsEnum)
                {
                    // 32 bit is the default, otherwise show the underlying type as the base type
                    var underlying = this.UnderlyingEnumType;
                    if (underlying.FullName == "Int32" || underlying.FullName == "UInt32")
                        return null;
                    else
                        return underlying;
                }
                else
                    return null;
            }
        }

        public bool HasInterfaces
        {
            get
            {
                return this.Interfaces != null && this.Interfaces.Count != 0;
            }
        }

        public string CodeModifiers
        {
            get
            {
                var sb = new StringBuilder();

                if (IsPublic)
                    sb.Append("public ");
                else if (IsInternal)
                    sb.Append("internal ");
                else
                    sb.Append("private ");

                if (IsStatic)
                    sb.Append("static ");

                if (IsSealed && IsClass && !IsStatic)
                    sb.Append("sealed ");

                sb.Append(this.TypeKind.ToString().ToLower() + " ");

                return sb.ToString();
            }
        }

        public TypeKind TypeKind
        {
            get
            {
                if (IsEnum)
                    return TypeKind.Enum;
                else if (IsClass)
                    return TypeKind.Class;
                else if (IsStruct)
                    return TypeKind.Struct;
                else if (IsInterface)
                    return TypeKind.Interface;
                else
                    return TypeKind.Any; // int or bool or some such basic type
            }
        }
        public string TypeKindString
        {
            get { return TypeKind.ToString().ToLower(); }
        }

        bool? _isFlagsEnum = null;
        internal void SetIsFlagsEnum(string v)
        {
            _isFlagsEnum = bool.Parse(v);
        }


        public bool IsFlagsEnum
        {
            get
            {
                if (_isFlagsEnum == null)
                {
                    _isFlagsEnum = false;

                    // bugbug
                    if (TypeViewModel.IsPhoneContractHack(this.DeclaringType))
                        return false;

                    if (IsEnum && CustomAttributes != null)
                    {
                        foreach (var a in CustomAttributes)
                        {
                            if (a.Name == "FlagsAttribute")
                            {
                                _isFlagsEnum = true;
                                break;
                            }
                        }
                    }
                }

                return _isFlagsEnum == true;

            }
        }


        bool? _isExperimental = null;
        override public bool IsExperimental
        {
            // bugbug: Does this need to override MemberViewModel?
            get
            {
                if (_isExperimental == null)
                {
                    _isExperimental = false;

                    // bugbug
                    if (TypeViewModel.IsPhoneContractHack(this))
                    {
                        return false;
                    }

                    if (CustomAttributes != null)
                    {
                        foreach (var a in CustomAttributes)
                        {
                            if (a.Name == "ExperimentalAttribute")
                            {
                                _isExperimental = true;
                                break;
                            }
                        }
                    }
                }

                return _isExperimental == true;
            }
        }



        abstract public bool IsEventArgs { get; }

        abstract public TypeViewModel GetGenericTypeDefinition();

        bool? _isWindows = null;
        public bool IsWindows
        {
            get
            {
                if (_isWindows == null)
                {
                    // Check to see if it's a Windows type by checking all the places Windows types get loaded
                    // bugbug: this doesn't handle the case where none of the Windows types have been loaded

                    // See if it's in the .Net Reflection System32 type set
                    if (Manager.WinmdTypeSet != null && Manager.WinmdTypeSet.Types != null && Manager.WinmdTypeSet.Types.Contains(this))
                    {
                        _isWindows = true;
                    }

                    // See if it's in the MR System32 type set
                    else if (Manager.WindowsTypeSet != null && Manager.WindowsTypeSet.Types != null && Manager.WindowsTypeSet.Types.Contains(this))
                    {
                        _isWindows = true;
                    }
                    else
                        _isWindows = false;
                }

                return _isWindows == true;

            }
        }

        public TypeSet TypeSet { get; protected set; }

        bool? _isCustom = null;
        public bool IsCustom
        {
            get
            {
                if (_isCustom == null)
                {
                    if (Manager.CustomTypeSet != null
                        && Manager.CustomTypeSet.Types != null
                        && Manager.CustomTypeSet.Types.Contains(this))
                    {
                        _isCustom = true;
                    }
                    else
                    {
                        _isCustom = false;
                    }
                }

                return _isCustom == true;

            }
        }

        public bool IsWinMD
        {
            get
            {
                return this.TypeSet != null && this.TypeSet.IsWinmd;
            }
        }


        bool? _isWpf = null;
        public bool IsWpf
        {
            get
            {
                if (_isWpf == null)
                {
                    if (Manager.WpfTypeSet != null && Manager.WpfTypeSet.Types.Contains(this))
                        _isWpf = true;
                    else
                        _isWpf = false;
                }

                return _isWpf == true;

            }
        }







        bool? _isPhone = null;
        public bool IsPhone
        {
            get
            {
                if (_isPhone == null)
                {
                    if (Manager.WPTypeSet != null && Manager.WPTypeSet.Types != null && Manager.WPTypeSet.Types.Contains(this))
                        _isPhone = true;
                    else
                        _isPhone = false;
                }

                return _isPhone == true;
            }
        }


        string _restrictionName;
        override public string RestrictionName
        {
            get
            {
                if (_restrictionName == null)
                {
                    RestrictedApiInfo info;

                    _restrictionName = "";

                    if (RestrictedApiList.RestrictedApis.TryGetValue(this.FullName, out info))
                    {
                        _restrictionName = info.Restriction;
                    }
                }

                return _restrictionName;
            }
        }

        public string AlsoAvailableOn
        {
            get
            {
                if (!IsPhone && Manager.WPEnabled)
                {
                    var t = Manager.GetMatchingType(Manager.WPTypeSet.Types, this);

                    if (t != null)
                        return "Also available on Phone " + t.VersionFriendlyName;
                }
                else if (!IsWindows && Manager.WinMDEnabled)
                {
                    var t = Manager.GetMatchingType(Manager.WinmdTypeSet.Types, this);
                    if (t != null)
                        return "Also available on Windows " + t.VersionFriendlyName;
                }

                return null;

            }
        }
        //if (ContainsType(MainWindow.InstanceOld.WinmdTypeSet.Types, typeVM))
        //    sb.Append("8");
        //if (ContainsType(MainWindow.InstanceOld.WPTypeSet.Types, typeVM))
        //    sb.Append("P");
        //if (ContainsType(MainWindow.InstanceOld.SLTypeSet.Types, typeVM))
        //    sb.Append("S");
        //if (ContainsType(MainWindow.InstanceOld.WpfTypeSet.Types, typeVM))
        //    sb.Append("W");


        public IEnumerable<TypeViewModel> Ancestors
        {
            //get { return Type2Ancestors.GetAncestors(this); }
            get
            {
                var t = this;
                for (t = t.BaseType; t != null; t = t.BaseType)
                {
                    if (t.ShouldIgnore)// (ShouldIgnoreType(t))
                        continue;

                    yield return t;
                }

            }
        }

        public string IfUnsealedSaySo
        {
            get
            {
                if (IsSealed || !IsClass)
                    return null;
                else
                    return "(unsealed)";
            }
        }

        public string IfFlagsSaySo
        {
            get
            {
                if (IsFlagsEnum)
                    return "(flags)";
                else
                    return null;
            }
        }

        public override bool IsAssembly => false;

        public override bool IsFamilyOrAssembly => false;


        public override bool IsFamilyAndAssembly => false;

        protected override TypeViewModel GetDeclaringType()
        {
            return this;
        }

        abstract public bool IsNotPublic { get; }

        abstract public TypeAttributes Attributes { get; }

        virtual protected bool GcPressureBug
        {
            get
            {
                return false;
            }
        }


        override public void SetVersion(string version)
        {
            _versionChecked = true;
            _version = version;
        }

        // bugbug: Why is this both here and in the base MemberViewModel?
        string _version = null;
        static string _highestVersion = null;
        bool _versionChecked = false;

        override public string Version
        {
            get
            {
                if (!_versionChecked)
                {
                    _versionChecked = true;
                    _version = "";

                    if (!string.IsNullOrEmpty(TypeSet.Version))
                    {
                        _version = TypeSet.Version;
                    }
                    else if (ImportedApis.Initialized)
                    {
                        if (IsGenericParameter || IsByRef)
                        {
                            _version = "";
                        }

                        else if (this.DeclaringType.IsPhone)
                        {

                            if (Name == "CapturedFrame"
                                &&
                                Namespace == "Windows.Media.Capture")
                            {
                                // TypeLoad exception GCAttribute bug
                                _version = "06030000";
                            }
                            else if (Name == "DatagramSocketMessageReceivedEventArgs"
                                     && Namespace == "Windows.Networking.Sockets")
                            {
                                _version = "06020000";
                            }
                            else if (Name == "MessageWebSocketMessageReceivedEventArgs"
                                     && Namespace == "Windows.Networking.Sockets")
                            {
                                _version = "06020000";
                            }
                            else
                            {
                                var attrs = CustomAttributes;

                                if (attrs == null && GcPressureBug)
                                {
                                    // Assume it's the GC pressure problem on some new type, guess at the version
                                    // BUGBUG  BUGBUG  BUGBUG
                                    _version = _highestVersion;
                                }
                                else
                                {
                                    _version = GetVersionFromAttributes(attrs);
                                }
                            }

                            if (_version != null && _version.CompareTo(_highestVersion) > 0)
                                _highestVersion = _version;
                        }

                        else if (Namespace.StartsWith("Windows."))
                        {
                            if (ImportedApis.Win8.Find(Namespace + "." + PrettyName) != null) // bugbug
                                _version = "06020000";
                            else if (ImportedApis.WinBlue.Find(Namespace + "." + PrettyName) != null)
                                _version = "06030000";
                            else if (ImportedApis.PhoneBlue.Find(Namespace + "." + PrettyName) != null)
                                _version = "06030100";
                            else
                            {
                                // Contracts don't have contracts
                                // Some types aren't declared (array parameters)
                                if (string.IsNullOrEmpty(this.Contract)
                                    && !this.Name.EndsWith("Contract")
                                    && (BaseType == null || BaseType.FullName != "System.Array"))
                                {
                                    Debug.WriteLine(this.FullName);
                                    DebugLog.Start("Can't find contract: " + this.FullName);
                                    _version = "";
                                }
                                else
                                {
                                    _version = ReleaseInfo.GetVersionFromContract(this.Contract);
                                }
                            }
                        }
                    }

                    Debug.Assert(_version != null);
                }

                return _version;
            }
        }



        IList<TypeViewModel> _interfaces = null;
        bool _interfacesChecked = false;


        public IEnumerable<TypeViewModel> Descendents
        {
            get
            {
                if (!_descendentsChecked)
                {
                    _descendents = CalcDescendents();
                    _descendentsChecked = true;
                }

                return _descendents;
            }
        }
        IEnumerable<TypeViewModel> _descendents = null;
        bool _descendentsChecked = false;
        public IList<TypeViewModel> CalcDescendents()
        {
            var d = (from a in Manager.CurrentTypeSet.Types
                     where a.IsPublic || Manager.Settings.InternalInterfaces
                     where a.BaseType == this || a.Interfaces.Contains(this)
                     select a).ToList();

            return d;
        }

        public IList<TypeViewModel> Interfaces
        {
            get
            {
                if (!_interfacesChecked)
                {
                    _interfaces = CalculateInterfacesFromType();
                    _interfacesChecked = true;
                }

                return _interfaces;
            }
        }

        public IList<TypeViewModel> InternalInterfaces
        {
            get
            {
                return CalculateInterfacesFromType(includeInternal: true);
            }
        }

        public IList<TypeViewModel> PublicInterfaces
        {
            get
            {
                return (from i in Interfaces where i.IsPublic select i).ToList();
            }
        }


        bool? _nameStartsWithVerb = null;
        public bool NameStartsWithVerb
        {
            get
            {
                if (_nameStartsWithVerb == null)
                {

                }

                return _nameStartsWithVerb == true;
            }
        }



        IList<EventViewModel> _events = null;
        bool _eventsCalculated = false;

        public IList<EventViewModel> Events
        {
            get
            {
                if (!_eventsCalculated)
                {
                    lock (this)
                    {
                        if (!_eventsCalculated)
                        {
                            _events = CalculateEventsFromType();
                            _eventsCalculated = true;
                        }
                    }
                }

                Debug.Assert(_events != null);
                return _events;
            }
        }

        IList<MethodViewModel> _methods;
        bool _methodsCalculated = false;

        public IList<MethodViewModel> Methods
        {
            get
            {
                if (!_methodsCalculated)
                {
                    lock (this)
                    {
                        if (!_methodsCalculated)
                        {
                            _methods = CalculateMethodsFromType();
                            _methodsCalculated = true;
                        }
                    }
                }
                return _methods;
            }
        }

        IList<ConstructorViewModel> _constructors;
        public IList<ConstructorViewModel> Constructors
        {
            get
            {
                if (_constructors == null)
                {
                    lock (this)
                    {
                        if (_constructors == null)
                        {
                            var rawConstructors = CalculateConstructorsFromTypeOverride();

                            var constructors = from c in rawConstructors
                                               where c.IsPublic
                                                     || c.IsProtected
                                                     || Manager.Settings.InternalInterfaces
                                               orderby c.Parameters.Count
                                               select c;

                            _constructors = constructors.ToList();
                        }
                    }
                }

                return _constructors;
            }
        }
        abstract protected IList<ConstructorViewModel> CalculateConstructorsFromTypeOverride();

        IList<PropertyViewModel> _properties = null;
        public IList<PropertyViewModel> Properties
        {
            get
            {
                if (_properties == null)
                {
                    lock (this)
                    {
                        if (_properties == null)
                        {
                            _properties = CalculatePropertiesFromType();
                        }
                    }
                }

                Debug.Assert(_properties != null);
                return _properties;
            }
        }


        //public delegate bool TypeHasAssemblyHandler(Type t);
        //static public TypeHasAssemblyHandler TypeHasAssemblyCallback = null;

        public static bool ShouldIgnoreType(Type t)
        {
            if (t == null)
                return true;

            return _typesToIgnore.Contains(t.FullName);
        }


        IList<MemberOrTypeViewModelBase> _members;
        public IList<MemberOrTypeViewModelBase> Members
        {
            get
            {
                if (_members == null)
                {
                    var members = (Properties as IEnumerable<MemberOrTypeViewModelBase>)
                        .Union(Methods as IEnumerable<MemberOrTypeViewModelBase>)
                        .Union(Events as IEnumerable<MemberOrTypeViewModelBase>)
                        .Union(Constructors as IEnumerable<MemberOrTypeViewModelBase>)
                        .Union(Fields as IEnumerable<MemberOrTypeViewModelBase>);
                    _members = (from m in members orderby m.Name select m).ToList();
                }
                return _members;
            }
        }

        public IEnumerable<MemberOrTypeViewModelBase> RawMembers
        {
            get
            {
                foreach (var member in Members)
                {
                    if (member is PropertyViewModel)
                    {
                        var pvm = member as PropertyViewModel;
                        if (pvm.Getter != null)
                            yield return pvm.Getter;
                        if (pvm.Setter != null)
                            yield return pvm.Setter;
                        yield return member;
                    }
                    else if (member is EventViewModel)
                    {
                        var evm = member as EventViewModel;
                        if (evm.Adder != null)
                            yield return evm.Adder;
                        if (evm.Remover != null)
                            yield return evm.Remover;
                        yield return member;
                    }
                    else
                        yield return member;
                }
            }
        }


        public IEnumerable<MemberOrTypeViewModelBase> GetMember(string name)
        {
            foreach (var member in Members)
            {
                if (member.Name == name)
                    yield return member;
            }

            yield break;
        }

        public FieldViewModel GetField(string name)
        {
            foreach (var field in Fields)
                if (field.Name == name)
                    return field;

            return null;
        }

        public IEnumerable<MethodViewModel> GetMethods(string name)
        {
            foreach (var m in Methods)
                if (m.Name == name)
                    yield return m;

        }

        public PropertyViewModel GetProperty(string name)
        {
            foreach (var property in Properties)
                if (property.Name == name)
                    return property;

            return null;
        }



        public abstract IList<TypeViewModel> CalculateInterfacesFromType(bool includeInternal = false);


        protected abstract IList<PropertyViewModel> CalculatePropertiesFromTypeOverride(bool shouldFlatten);
        public IList<PropertyViewModel> CalculatePropertiesFromType(bool shouldFlatten = false)
        {
            if (!shouldFlatten && _properties != null)
            {
                return _properties;
            }

            var rawProperties = CalculatePropertiesFromTypeOverride(shouldFlatten);
            if (Manager.Settings.InternalInterfaces)
            {
                return rawProperties;
            }
            else
            {
                var properties = from prop in rawProperties
                                 let getMethod = prop.Getter
                                 let setMethod = prop.Setter
                                 where Manager.Settings.InternalInterfaces
                                       ||
                                       getMethod != null
                                       && (getMethod.IsPublic
                                             || getMethod.IsProtected
                                             || getMethod.IsExplicitImplementation())
                                       ||
                                       setMethod != null
                                       && (setMethod.IsPublic
                                             || setMethod.IsProtected
                                             || setMethod.IsExplicitImplementation())
                                 where !TypeViewModel.ShouldIgnoreType(prop.DeclaringType)
                                 orderby prop.Name
                                 select prop;

                return properties.ToList();
            }
        }

        abstract protected IList<EventViewModel> CalculateEventsFromTypeOverride(bool shouldFlatten);
        public IList<EventViewModel> CalculateEventsFromType(bool shouldFlatten = false)
        {
            if (!shouldFlatten && _events != null)
                return _events;

            var events = CalculateEventsFromTypeOverride(shouldFlatten);
            if (Manager.Settings.InternalInterfaces)
            {
                return events;
            }
            else
            {
                return (from ev in events
                        where ev.IsPublic
                            || ev.IsProtected
                            || ev.Adder.IsExplicitImplementation()
                        select ev).ToList();
            }
        }


        abstract protected IList<MethodViewModel> CalculateMethodsFromTypeOveride(bool shouldFlatten);
        public IList<MethodViewModel> CalculateMethodsFromType(bool shouldFlatten = false)
        {
            if (!shouldFlatten && _methods != null)
                return _methods;

            var rawMethods = CalculateMethodsFromTypeOveride(shouldFlatten);

            var methods = from method in rawMethods
                          orderby method.Name, CatchingGetMethodParametersLength(method)
                          where method.IsPublic
                                || method.IsProtected && !this.IsSealed // Protected doesn't really count if it can't be overridden
                                || method.IsExplicitImplementation()
                                || Manager.Settings.InternalInterfaces
                          select method;

            return methods.ToList();
        }

        int CatchingGetMethodParametersLength(MethodViewModel m)
        {
            try
            {
                return m.Parameters.Count;
            }
            catch (TypeLoadException)
            {
                // Calling GetParameters caused reflection to attempt to load a type that couldn't be
                // loaded (the assembly's probably not loaded).
                return 0;
            }
        }


        IList<FieldViewModel> _fields = null;
        bool _fieldsCalculated = false;

        public IList<FieldViewModel> Fields
        {
            get
            {
                if (!_fieldsCalculated)
                {
                    lock (this)
                    {
                        if (!_fieldsCalculated)
                        {
                            _fields = CalculateFieldsFromType();
                            _fieldsCalculated = true;
                        }
                    }
                }

                return _fields;
            }
        }

        bool _fieldsCalculating = false;
        public IList<FieldViewModel> FieldsAsync
        {
            get
            {
                IList<FieldViewModel> fields = null;

                if (_fieldsCalculated || _fieldsCalculating)
                    return _fields;
                else
                {
                    //var bw = new BackgroundWorker();
                    //bw.DoWork += (s, e) =>
                    var task = new Task(() =>
                    {
                        if (!_fieldsCalculated)
                            fields = CalculateFieldsFromType();
                    });

                    //bw.RunWorkerCompleted += (s, e) =>
                    task.ContinueWith((t) =>
                    {
                        if (!_fieldsCalculated)
                        {
                            _fields = fields;
                            _fieldsCalculated = true;
                        }
                        RaisePropertyChanged("FieldsAsync");
                    });

                    _fieldsCalculating = true;
                    //bw.RunWorkerAsync();
                    task.Start();

                    return null;
                }
            }



        }



        abstract protected IList<FieldViewModel> CalculateFieldsFromTypeOverride(bool shouldFlatten);
        public IList<FieldViewModel> CalculateFieldsFromType(bool shouldFlatten = false)
        {
            if (!shouldFlatten && _fields != null)
                return _fields;

            var rawFields = CalculateFieldsFromTypeOverride(shouldFlatten);
            // Filter out the compiler-generated event fields
            // For enum fields, sort by value
            var fields = from field in rawFields
                         where !field.IsSpecialName
                         where field.IsPublic
                             || CheckProtected(field)
                             || Manager.Settings.InternalInterfaces
                         orderby field.NameForSorting()
                         select field;

            return fields.ToList();
        }


        abstract public TypeViewModel GetInterface(string name);

        public IEnumerable<TypeViewModel> GetStaticInterfaces()
        {
            if (this.CustomAttributes == null)
                yield break;

            foreach (var attr in this.CustomAttributes)
            {
                if (attr.Name == "StaticAttribute")
                {
                    var args = attr.ConstructorArguments;// AttributeView.SafeGetCustomAttributeConstructorArguments(attr);
                    if (args != null && args.Count > 0 && args[0] != null)
                    {
                        yield return args[0].Value as TypeViewModel;
                    }
                }

            }
        }



        abstract public IEnumerable<TypeViewModel> GetAllInterfaces();
        abstract public IEnumerable<TypeViewModel> GetConstructorInterfaces();


        bool? _shouldIgnoreType = null;
        public bool ShouldIgnore
        {
            get
            {
                if (_shouldIgnoreType == null)
                {
                    _shouldIgnoreType = ShouldIgnoreType(this);
                }

                return _shouldIgnoreType == true;
            }
        }

        internal static string[] _typesToIgnore =
            {
                "System.__ComObject",
                "System.Attribute",
                "System.Runtime.InteropServices._Attribute",
                "System.Runtime.InteropServices.WindowsRuntime.RuntimeClass",
                "System.Runtime.InteropServices._MemberInfo",
                "System.Runtime.InteropServices._Type",
                "System.Runtime.InteropServices._Exception",
                "System.MarshalByRefObject",
                "System.Object",
                            "System.MulticastDelegate", // bugbug: This used to be commented out?
            "System.Delegate",
                "System.EventArgs",
                "System.ValueType",
                "System.Enum",
                "System.ICloneable",
                "System.Runtime.Serialization.ISerializable",
                "System.IComparable",
                "System.IFormattable",
                "System.IConvertible",
                "System.Runtime.Serialization.SerializationInfo",
                "System.Runtime.Serialization.IDeserializationCallback"
            };

        //static Assembly _mscorlib = null;
        private static bool ShouldIgnoreType(TypeViewModel t) // bugbug: make this an instance on t
        {
            if (t == null)
                return true;

            // bugbug: This is here for desktop, why?
            if (t.CantIgnoreTypesInAssembly())
                return false;

            return _typesToIgnore.Contains(t.FullName);
        }

        protected virtual bool CantIgnoreTypesInAssembly()
        {
            return Assembly == null;
        }


        static public void ReportSettingsInternalChanged()
        {
            lock (typeof(Manager))
            {
                var list = new List<TypeViewModel>();
                foreach (var typeSet in Manager.AllTypeSets)
                {
                    if (typeSet != null && typeSet.Types != null)
                    {
                        foreach (var type in typeSet.Types)
                        {
                            list.Add(type);
                        }
                    }
                }

                //var copy = _typeVMCache.ToList();
                foreach (var typeVM in list)
                {
                    //typeAndVM.Value.RaisePropertyChanged(String.Empty);
                    typeVM.OnSettingsInternalChanged();
                }
            }

        }

        protected virtual void OnSettingsInternalChanged()
        {
            // When Settings is changed to show/hide non-public APIs, we need to clear some caches
            _members = null;

            _properties = null;

            _methodsCalculated = false;
            _methods = null;

            _eventsCalculated = false;
            _events = null;

            _fieldsCalculated = false;
            _fields = null;

            _constructors = null;

            _interfaces = null;
            _interfacesChecked = false;

            _descendents = null;
            _descendentsChecked = false;

        }

        abstract public bool IsVoid { get; }
        abstract public bool IsByRef { get; }
        abstract public System.Reflection.GenericParameterAttributes GenericParameterAttributes { get; }

        TypeViewModel _baseType = null;
        bool _baseTypeChecked = false;
        public TypeViewModel BaseType
        {
            get
            {
                if (!_baseTypeChecked)
                {
                    _baseTypeChecked = true;
                    _baseType = GetBaseType();
                }

                return _baseType;
            }
        }
        abstract protected TypeViewModel GetBaseType();

        abstract public TypeViewModel[] GetGenericArguments();
        abstract public bool IsGenericType { get; }
        abstract public bool IsGenericParameter { get; }
        abstract public bool IsGenericTypeDefinition { get; }

        virtual public bool IsIdlSupported { get { return false; } }


        //// A cache of TypeVMs and a list of weak references. The list is necessary becuse you can't
        //// enumerate members of a ConditionalWeakTable
        //static private ConditionalWeakTable<object, TypeViewModel> _typeVMCache = new ConditionalWeakTable<object, TypeViewModel>();
        //static private List<WeakReference<TypeViewModel>> _typeVMWeakList = new List<WeakReference<TypeViewModel>>();


        //static public void AddToCache(string name, TypeViewModel vm, bool checkForDup = false)
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

        protected override Task<string> GetGitUrlFilenameAsync(string baseUri)
        {
            // Get the Git docs URL suffix for a type. E.g. "button.md"

            var s = $"{this.Name.ToLower()}.md";
            return Task<string>.FromResult(s);
        }

        //static protected TypeViewModel GetFromCacheBase(object t, Func<TypeViewModel> create)
        //{
        //    TypeViewModel vm = null;

        //    if (t == null)
        //        return null;

        //    if (!_typeVMCache.TryGetValue(t, out vm))
        //    {
        //        lock (_typeVMCache)
        //        {
        //            if (!_typeVMCache.TryGetValue(t, out vm))
        //            {
        //                vm = create();

        //                // This is a ConditionalWeakTable to avoid leaks
        //                _typeVMCache.Add(t, vm);

        //                // This is so that we can do enumeration
        //                _typeVMWeakList.Add(new WeakReference<TypeViewModel>(vm));
        //            }
        //        }
        //    }

        //    return vm;
        //}

        // bugbug: Consolidate this with the other cache
        // Can't use TypeInfo instance as a key because CLR appears to give back different instances
        // of TypeInfo for the same type, and the equality isn't overridden.
        static Dictionary<string, TypeViewModel> _cacheTypeInfo = new Dictionary<string, TypeViewModel>();

        static protected TypeViewModel GetFromCacheBase(TypeInfo typeInfo, Func<TypeViewModel> create)
        {
            TypeViewModel vm = null;

            if (typeInfo == null)
                return null;

            // Open types have a Namespace and a Name, but FullName is null.
            // Why does an open type have its type parameters in GenericTypeArguments?
            var fullName = typeInfo.FullName;
            if (fullName == null)
                fullName = typeInfo.Namespace + "." + typeInfo.Name;

            if (!_cacheTypeInfo.TryGetValue(fullName, out vm))
            {
                lock (_cacheTypeInfo)
                {
                    if (!_cacheTypeInfo.TryGetValue(fullName, out vm))
                    {
                        vm = create();// new TypeViewModel(t);
                        _cacheTypeInfo.Add(fullName, vm);
                    }
                }
            }

            return vm;
        }


        //static public TypeViewModel LookupByName(string typeName)
        //{

        //    // bugbug:  should be a cache per type set

        //    lock (_typeVMCache)
        //    {
        //        List<WeakReference<TypeViewModel>> cleanup = null;

        //        try
        //        {
        //            foreach (var typeWeakRef in _typeVMWeakList)
        //            {
        //                if (typeWeakRef.TryGetTarget(out var type))
        //                {
        //                    if (type.FullName == typeName)
        //                    {
        //                        return type;
        //                    }
        //                }
        //                else
        //                {
        //                    // The type's not in the list anymore. Remember that it needs to be cleaned
        //                    // up (but don't clean it up now as it would break the enumerator).
        //                    if (cleanup == null)
        //                    {
        //                        cleanup = new List<WeakReference<TypeViewModel>>();
        //                    }
        //                    cleanup.Add(typeWeakRef);
        //                }
        //            }
        //        }

        //        finally
        //        {
        //            if (cleanup != null)
        //            {
        //                foreach (var typeWeakRef in cleanup)
        //                {
        //                    _typeVMWeakList.Remove(typeWeakRef);
        //                }
        //            }
        //        }
        //    }

        //    return null;
        //}


        static public bool operator !=(TypeViewModel t1, TypeViewModel t2)
        {
            return !(t1 == t2);
        }

        static public bool operator ==(TypeViewModel t1, TypeViewModel t2)
        {
            // bugbug: huh?
            if (Object.ReferenceEquals(t1, null))
                return Object.ReferenceEquals(t2, null);

            return t1.Equals(t2);
        }

        public override bool Equals(object obj)
        {
            //return base.Equals(obj);
            if (obj == null || !(obj is TypeViewModel))
                return false;

            return FullName == (obj as TypeViewModel).FullName;
            //|| UnprojectedFullName == (obj as TypeViewModel).UnprojectedFullName;
        }

        public override int GetHashCode()
        {
            return FullName.GetHashCode();
        }


        //abstract public string FullName { get; }
        //abstract public string Namespace { get; }
        abstract public Assembly Assembly { get; }

        abstract public string AssemblyLocation { get; }


        bool? _isXamlControl = null;
        public bool IsXamlControl
        {
            get
            {
                if (_isXamlControl == null)
                {
                    _isXamlControl = false;

                    var type = this;
                    while (type != null)
                    {
                        if (type.FullName == "Windows.UI.Xaml.Controls.Control")
                        {
                            _isXamlControl = true;
                            break;
                        }

                        type = type.BaseType;
                    }
                }

                return _isXamlControl == true;
            }
        }

        bool? _isAutomationPeer;
        public bool IsAutomationPeer
        {
            get
            {
                if (_isAutomationPeer == null)
                {
                    _isAutomationPeer = false;

                    foreach (var iface in Interfaces)
                    {
                        if (iface.Namespace == "Windows.UI.Xaml.Automation.Provider"
                            && iface.Name.EndsWith("Provider"))
                        {
                            _isAutomationPeer = true;
                            break;
                        }
                    }

                }

                return _isAutomationPeer == true;
            }
        }

        TypeViewModel _automationPeer = null;
        public TypeViewModel AutomationPeer
        {
            get
            {
                return _automationPeer;
            }
            set
            {
                _automationPeer = value;
                RaisePropertyChanged("AutomationProviders");
            }
        }

        public IEnumerable<TypeViewModel> AutomationProviders
        {
            get
            {
                if (AutomationPeer == null)
                    yield break;

                foreach (var iface in AutomationPeer.Interfaces)
                {
                    if (iface.Name.EndsWith("Provider"))
                    {
                        yield return iface;
                    }
                }

                yield break;
            }
        }

        public bool CheckedAutomationPeer
        { get; set; }


        public int ExportIndex { get; set; }
        public bool ReallyMatchedInSearch { get; internal set; }
        public abstract TypeViewModel UnderlyingEnumType { get; }

        virtual public AcidInfo AcidInfo => null;

        /// <summary>
        /// True if this type is in a TypeSet (not a generated/fake type)
        /// </summary>
        public bool IsInTypes { get; internal set; } = false;


        /// <summary>
        /// Return a cached bool in context of `context` if it was set in the same MatchGeneration
        /// </summary>
        public bool? CheckMatchesCache(object context)
        {
            if (_cacheMatchGeneration != Manager.MatchGeneration)
            {
                // Cached value is invalid
                return null;
            }

            if (_matchCache.TryGetValue(context, out var value))
            {
                // Return cached value
                return value;
            }

            // Not cached yet
            return null;
        }

        /// <summary>
        /// Cache a bool in context of `context` for this MatchGeneration
        /// </summary>
        public void SetMatchesCache(object context, bool value)
        {
            if (_cacheMatchGeneration != Manager.MatchGeneration)
            {
                _matchCache.Clear();
            }
            _matchCache[context] = value;
            _cacheMatchGeneration = Manager.MatchGeneration;
        }
        int _cacheMatchGeneration = -1;
        Dictionary<object, bool> _matchCache = new Dictionary<object, bool>();


    }
}
