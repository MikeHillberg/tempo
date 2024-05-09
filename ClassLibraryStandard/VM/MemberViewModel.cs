using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
//using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{

    //abstract public class CustomAttributeViewModel
    //{
    //    public virtual ConstructorInfo Constructor { get; }
    //    public virtual IList<CustomAttributeTypedArgument> ConstructorArguments { get; }
    //    public virtual IList<CustomAttributeNamedArgument> NamedArguments { get; }
    //    public override bool Equals(object obj);
    //    public static IList<CustomAttributeData> GetCustomAttributes(Assembly target);
    //    public static IList<CustomAttributeData> GetCustomAttributes(MemberInfo target);
    //    public static IList<CustomAttributeData> GetCustomAttributes(Module target);
    //    public static IList<CustomAttributeData> GetCustomAttributes(ParameterInfo target);
    //    public override int GetHashCode();
    //    public override string ToString();
    //}

    abstract public class CustomAttributeViewModel
    {
        abstract public string Name { get; }
        abstract public string FullName { get; }
        abstract public IList<CustomAttributeTypedArgumentViewModel> ConstructorArguments { get; }
        abstract public IList<CustomAttributeNamedArgumentViewModel> NamedArguments { get; }

        string _guidAsString = null;
        public string TryParseGuidAttribute()
        {
            if (_guidAsString != null)
            {
                return _guidAsString;
            }

            _guidAsString = string.Empty;

            var ta = new AttributeTypeInfo();

            ta.TypeName = this.Name;

            if (ta.TypeName != "GuidAttribute" || this.ConstructorArguments.Count != 11)
            {
                return _guidAsString;
            }

            var args = this.ConstructorArguments;
            if (args == null)
            {
                return _guidAsString;
            }

            // I don't trust the metadata enough to be in the expected format, so don't die on exceptions
            try
            {
                // What the default display would show:
                // {UInt32 = 0x1B36E047, UInt16 = 35259, UInt16 = 16950, Byte = 150, Byte = 172, Byte = 113, Byte = 208, Byte = 18, Byte = 187, Byte = 72, Byte = 105}
                //
                // Standard format that we want (note 3 words after the dword instead of 2):
                // {1b36e047-89bb-4236-96ac-71d012bb4869}

                var parts = new uint[11];
                for (int i = 0; i < parts.Length; i++)
                {
                    switch (args[i].ArgumentType.Name)
                    {
                        // The first argument
                        case "UInt32":
                            parts[i] = (uint)args[i].Value;
                            break;

                        // The second/third arguments
                        case "UInt16":
                            parts[i] = (ushort)args[i].Value;
                            break;

                        // The rest
                        case "Byte":
                            parts[i] = (byte)args[i].Value;
                            break;

                        default:
                            return _guidAsString;
                    }
                }

                var guidString = new StringBuilder();
                guidString.Append("{");

                // The dword
                guidString.Append(parts[0].ToString("x8"));
                guidString.Append("-");

                // The two words
                guidString.Append(parts[1].ToString("x4"));
                guidString.Append("-");
                guidString.Append(parts[2].ToString("x4"));
                guidString.Append("-");

                // The next two bytes are displayed together, like a word, but they're not byte swapped
                var combined = (parts[3] << 8) + parts[4];
                guidString.Append(combined.ToString("x4"));
                guidString.Append("-");

                // The rest of the bytes
                guidString.Append(parts[5].ToString("x2"));
                guidString.Append(parts[6].ToString("x2"));
                guidString.Append(parts[7].ToString("x2"));
                guidString.Append(parts[8].ToString("x2"));
                guidString.Append(parts[9].ToString("x2"));
                guidString.Append(parts[10].ToString("x2"));

                guidString.Append("}");

                _guidAsString = guidString.ToString();

            }
            catch (Exception e)
            {
                UnhandledExceptionManager.ProcessException(e);
            }

            return _guidAsString;
        }


        // Write out the attribute in a nice string that looks a little like C#
        public string PrettyFormat
        {
            get
            {
                if (_prettyFormat == null)
                {
                    var sb = new StringBuilder();
                    sb.Append($"[{this.Name}");

                    var first = true;
                    foreach (var arg in this.ConstructorArguments)
                    {
                        if (first)
                        {
                            sb.Append("(");
                        }
                        else
                        {
                            sb.Append(", ");
                        }
                        first = false;

                        sb.Append(arg.Value);
                    }

                    foreach (var arg in this.NamedArguments)
                    {
                        if (first)
                        {
                            sb.Append("(");
                        }
                        else
                        {
                            sb.Append(", ");
                        }
                        first = false;

                        sb.AppendFormat("{0}={1}", arg.MemberName, arg.TypedValue.Value);
                    }

                    if (!first)
                    {
                        sb.Append(")");
                    }

                    sb.Append("]");
                    _prettyFormat = sb.ToString();
                }

                return _prettyFormat;
            }
        }

        string _prettyFormat = null;
    }

    abstract public class CustomAttributeNamedArgumentViewModel
    {
        //abstract public MemberViewModel MemberViewModel { get; }
        abstract public string MemberName { get; }
        abstract public CustomAttributeTypedArgumentViewModel TypedValue { get; }

    }

    abstract public class CustomAttributeTypedArgumentViewModel
    {
        abstract public object Value { get; }
        abstract public TypeViewModel ArgumentType { get; }
    }

    abstract public partial class BaseViewModel
    {
        IList<CustomAttributeViewModel> _customAttributes = null;
        bool _customAttributesFetched = false;
        abstract protected IList<CustomAttributeViewModel> CreateAttributes();
        public IList<CustomAttributeViewModel> CustomAttributes
        {
            get
            {
                if (!_customAttributesFetched)
                {
                    _customAttributes = CreateAttributes();
                    _customAttributesFetched = true;
                }
                return _customAttributes;
            }
        }


        // Attributes in WinMD that you don't see in a projection
        static string[] _winmdAttributes = new string[]
        {
            "ComImportAttribute",
            "StaticAttribute",
            "ActivatableAttribute",
            "ContractVersionAttribute",
            "ComposableAttribute",
            "OverloadAttribute"
        };


        IList<CustomAttributeViewModel> _nonWinMdCustomAttributes;

        /// <summary>
        /// CustomAttributes, with the WinMD attributes like "ComposableAttribute" filtered out
        /// </summary>
        public IList<CustomAttributeViewModel> NonWinmdCustomAttributes
        {
            get
            {
                if (_nonWinMdCustomAttributes != null)
                {
                    return _nonWinMdCustomAttributes;
                }
                _nonWinMdCustomAttributes = new List<CustomAttributeViewModel>();

                foreach (var attr in CustomAttributes)
                {
                    if (!_winmdAttributes.Contains(attr.Name))
                    {
                        _nonWinMdCustomAttributes.Add(attr);
                    }
                }

                return _nonWinMdCustomAttributes;
            }
        }

        public virtual bool IsOverloaded => false;

        abstract public bool IsProtected { get; }


        // This is used when evaluating -where expressions. This is used to string-ize
        // property values.
        public virtual string ToWhereString()
        {
            return ToString();
        }


        protected string GetVersionFromAttributes(IEnumerable<CustomAttributeViewModel> attrs)
        {
            string version = null;

            if (attrs != null)
            {
                foreach (var attr in attrs)
                {
                    if (attr.Name != "VersionAttribute")
                        continue;

                    var argument = attr.ConstructorArguments[0];
                    if (argument != null)
                    {
                        var ver = (UInt32)argument.Value;
                        version = String.Format("{0:X8}", ver);
                    }
                }
            }

            if (version == null)
            {
                var contract = GetContractFromAttributes(attrs);
                if (!string.IsNullOrEmpty(contract))
                {
                    version = ReleaseInfo.GetVersionFromContract(contract);
                }
            }

            if (version == null)
                version = "";

            return version;
        }

        protected virtual string GetContractFromAttributes(IEnumerable<CustomAttributeViewModel> attrs)
        {
            return TypeViewModel.GetContractFromAttributesWithNetReflectionHack(attrs);
        }

    }




    [Flags]
    public enum ExportTypeViewModelFlags
    {
        Struct = 1,
        Delegate = 2,
        NotPublic = 4,
        Void = 8,
        ByRef = 0x10,
        GenericType = 0x11,
        GenericParameter = 0x12,
        GenericTypeDefinition = 0x14,
        Class = 0x18,
        Interface = 0x20,
        Enum = 0x21,
        ValueType = 0x22,
        Sealed = 0x24,
        Static = 0x28,
        Virtual = 0x40,
        Abstract = 0x41,
        Family = 0x42
    };


    abstract public class PropertyViewModel : MemberViewModelBase
    {
        public override MemberKind MemberKind { get { return MemberKind.Property; } }

        public override bool IsProperty => true;

        public override string MsdnRelativePath
        {
            get
            {
                return DeclaringType.FullName + "." + Name;
            }
        }

        // Is this property backed by a DP?
        // (Note that this returns true for Background, not BackgroundProperty. To detect the latter,
        // use IsActualDependencyProperty
        virtual public bool IsDependencyProperty
        {
            get
            {
                // Look for a field that's this property's name plus "Property", of type DependencyProperty
                // (This is the WPF case)
                var field = this.DeclaringType.GetField(this.Name + "Property");
                if (field != null && !IsDependencyPropertyTypeName(field.FieldType.FullName))
                {
                    field = null;
                }

                // For Xaml case, do the same, but look for a property
                var prop = this.DeclaringType.GetProperty(this.Name + "Property");
                if (prop != null && !IsDependencyPropertyTypeName(prop.PropertyType.FullName))
                {
                    prop = null;
                }

                // If either exists, then this property is a DP
                return field != null || prop != null;
            }
        }

        /// <summary>
        /// Override for properties which require special handling
        /// </summary>
        internal override bool GetIsDeprecated(out string deprecationString)
        {
            if (base.GetIsDeprecated(out deprecationString))
            {
                return true;
            }

            // If the property itself isn't deprecated, the getter might be
            // (Not sure what causes this to happen)
            var getter = Getter;
            if(getter == null)
            {
                // This shouldn't ever happen
                return false;
            }

            return getter.GetIsDeprecated(out deprecationString);
        }

        public bool IsDependencyPropertyField
        {
            get { return IsDependencyPropertyFieldHelper(this, this.PropertyType); }
        }

#pragma warning disable CS1998 // async method that returns sync
        async protected override Task<string> GetGitUrlFilenameAsync(string baseUri)
        {
            // Get the Git docs URL filename for a property.

            return $"{this.DeclaringType.Name.ToLower()}_{this.Name.ToLower()}.md";
        }
#pragma warning restore CS1998

        public override bool IsAdded
        {
            get
            {
                return base.IsAdded || !string.IsNullOrEmpty(SetterVersionFriendlyNameOverride);
            }
        }

        // A property setter can be added in a later version
        // then the getter
        string _setterVersionFriendlyNameOverride = null;
        public string SetterVersionFriendlyNameOverride
        {
            get
            {
                UpdateVersionAndContractSetterOverride();
                return _setterVersionFriendlyNameOverride;
            }
        }

        string _setterContractOverride = null;
        public string SetterContractOverride
        {
            get
            {
                UpdateVersionAndContractSetterOverride();
                return _setterContractOverride;

            }
            private set { _setterContractOverride = value; }
        }

        void UpdateVersionAndContractSetterOverride()
        {
            if (_setterVersionFriendlyNameOverride != null)
                return;

            _setterVersionFriendlyNameOverride = string.Empty;
            _setterContractOverride = string.Empty;

            if (Setter != null)
            {
                if (Setter.Version != this.Version)
                {
                    _setterVersionFriendlyNameOverride = Setter.VersionFriendlyName;
                    _setterContractOverride = Setter.Contract;
                }
            }
        }

        public override string ToString()
        {
            //return "TempoProperty: " + DeclaringType.Name + "." + Name;
            return Name;
        }

        string _fullName = null;
        public override string FullName
        {
            get
            {
                if (_fullName == null)
                {
                    var sb = new StringBuilder();
                    sb.Append(DeclaringType.FullName);
                    sb.Append("." + Name);

                    _fullName = sb.ToString();
                }
                return _fullName;
            }
        }

        public override bool IsSealed
        {
            get { return Getter != null && Getter.IsSealed; }
        }

        public string GetSetCodeString
        {
            get
            {
                if (CanWrite)
                    return "get; set;";
                else
                    return "get;";
            }
        }



        public override bool IsAssembly
        {
            get { return Getter.IsAssembly; }
        }

        public override bool IsFamilyOrAssembly
        {
            get { return Getter.IsFamilyOrAssembly; }
        }

        public override bool IsFamilyAndAssembly
        {
            get { return Getter.IsFamilyAndAssembly; }
        }



        MethodViewModel _getMethodViewModel = null;
        bool _getMethodViewModelFetched = false;
        abstract protected MethodViewModel CreateGetterMethodViewModel();
        public MethodViewModel Getter
        {
            get
            {
                if (!_getMethodViewModelFetched)
                {
                    _getMethodViewModel = CreateGetterMethodViewModel();
                    //if (_getMethod != null)
                    //    _getMethodViewModel = new MethodViewModel(_getMethod);
                    _getMethodViewModelFetched = true;
                }
                return _getMethodViewModel;
            }
        }


        MethodViewModel _setMethodViewModel = null;
        bool _setMethodViewModelFetched = false;
        abstract protected MethodViewModel CreateSetterMethodViewModel();
        public MethodViewModel Setter
        {
            get
            {
                if (!_setMethodViewModelFetched)
                {
                    _setMethodViewModel = CreateSetterMethodViewModel();
                    //if (_setMethod != null)
                    //    _setMethodViewModel = new MethodViewModel(_setMethod);
                    _setMethodViewModelFetched = true;
                }
                return _setMethodViewModel;
            }
        }


        IList<ParameterViewModel> _indexParameters = null;
        bool _indexParametersFetched = false;
        protected abstract IList<ParameterViewModel> CreateIndexParameters();
        public IList<ParameterViewModel> IndexParameters
        {
            get
            {
                if (!_indexParametersFetched)
                {
                    _indexParameters = CreateIndexParameters();
                    //var q = from p in this.PropertyInfo.GetIndexParameters()
                    //        select new ParameterViewModel(p);
                    //_indexParameters = q.ToList();
                }
                return _indexParameters;
            }
        }

        public bool GetMethodIsStatic
        {
            get
            {
                //return _getMethod != null && _getMethod.IsStatic;
                return Getter != null && Getter.IsStatic;
            }
        }

        public override bool IsPublic { get { return Getter != null && Getter.IsPublic || Setter != null && Setter.IsPublic; } }
        public bool IsPrivate { get { return Getter != null && Getter.IsPrivate || Setter != null && Setter.IsPrivate; } }

        public override bool IsStatic
        {
            get
            {
                return Getter != null && Getter.IsStatic || Setter != null && Setter.IsStatic;
            }
        }

        public MethodAttributes GetterAttributes
        {
            get
            {
                return Getter.Attributes;
            }
        }

        public MethodAttributes SetterAttributes
        {
            get
            {
                if (Setter == null)
                    return (MethodAttributes)0;
                else
                    return Setter.Attributes;
            }
        }

        abstract public bool CanRead { get; }
        abstract public bool CanWrite { get; }
        public override bool IsAbstract
        {
            get { return Getter.IsAbstract; }
        }
        public override bool IsVirtual
        {
            get { return Getter.IsVirtual; }
        }

        public override bool IsFamily
        {
            get { return Getter.IsFamily; }
        }

        public abstract TypeViewModel PropertyType { get; }

    }

    abstract public class EventViewModel : MemberViewModelBase
    {
        public override MemberKind MemberKind { get { return MemberKind.Event; } }

        public override bool IsEvent => true;

        public override string MsdnRelativePath
        {
            get
            {
                return DeclaringType.FullName + "." + Name;
            }
        }


        public override string ToString()
        {
            return Name;
        }


        string _fullName = null;
        public override string FullName
        {
            get
            {
                if (_fullName == null)
                {
                    var sb = new StringBuilder();
                    sb.Append(DeclaringType.FullName);
                    sb.Append("." + Name);
                    _fullName = sb.ToString();
                }
                return _fullName;
            }
        }

        public System.Reflection.MethodAttributes AddAttributes
        {
            get
            {
                if (Adder == null)
                    return (System.Reflection.MethodAttributes)0;
                else
                    return Adder.Attributes;
            }
        }


        public System.Reflection.MethodAttributes RemoveAttributes
        {
            get
            {
                if (Remover == null)
                    return (MethodAttributes)0;
                else
                    return Remover.Attributes;
            }
        }


#pragma warning disable CS1998 // async method that returns sync
        async protected override Task<string> GetGitUrlFilenameAsync(string baseUri)
        {
            // Get the Git docs URL filename for an event.

            return $"{this.DeclaringType.Name.ToLower()}_{this.Name.ToLower()}.md";
        }
#pragma warning restore CS1998 // async method that returns sync

        public override bool IsSealed
        {
            get { return Adder != null && Adder.IsSealed; }
        }

        public override bool IsAssembly
        {
            get { return Adder.IsAssembly; }
        }
        public override bool IsFamilyOrAssembly
        {
            get { return Adder.IsFamilyOrAssembly; }
        }

        public override bool IsFamilyAndAssembly
        {
            get { return Adder.IsFamilyAndAssembly; }
        }

        MethodViewModel _adder = null;
        abstract protected MethodViewModel CreateAdderMethodViewModel();
        public MethodViewModel Adder
        {
            get
            {
                if (_adder == null)
                    _adder = CreateAdderMethodViewModel();
                //new MethodViewModel(_addMethod);
                return _adder;
            }
        }

        MethodViewModel _remover = null;
        abstract protected MethodViewModel CreateRemoverMethodViewModel();
        public MethodViewModel Remover
        {
            get
            {
                if (_remover == null)
                    _remover = CreateRemoverMethodViewModel();
                return _remover;
            }
        }


        MethodViewModel _invoker = null;
        protected abstract MethodViewModel CreateInvoker();
        public MethodViewModel Invoker
        {
            get
            {
                if (_invoker == null)
                {
                    _invoker = CreateInvoker();
                }
                return _invoker;
            }
        }

        protected abstract TypeViewModel GetTypeFromCache(TypeViewModel typeViewModel);

        TypeViewModel _argsType = null;
        public TypeViewModel ArgsType
        {
            get
            {
                if (_argsType == null && Invoker != null)
                {
                    var parameters = Invoker.Parameters;
                    if (parameters.Count < 2)
                        return null;

                    _argsType = parameters[1].ParameterType;

                }

                return _argsType;
            }
        }


        TypeViewModel _senderType = null;
        public TypeViewModel SenderType
        {
            get
            {
                if (_senderType == null && Invoker != null)
                {
                    var parameters = Invoker.Parameters;
                    if (parameters.Count < 2)
                        return null;

                    _senderType = parameters[0].ParameterType;
                }

                return _senderType;
            }
        }

        public bool IsUntyped
        {
            get
            {
                var isUntyped = false;

                // Sometimes in the debugger Invoker is null
                if (Invoker != null)
                {
                    var parameters = Invoker.Parameters;// invokeMethod.GetParameters();

                    if (parameters.Count < 2)
                        isUntyped = true;
                    else if (parameters[1].ParameterType.FullName == "System.Object")
                        isUntyped = true;
                }
                else
                {
                    Debug.Assert(Debugger.IsAttached);
                }

                return isUntyped;
            }
        }

        public override bool IsPublic
        {
            get { return Adder.IsPublic; }
        }
        public bool IsPrivate
        {
            get { return Adder.IsPrivate; }
        }
        public override bool IsAbstract
        {
            get { return Adder.IsAbstract; }
        }
        public override bool IsVirtual
        {
            get { return Adder.IsVirtual; }
        }
        public override bool IsFamily
        {
            get { return Adder.IsFamily; }
        }
        public override bool IsStatic
        {
            get
            {
                return Adder.IsStatic;
            }
        }

        TypeViewModel _eventHandlerType = null;
        protected abstract TypeViewModel CreateEventHandlerType();
        public TypeViewModel EventHandlerType
        {
            get
            {
                if (_eventHandlerType == null)
                {
                    _eventHandlerType = CreateEventHandlerType();
                    //ReflectionTypeViewModel.GetFromCache(EventInfo.EventHandlerType);
                }
                return _eventHandlerType;
            }
        }
    }

    abstract public class ParameterViewModel : BaseViewModel
    {
        public override MemberKind MemberKind { get { return MemberKind.Any; } }
        public override string Version
        {
            get { throw new NotImplementedException(); }
        }

        public int ParameterIndex { get; set; } = -1;

        public virtual string ParameterExtensionModifier
        {
            get
            {
                return "";
            }
        }


        int _matchGeneration = -1;
        public void SetMatchGeneration()
        {
            _matchGeneration = Manager.MatchGeneration;
        }

        /// <summary>
        /// True if this parameter was found in the latest search
        /// </summary>
        public bool IsMatch
        {
            get { return _matchGeneration == Manager.MatchGeneration; }
        }

        /// <summary>
        /// True if the parameter name matched in the current search
        /// </summary>
        public bool IsNameMatch
        {
            get { return _nameMatchGeneration == Manager.MatchGeneration; }
        }

        int _nameMatchGeneration = -1;
        public void SetNameMatchGeneration()
        {
            _nameMatchGeneration = Manager.MatchGeneration;
        }


        public override bool IsFamilyOrAssembly
        {
            get { throw new NotImplementedException(); }
        }
        public override bool IsFamilyAndAssembly
        {
            get { throw new NotImplementedException(); }
        }
        public override bool IsAssembly
        {
            get { throw new NotImplementedException(); }
        }

        public override MyMemberTypes MemberType { get { return (MyMemberTypes)0; } }
        abstract public bool IsIn { get; }

        abstract public bool IsOut { get; }

        public override bool IsStatic
        {
            get { throw new NotImplementedException(); }
        }


        TypeViewModel _parameterType = null;
        abstract protected TypeViewModel CreateParameterType();
        public TypeViewModel ParameterType
        {
            get
            {
                if (_parameterType == null)
                {
                    _parameterType = CreateParameterType();// ReflectionTypeViewModel.GetFromCache(ParameterInfo.ParameterType);
                }
                return _parameterType;
            }
        }
    }

    /// <summary>
    /// Base for all member (non-type) ViewModels
    /// </summary>
    abstract public class MemberViewModelBase : MemberOrTypeViewModelBase
    {
        // Override all of these abstracts that only get set on a type
        override public bool IsInterface => false;
        override public bool IsEnum => false;
        override public bool IsValueType => false;
        override public bool IsClass => false;
        override public bool IsStruct => false;
        override public bool IsDelegate => false;

    }


    abstract public class MethodViewModel : MemberViewModelBase
    {
        public override MemberKind MemberKind { get { return MemberKind.Method; } }

        async protected override Task<string> GetGitUrlFilenameAsync(string baseUrl)
        {
            return await GetGitUrlFilenameAsyncHelper(
                baseUrl,
                this.DeclaringType,
                this.Name,
                this.Parameters);
        }

        static async public Task<string> GetGitUrlFilenameAsyncHelper(
            string baseUrl,
            TypeViewModel declaringType,
            string name,
            IList<ParameterViewModel> parameters)
        {
            // I don't know how to figure out the filename for the .md files for methods, they appear to have a hash of something, in order to deal
            // with overloaded method names. E.g. CameraInstrincisc.DistortPoints(...) is "cameraintrinsics_distortpoints_88187186.md".
            // So the solution here is to get all the potential files by querying GitHub for e.g. "type_method_*", then look inside at the 
            // parameters of each one to find the right overload.

            // This query URI searches GitHub for the files for this method name. We have to search for something, not just the path/filename
            // or GitHub will reject it, so search for the type name, because it's always on the method pages.
            var queryUri = "https://api.github.com/search/code" +
                      $"?q={declaringType.Name.ToLower()}" +
                      $"+repo:MicrosoftDocs/winrt-api" +
                      $"+path:/{declaringType.Namespace.ToLower()}/" +
                      $"+filename:{declaringType.Name.ToLower()}_{name.ToLower()}_*";

            // Send the request to GitHub and get back the filenames that match the pattern
            var reader = await OverridableHelpers.Instance.GetUriAsync(queryUri);

            // Get the actual filenames out of the results
            var names = OverridableHelpers.Instance.GetMethodMarkdownFilenamesFromQuery(reader);

            string theName = null;
            foreach (var nm in names)
            {
                // Read the method markdown page
                var fileUri = $"{baseUrl}{nm}";
                var fileReader = await OverridableHelpers.Instance.GetUriAsync(fileUri);

                // Read the file and get the method's parameter count.
                // This could turn up a wrong answer, better would be to look at the parameter names.
                // It's possible for that to still be ambiguous though.
                var parameterCount = GetParameterCountFromMethodDocPage(fileReader);
                if (parameterCount == parameters.Count)
                {
                    theName = nm;
                    break;
                }
            }
            if (string.IsNullOrEmpty(theName))
            {
                return "";
            }

            return $"{theName.ToLower()}";
        }

        // Given the md for a method in the docs (from GitHub), get the method's parameter count
        static public int GetParameterCountFromMethodDocPage(StringReader reader)
        {
            /* Example content from method md page:
             
                    ## -parameters
                    ### -param input
                    The string to validate.

                    ### -param contentCodingHeaderValue
                    The [HttpContentCodingHeaderValue](httpcontentcodingheadervalue.md) version of the string.
             */

            var foundParameters = false;
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    // End of file
                    break;
                }

                line = line.Trim();
                if (line.StartsWith("## -parameters"))
                {
                    foundParameters = true;
                    break;
                }
            }

            if (!foundParameters)
            {
                return 0;
            }

            int count = 0;
            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                {
                    // End of file
                    break;
                }

                line = line.Trim();

                if (line.StartsWith("## "))
                {
                    // Out of the parameters section
                    break;
                }
                else if (line.StartsWith("### -param"))
                {
                    // Found a parameter
                    count++;
                }
            }

            return count;
        }


        public override bool IsMethod => true;

        bool? _isOverloaded = null;
        override public bool IsOverloaded
        {
            get
            {
                if (_isOverloaded == null)
                {
                    var memberList = DeclaringType.GetMember(Name);
                    _isOverloaded = memberList.ToList().Count > 1;
                }

                return _isOverloaded == true;
            }
        }

        /// <summary>
        /// Return the ABI name from any ViewModel. Only actually returns something for a Method,
        /// but allow any type for ease of use.
        /// </summary>
        public static string CalculateAbiName(MemberOrTypeViewModelBase vm)
        {
            var methodVM = vm as MethodViewModel;
            if (methodVM == null)
            {
                return null;
            }

            return methodVM.AbiName;
        }

        public virtual bool IsExtensionMethod
        {
            get { return false; }
        }

        public override string MsdnRelativePath
        {
            get
            {
                return DeclaringType.FullName + "." + Name;
            }
        }

        string _fullName = null;
        public override string FullName
        {
            get
            {
                if (_fullName == null)
                {
                    var sb = new StringBuilder();
                    sb.Append(DeclaringType.FullName);
                    sb.Append(".");
                    sb.Append(Name);
                    sb.Append(GetParameterSignature(Parameters));
                    return _fullName = sb.ToString();
                }
                return _fullName; ;
            }
        }

        public static string GetParameterSignature(IList<ParameterViewModel> parameters)
        {
            var sb = new StringBuilder("(");
            var first = true;
            foreach (var parm in parameters)
            {
                if (!first)
                    sb.Append(", ");
                first = false;

                sb.Append(parm.ParameterType.Name);
            }
            sb.Append(")");

            return sb.ToString();
        }

        public override string ToString()
        {
            //return "TempoMethod: " + DeclaringType.Name + "." + Name;
            return Name;
        }

        public override sealed bool IsSpecialName
        {
            get
            {
                return GetIsSpecialName();
            }
        }


        virtual public bool IsGenericMethod
        {
            get { return false; }
        }

        virtual public TypeViewModel[] GetGenericArguments()
        {
            return null;
        }

        string _restrictionName;
        public override string RestrictionName
        {
            get
            {
                if (_restrictionName == null)
                {
                    _restrictionName = "";

                    var sb = new StringBuilder();
                    sb.Append(this.DeclaringType.FullName);
                    sb.Append(".");
                    sb.Append(this.Name);
                    sb.Append("`");
                    sb.Append(this.Parameters.Count);

                    RestrictedApiInfo info;
                    if (RestrictedApiList.RestrictedApis.TryGetValue(sb.ToString(), out info))
                        _restrictionName = info.Restriction;
                    else
                        _restrictionName = DeclaringType.RestrictionName;
                }

                return _restrictionName;
            }
        }


        IList<MethodViewModel> _overloads = null;
        public IList<MethodViewModel> Overloads
        {
            get
            {
                if (AbiName == string.Empty)
                    return null;

                if (_overloads == null)
                {
                    _overloads = new List<MethodViewModel>();
                    foreach (var method in DeclaringType.GetMethods(Name))
                    {
                        if (method != this)
                        {
                            _overloads.Add(method);
                        }
                    }
                }

                return _overloads;
            }
        }

        string _abiName = null;
        public string AbiName
        {
            get
            {
                if (_abiName == null)
                {
                    _abiName = "";
                    foreach (var attr in this.CustomAttributes)
                    {
                        if (attr.Name == "OverloadAttribute")
                        {
                            _abiName = attr.ConstructorArguments[0].Value as string;
                        }

                    }
                }

                return _abiName;
            }
        }

        public abstract bool IsFinal { get; }
        public abstract bool GetIsSpecialName();

        public abstract System.Reflection.MethodAttributes Attributes { get; }
        abstract public bool IsPrivate { get; }

        TypeViewModel _returnType = null;
        protected abstract TypeViewModel CreateReturnType();
        override public TypeViewModel ReturnType
        {
            get
            {
                if (_returnType == null)
                {
                    _returnType = CreateReturnType();
                    //_returnType = ReflectionTypeViewModel.GetFromCache(MethodInfo.ReturnType);
                }
                return _returnType;
            }
        }

        IList<ParameterViewModel> _parameters = null;
        bool _parametersFetched = false;
        protected abstract IList<ParameterViewModel> CreateParameters();
        public IList<ParameterViewModel> Parameters
        {
            get
            {
                if (!_parametersFetched)
                {
                    _parameters = CreateParameters();
                    _parametersFetched = true;
                    //var q = from p in MethodInfo.GetParameters()
                    //        select new ParameterViewModel(p);
                    //_parameters = q.ToList();
                    //_parametersFetched = true;
                }
                return _parameters;
            }

        }
    }

    abstract public class FieldViewModel : MemberViewModelBase
    {
        public override MemberKind MemberKind { get { return MemberKind.Field; } }
        public override MyMemberTypes MemberType { get { return MyMemberTypes.Field; } } //(MyMemberTypes)FieldInfo.MemberType; } }

        public override bool IsField => true;

        public override string ToString()
        {
            //return "TempoField: " + DeclaringType.Name + "." + Name;
            return Name;
        }

        // Helper to help with sort order. Sort enum fields by value, otherwise don't sort
        // (because in C++ the order matters).
        public string NameForSorting()
        {
            if (this.DeclaringType.IsEnum)
                return String.Format("{0:8X}", this.RawConstantValue);
            else
                // Don't sort struct fields; order matters
                return string.Empty; // field.Name;
        }

        public bool IsDependencyPropertyField
        {
            get { return IsDependencyPropertyFieldHelper(this, this.FieldType); }
        }


        public override string MsdnRelativePath
        {
            get
            {
                if (DeclaringType.IsEnum)
                    return DeclaringType.FullName;
                else
                    return DeclaringType.FullName + "." + Name;
            }
        }

        string _fullName = null;
        public override string FullName
        {
            get
            {
                if (_fullName == null)
                {
                    var sb = new StringBuilder();
                    sb.Append(DeclaringType.FullName);
                    sb.Append("." + Name);

                    _fullName = sb.ToString();
                }
                return _fullName;
            }
        }

        // bugbug:  Why is in MemberViewModelBase and in subclasses?
        string _contract = null;
        public override string Contract
        {
            get
            {
                if (_contract == null)
                {
                    _contract = TypeViewModel.GetContractFromAttributesWithNetReflectionHack(CustomAttributes);
                    if (string.IsNullOrEmpty(_contract))
                        _contract = DeclaringType.Contract;
                }

                return _contract;
            }
        }


        public override bool IsSealed
        {
            get { return true; }
        }

        abstract public bool IsLiteral { get; }
        abstract public object RawConstantValue { get; }

        public string RawConstantValueString
        {
            get
            {
                if (DeclaringType.IsFlagsEnum)
                    return $"0x{RawConstantValue:X}";
                else
                    return $"{RawConstantValue}";
            }
        }


        abstract public bool IsPrivate { get; }

        public abstract FieldAttributes Attributes { get; }
        public bool IsConst => Attributes.HasFlag(FieldAttributes.Literal);

        public override sealed bool IsSpecialName
        {
            get { return GetIsSpecialName(); }
        }

        abstract public bool GetIsSpecialName();

        public abstract bool IsInitOnly { get; }


        abstract protected bool CheckIsStatic();
        public override bool IsStatic
        {
            get
            {
                if (DeclaringType.IsEnum)
                    return false;
                else
                    return CheckIsStatic();
            }
        }

        abstract protected TypeViewModel GetTypeFromCache(TypeViewModel typeViewModel);
        abstract public TypeViewModel FieldType { get; }

        //public override bool IsAbstract
        //{
        //    get { throw new NotImplementedException(); }
        //}
        public override bool IsVirtual
        {
            get { return false; }
        }

        public override string Version
        {
            get
            {
                return base.Version;
            }
        }

        const string enumDescriptionHeader = "## -enum-fields";
        const string structDescriptionHeader = "## -struct-fields";

        // Get the API description from docs.microsoft.com for this field.
        // (Fields are a special case because they don't get their own page.)
        async public override Task<string> GetApiDescriptionAsync()
        {
            // Fields don't get a special page in DMC, instead they're described in sections within
            // the type page (struct or enum). So for fields, get the type page, and find the field description.

            /*
             Enum example:
                 ## -enum-fields
                 ### -field Visible:0
                 Display the element.

                 ### -field Collapsed:1
                 Do not display the element, and do not reserve space for it in layout.

             Struct example:
                 ## -struct-fields

                 ### -field A
                 Gets or sets the **sRGB** alpha channel value of the color.
    
                 ### -field R
                 Gets or sets the **sRGB** red channel value of the color.
             */

            // This code path and the callees could be more robust but it would be a bunch more code, and have the same result as a catch in the
            // end. So ... catch everything
            var description = new StringBuilder();
            try
            {
                var isEnum = this.DeclaringType.IsEnum;
                var isStruct = this.DeclaringType.IsStruct;

                // Get the whole type page from GitHub
                var reader = await this.DeclaringType.GetApiDocPageAsync();

                // Find the L2 section that describes all the fields
                var found = false;
                while (!found)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    if (isEnum && line.StartsWith(enumDescriptionHeader)
                        || isStruct && line.StartsWith(structDescriptionHeader))
                    {
                        found = true;
                    }
                }

                if (!found)
                {
                    // Couldn't find any of the fields
                    return null;
                }

                // Find the L3 section for this field, example:
                // ### -field Collapsed:1

                found = false;
                var fieldName = $"### -field {this.Name}";
                while (!found)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        break;
                    }

                    if (line.StartsWith(fieldName))
                    {
                        found = true;
                    }
                }
                if (!found)
                {
                    // Couldn't find the field
                    return null;
                }

                // The rest is the description for this field

                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null || line.StartsWith("#"))
                    {
                        break;
                    }

                    description.Append(line);
                }
            }
            catch (Exception e)
            {
                // Probably a 404
                Debug.Assert(e is WebException);
            }


            return MarkdownToPlainText(description.ToString()).Trim();
        }

        virtual public string Symbol
        {
            get
            {
                return string.Empty;
            }
        }

        public virtual bool IsCompilerGenerated { get { return false; } }
    }

    abstract public class ConstructorViewModel : MemberViewModelBase // (MemberViewModel : BaseViewModel)
    {
        public override MemberKind MemberKind { get { return MemberKind.Constructor; } }

        public override bool IsConstructor => true;

        bool? _isOverloaded = null;
        override public bool IsOverloaded
        {
            get
            {
                if (_isOverloaded == null)
                {
                    var constructors = DeclaringType.Constructors;
                    _isOverloaded = constructors.Count > 1;
                }

                return _isOverloaded == true;
            }
        }

        async protected override Task<string> GetGitUrlFilenameAsync(string baseUrl)
        {
            return await MethodViewModel.GetGitUrlFilenameAsyncHelper(
                baseUrl,
                this.DeclaringType,
                this.DeclaringType.Name,
                this.Parameters);
        }

        virtual public bool IsGenericMethod
        {
            get { return false; }
        }

        virtual public TypeViewModel[] GetGenericArguments()
        {
            return null;
        }

        public override string MsdnRelativePath
        {
            get
            {
                return DeclaringType.FullName + "." + DeclaringType.Name;
            }
        }

        public abstract MethodAttributes Attributes { get; }

        abstract public bool IsFinal { get; }

        abstract public bool IsPrivate { get; }
        //abstract public bool IsSpecialName { get; }

        string _prettyName = null;
        public override string PrettyName
        {
            get
            {
                if (_prettyName == null)
                {
                    var sb = new StringBuilder();
                    sb.Append(DeclaringType.PrettyName);
                    sb.Append(" ");
                    sb.Append(MethodViewModel.GetParameterSignature(Parameters));

                    _prettyName = sb.ToString();
                }

                return _prettyName;
            }
        }


        string _fullName = null;
        public override string FullName
        {
            get
            {
                if (_fullName == null)
                {
                    var sb = new StringBuilder();
                    sb.Append(DeclaringType.FullName);
                    sb.Append(".");
                    sb.Append(DeclaringType.Name);
                    sb.Append(MethodViewModel.GetParameterSignature(Parameters));

                    _fullName = sb.ToString();
                }

                return _fullName;
            }
        }


        IList<ParameterViewModel> _parameters = null;
        bool _parametersCalculated = false;
        abstract protected IList<ParameterViewModel> CreateParameters();
        public IList<ParameterViewModel> Parameters
        {
            get
            {
                if (!_parametersCalculated)
                {
                    _parameters = CreateParameters();
                    _parametersCalculated = true;

                    //var q = from p in ConstructorInfo.GetParameters()
                    //        select new ParameterViewModel(p);
                    //_parameters = q.ToList();
                }
                return _parameters;
            }
        }


    }
}
