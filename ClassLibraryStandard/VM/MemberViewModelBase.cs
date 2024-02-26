using CommonLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{


    // Summary:
    //     Marks each type of member that is defined as a derived class of MemberInfo.
    [Flags]
    public enum MyMemberTypes
    {
        Constructor = 1,
        Event = 2,
        Field = 4,
        Method = 8,
        Property = 16,
        TypeInfo = 32,
        Custom = 64,
        NestedType = 128,
        All = 191,
    }


    public class AttributeViewModel
    {

    }

    // PSHack is a workaround for a PowerShell issue: https://github.com/PowerShell/PowerShell/issues/11601
    // The issue is in a PowerShell Provider when the GetChildItems override is calling WriteItemObject, if it
    // passes objects of different types, only one can have a customer View definition. To workaround that, 
    // we only have one view definition, and it looks for this class, and displays the PSNameHack property.
    // So subclasses can override this and pick what is going to get displayed by default.
    abstract public class PSHack
    {
        public abstract string PSNameHack { get; }
    }

    abstract public partial class BaseViewModel : PSHack, INotifyPropertyChanged
    {
        // In PowerShell, by default show the PrettyName
        public override string PSNameHack => PrettyName;

        public virtual string PrettyName
        {
            get
            {
                if (Name.StartsWith("op_"))
                {
                    return "[implicit conversion operator]";
                }
                return Name;
            }
        }

        protected void RaisePropertyChanged(string name)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }

        static public bool CheckProtected(dynamic method)
        {
            return method.IsFamily
                   ||
                   method.IsFamilyAndAssembly
                   ||
                   method.IsFamilyOrAssembly && !method.IsAssembly;
        }

        abstract public bool IsStatic { get; }
        abstract public bool IsPublic { get; }

        abstract public bool IsInternal { get; }
        abstract public bool IsVirtual { get; }
        abstract public bool IsAbstract { get; }
        abstract public string Name { get; }

        virtual public string FlexibleName { get { return Name; } }

        abstract public bool IsFamily { get; }
        abstract public bool IsFamilyOrAssembly { get; }
        abstract public bool IsFamilyAndAssembly { get; }
        abstract public bool IsAssembly { get; }


        virtual public string RestrictionName
        {
            get
            {
                return null;
            }
        }

        public bool IsRestricted
        {
            get { return !string.IsNullOrEmpty(RestrictionName); }
        }


        abstract public MyMemberTypes MemberType { get; }

        abstract protected TypeViewModel GetDeclaringType();
        abstract public MemberKind MemberKind { get; }

        TypeViewModel _declaringType = null;

        public event PropertyChangedEventHandler PropertyChanged;

        public TypeViewModel DeclaringType
        {
            get
            {
                if (_declaringType == null)
                {
                    _declaringType = GetDeclaringType();
                }
                return _declaringType;
            }
        }

        // Technically only types have a namespace, but makes sense for all members to have it
        // Note that TypeVM.DeclaringType just returns itself
        public virtual string Namespace => DeclaringType.Namespace;

        abstract public string Version { get; }

        public string VersionFriendlyName
        {
            get
            {
                var version = this.Version;

                // ??
                if (this.DeclaringType.IsPhone && version == null)
                {
                    return " ";
                }
                else
                {
                    if (version != null && ReleaseInfo.VersionFriendlyNames.TryGetValue(version, out var friendlyName))
                    {
                        return friendlyName;
                    }
                    else if(this.DeclaringType.IsWindows)
                    {
                        return ReleaseInfo.FriendlyBuildNameFromUglyVersionName(version);
                    }
                    {
                        return version;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Base class for TypeViewModel and any of the member ViewModels, but not e.g. ParameterViewModel
    /// </summary>
    abstract public class MemberOrTypeViewModelBase : BaseViewModel
    {
        bool _isDeprecated = false;
        bool _isDeprecatedChecked = false;
        string _deprecationString = null;

        bool _versionChecked = false;
        string _version = null;


        // Determine if this member is an explicit interface implementation
        // (Supported in .Net, not in WinRT)
        public bool IsExplicitImplementation()
        {
            // bugbug: make IsPrivate a property
            dynamic m2 = this;
            if (!m2.IsPrivate)
                return false;

            if (this.Name.Substring(1).Contains("."))
            {
                var interfaceName = this.Name.Substring(0, this.Name.LastIndexOf('.'));
                var i = this.DeclaringType.GetInterface(interfaceName);
                if (i == null || i.IsNotPublic)
                {
                    return false;
                }

                return true; // explicit interface 
            }

            return false;
        }

        public static bool IsDependencyPropertyTypeName(string fullName)
        {
            return fullName == "System.Windows.DependencyProperty"
                || fullName == "Windows.UI.Xaml.DependencyProperty"
                || fullName == "Microsoft.UI.Xaml.DependencyProperty";
        }


        static public bool IsDependencyPropertyFieldHelper(MemberOrTypeViewModelBase member, TypeViewModel memberType)
        {
            // Field in WPF, Property in Xaml
            if (member.MemberKind != MemberKind.Field && member.MemberKind != MemberKind.Property)
            {
                return false;
            }

            // Must be of type DP
            if (!IsDependencyPropertyTypeName(memberType.FullName))
            {
                return false;
            }

            // Naming pattern is to end in "Property"
            var index = member.Name.IndexOf("Property");
            if (index == -1)
            {
                return false;
            }

            // Must be a property of the same name without the "Property" suffix
            var prop = member.DeclaringType.GetProperty(member.Name.Substring(0, index));
            if (prop == null)
            {
                return false;
            }

            return true;

        }


        public virtual bool TryGetVMProperty(string key, out object value)
        {
            value = null;
            return false;
        }

        public static string NormalizePropertyNameForQueries(string s)
        {
            // If a query asks for Name, they probably want PrettyName
            // (like "Foo()" rather than ".ctor")
            if (s == "Name")
            {
                s = "PrettyName";
            }

            return s;
        }



        public virtual bool IsUac { get { return false; } }

        abstract public string FullName { get; }

        // CLR projects e.g. Windows.UI.Xaml.Data.INotifyPropertyChanged to System.ComponentModel
        // This property gives you the original Windows name
        virtual public string UnprojectedFullName
        {
            get { return FullName; }
        }

        public virtual bool IsProperty => false;
        public virtual bool IsMethod => false;
        public virtual bool IsEvent => false;
        public virtual bool IsField => false;
        public virtual bool IsConstructor => false;

        // Define on this most base of classes so that you can e.g. check IsInterface on a property
        // to see if it's a property of an interface
        abstract public bool IsInterface { get; }
        abstract public bool IsEnum { get; }
        abstract public bool IsValueType { get; }
        abstract public bool IsClass { get; }
        abstract public bool IsStruct { get; }
        abstract public bool IsDelegate { get; }

        public bool IsType => IsInterface || IsEnum || IsClass || IsStruct || IsDelegate;
        public bool IsMember => !IsType;



        public string MemberPrettyName
        {
            get
            {
                if (this is TypeViewModel)
                    return PrettyName;
                else if (DeclaringType != null)
                    return $"{DeclaringType.PrettyName}.{PrettyName}";
                else
                    return PrettyName;
            }
        }

        string _apiDesignNotes = null;
        public virtual string ApiDesignNotes
        {
            get
            {
                if (_apiDesignNotes == null)
                {
                    _apiDesignNotes = string.Empty;

                    // There's a race here, but if it runs twice we'll get the same answer both times anyway
                    UpdateApiDesignNotes(); // async
                }
                return _apiDesignNotes;
            }
        }
        protected virtual void UpdateApiDesignNotes()
        {
            return;
        }
        public void SetApiDesignNotes(string notes)
        {
            _apiDesignNotes = notes;
            RaisePropertyChanged(nameof(ApiDesignNotes));
        }



        // Description from MSDN
        // Async means it will return "" when you first call it, then when it calculates the
        // value it will raise a property change notification
        string _apiDescription = null;
        public string ApiDescriptionAsync
        {
            get
            {
                if (_apiDescription == null)
                {
                    _apiDescription = "";
                    UpdateApiDescription();
                }

                return _apiDescription;
            }
        }

        async void UpdateApiDescription()
        {
            // Worried about unit tests hitting GitHub too hard and causing a lockout
            if (Settings.IsInUnitTest)
            {
                _apiDescription = "";
            }
            else
            {
                var helpText = await GetApiDescriptionAsync();
                _apiDescription = helpText;
            }

            RaisePropertyChanged(nameof(ApiDescriptionAsync));
        }

        // Get the description of this API from the GitHub for docs.microsoft.com. (This was easier than getting it from DMC itself.)
        async public virtual Task<string> GetApiDescriptionAsync()
        {
            // This code path and the callees could be more robust but it would be a bunch more code, and have the same result as a catch in the
            // end. So ... catch everything
            try
            {
                // Get the whole page from GitHub
                var reader = await GetApiDocPageAsync();
                if (reader == null)
                {
                    return "";
                }

                var description = new StringBuilder();

                // Look for the beginning of the description
                var found = false;
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        // End of file
                        break;
                    }

                    if (line == "## -description")
                    {
                        // Found the description section
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    return "";
                }

                // Read to the description
                var needNewline = false;
                var haveSeenText = false;
                while (true)
                {
                    var line = reader.ReadLine();
                    if (line == null)
                    {
                        // End of file
                        break;
                    }

                    if (line.Contains("##"))
                    {
                        // Beginning of the next section
                        break;
                    }

                    line = line.Trim();
                    if (line == string.Empty)
                    {
                        // A blank line means add a newline, unless we're at the top, or there were
                        // multiple blank lines (just done one newline for that). But don't add it
                        // yet because we might be at the end of the description.
                        if (haveSeenText)
                        {
                            needNewline = true;
                        }
                    }
                    else
                    {
                        haveSeenText = true;

                        if (needNewline)
                        {
                            // First text after a blank link, end the line and add a blank line
                            description.AppendLine("");
                            description.AppendLine("");
                        }

                        description.Append(line);
                        needNewline = false;
                    }
                }

                // Do a pseudo job of converting markdown to a plain string
                return MarkdownToPlainText(description.ToString());
            }
            catch (Exception e)
            {
                // Probably a 404
                Debug.Assert(e is WebException);
            }

            return "";
        }

        // Get this member/type's API page from GitHub
        async public Task<StringReader> GetApiDocPageAsync()
        {
            // Type example (note the difference between winrt-api and winui-api)
            // https://raw.githubusercontent.com/MicrosoftDocs/winrt-api/docs/windows.devices.smartcards/smartcardcryptographickeyattestationstatus.md
            // https://raw.githubusercontent.com/MicrosoftDocs/winui-api/docs/microsoft.ui.xaml.controls/treeview.md

            var winRTorWinUI = this.DeclaringType.Namespace.StartsWith("Microsoft.") ? "winapps-winrt-api" : "winrt-api";

            var baseUri = $@"https://raw.githubusercontent.com/MicrosoftDocs/{winRTorWinUI}/docs/{this.DeclaringType.Namespace.ToLower()}/";

            // Get the right filename for this member (depends on if this is a type, properyt, method, etc).
            var filename = await GetGitUrlFilenameAsync(baseUri);
            if (string.IsNullOrEmpty(filename))
            {
                return null;
            }

            // The final Uri to the doc page in GitHub
            var docPageGitUrl = $@"{baseUri}{filename}";

            return await OverridableHelpers.Instance.GetUriAsync(docPageGitUrl);
        }

        protected virtual Task<string> GetGitUrlFilenameAsync(string baseUrl)
        {
            return Task<string>.FromResult("");
        }


        enum HyperlinkStage
        {
            None = 0,
            Open = 1,
            Closed = 2,
            UriOpen = 3
        }

        enum BoldStage
        {
            None = 0,
            OneStar = 1,
            InBold = 2,
            ClosingBold = 3
        }

        // Strip markdown to plain text (very bare bones job).
        // This supports bold, hyperlinks, and bulleted lists. (Nesting is OK but overlapping is not, which
        // probably isn't legal anyway.)
        public static string MarkdownToPlainText(string original)
        {
            // Example from SecondaryAuthenticationFactorRegistrationResult class    
            /*
            Provides information about the result of a companion device registration.

            > [!NOTE]> This API is not available to all apps. Unless your developer account is specially provisioned by Microsoft 
            to use the **secondaryAuthenticationFactor** capability, calls to this API will fail. To apply 
            for approval, contact [cdfonboard@microsoft.com](mailto:cdfonboard@microsoft.com). For more information 
            on capabilities, see [App capability declarations](https://aka.ms/appcap). For an overview of the Companion 
            Device Framework, see the [Windows Unlock with companion (IoT) devices](https://docs.microsoft.com/windows/uwp/security/companion-device-unlock) 
            overview.             
            */

            // Don't understand the MD syntax here, but it's a common pattern in DMC, so short-circuiting it.
            original = original.Replace("> [!NOTE]> ", "! NOTE\r\n");

            // Keep track if we're in a hyperlink or bold range, and if so where in the range.
            HyperlinkStage hyperlinkStage = 0;
            var boldStage = BoldStage.None;

            char prevChar;
            char savePrevChar = (char)0;

            // This holds the stripped result. When we start a new range,
            // (bold or hyperlink), do a push, then put characters into the new
            // string while we search for the end of the range (or not, the first
            // character of the range could be a decoy).
            var stackingStringBuilder = new StackingStringBuilder();

            // Loop through the characters of the string looking for hyperlinks and bulleted lists.
            // For hyperlinks we have to go through stages form the open to close square brackets then parens.
            foreach (var c in original)
            {
                prevChar = savePrevChar;
                savePrevChar = c;

                switch (hyperlinkStage)
                {
                    // Not in a hyperlink (yet)
                    case HyperlinkStage.None:
                        if (c == '[')
                        {
                            // Now in a hyperlink
                            hyperlinkStage = HyperlinkStage.Open;
                            stackingStringBuilder.Push();
                            continue;
                        }

                        break;

                    // Seen the [, looking for a ]
                    case HyperlinkStage.Open:
                        if (c == ']')
                        {
                            // Now looking for an open paren
                            hyperlinkStage = HyperlinkStage.Closed;
                            
                            // No more processing of this character
                            continue;
                        }
                        break;

                    // Found the ], expecting a (
                    case HyperlinkStage.Closed:
                        if (c == '(')
                        {
                            // Now we're in the URI, so this really was a hyperlink.
                            // Append the temporary text (which doesn't include the brackets)
                            // to the  previous string (which could be the result or another temp
                            // string).
                            hyperlinkStage = HyperlinkStage.UriOpen;
                            var tempString = stackingStringBuilder.Current.ToString();
                            stackingStringBuilder.Pop();
                            stackingStringBuilder.Current.Append(tempString);
                            continue;
                        }
                        else
                        {
                            // Not a hyperlink after all. Replay the brackets and text we were saving

                            var tempString = stackingStringBuilder.Current.ToString();
                            stackingStringBuilder.Pop();

                            stackingStringBuilder.Current.Append('[');
                            stackingStringBuilder.Current.Append(tempString);
                            stackingStringBuilder.Current.Append(']');

                            hyperlinkStage = HyperlinkStage.None;
                            break;
                        }

                    // Looking for the end of the URI
                    case HyperlinkStage.UriOpen:
                        if (c == ')')
                        {
                            // Found it, we're done with the hyperlink
                            hyperlinkStage = HyperlinkStage.None;
                        }
                        continue;

                    default:
                        Debug.Assert(false);
                        break;

                }

                // Check for a bulleted list.
                if (c == '+'
                    && (prevChar == '\n' || prevChar == '\r'))
                {
                    // Add a new line before each bullet
                    stackingStringBuilder.Current.AppendLine("");
                }

                // Process bold
                if (c == '*')
                {
                    if (boldStage == BoldStage.InBold)
                    {
                        // This might be the end of the bold range
                        boldStage = BoldStage.ClosingBold;
                        continue; // No more processing on this char
                    }
                    else if (boldStage == BoldStage.ClosingBold)
                    {
                        // This is the end of the bold range (second '*')
                        // Put the text onto the lower string (could be the result
                        // string or could be another range).
                        boldStage = BoldStage.None;

                        var tempString = stackingStringBuilder.Current.ToString();
                        stackingStringBuilder.Pop();
                        stackingStringBuilder.Current.Append(tempString); // Put all the bolded text in the result

                        continue;
                    }
                    else if (boldStage == BoldStage.OneStar)
                    {
                        // We now have two '*' in a row, we're in a bold range
                        boldStage = BoldStage.InBold;
                        continue; // No more processing on this char
                    }
                    else
                    {
                        // Potential beginning of a bold stage
                        boldStage = BoldStage.OneStar;
                        stackingStringBuilder.Push();
                        continue; // No more processing on this char
                    }
                }
                else if (boldStage == BoldStage.OneStar)
                {
                    // This wasn't a bold range after all. Put back the '*' that was eaten and carry on
                    Debug.Assert(prevChar == '*');

                    stackingStringBuilder.Pop();
                    stackingStringBuilder.Current.Append('*');
                    boldStage = BoldStage.None;
                }
                else if (boldStage == BoldStage.ClosingBold)
                {
                    // That '*' we just saw is part of the bold range, not the end of it.
                    Debug.Assert(prevChar == '*');
                    stackingStringBuilder.Current.Append('*'); // PrevChar was ignored, add it now
                    boldStage = BoldStage.InBold;
                }

                stackingStringBuilder.Current.Append(c);
            }

            return stackingStringBuilder.Current.ToString();
        }



        bool? _isRemoteAsync = null;
        public virtual bool IsRemoteAsync
        {
            get
            {
                if (_isRemoteAsync == null)
                {
                    var attrs = this.CustomAttributes;
                    if (attrs != null)
                    {
                        foreach (var attr in attrs)
                        {
                            if (attr.FullName == "Windows.Foundation.Metadata.RemoteAsyncAttribute")
                                _isRemoteAsync = true;
                        }
                    }
                }

                return _isRemoteAsync == true;
            }
        }

        abstract public string MsdnRelativePath { get; }


        virtual public object Color
        {
            get { return null; }
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

                    if (RestrictedApiList.RestrictedApis.TryGetValue(this.DeclaringType.FullName + "." + this.Name, out info))
                    {
                        _restrictionName = info.Restriction;
                    }
                    else
                        _restrictionName = this.DeclaringType.RestrictionName;
                }

                return _restrictionName;
            }
        }


        int? _wordCount = null;
        public int WordCount
        {
            get
            {
                if (_wordCount == null)
                {
                    _wordCount = GetWords(Name).Count;
                }

                return (int)_wordCount;
            }
        }



        bool? _isFirstWordAVerb = null;
        public bool IsFirstWordAVerb
        {
            get
            {
                if (_isFirstWordAVerb == null)
                {
                    var firstWord = GetWords(Name).First();
                    _isFirstWordAVerb = Grammar.IsVerb(firstWord);
                }

                return _isFirstWordAVerb == true;
            }
        }


        public static IList<string> GetWords(string name)
        {
            var words = new List<string>();
            var word = new StringBuilder();
            char prev = ' ';
            foreach (var letter in name)
            {
                if (char.IsUpper(letter) && !char.IsUpper(prev))
                {
                    if (word.Length != 0)
                        words.Add(word.ToString());
                    word.Clear();
                }

                word.Append(letter);

                prev = letter;

                // Ignore generics
                if (letter == '`')
                    break;
            }

            if (word.Length != 0)
                words.Add(word.ToString());

            return words;
        }

        public virtual bool IsSpecialName
        {
            get { return false; }
        }

        int _matchGeneration = -1;
        public void SetMatchGeneration()
        {
            _matchGeneration = Manager.MatchGeneration;
        }

        public bool IsMatch
        {
            get { return _matchGeneration == Manager.MatchGeneration; }
        }



        public abstract bool IsSealed { get; }
        //public abstract bool IsDeprecated { get; }
        //public abstract string Deprecation { get; }

        public abstract TypeViewModel ReturnType { get; }

        public static MemberOrTypeViewModelBase FirstNotNull(
            MemberOrTypeViewModelBase propertyViewModel,
            MemberOrTypeViewModelBase eventViewModel,
            MemberOrTypeViewModelBase fieldViewModel,
            MemberOrTypeViewModelBase constructorViewModel,
            MemberOrTypeViewModelBase methodViewModel)
        {
            if (propertyViewModel != null)
                return propertyViewModel;
            else if (eventViewModel != null)
                return eventViewModel;
            else if (fieldViewModel != null)
                return fieldViewModel;
            else if (constructorViewModel != null)
                return constructorViewModel;
            else if (methodViewModel != null)
                return methodViewModel;
            else
                return null;

        }


        virtual public bool IsAdded
        {
            get
            {
                return this.Version != DeclaringType.Version;
            }
        }

        public string AddedInVersion
        {
            get
            {
                return "Added in version " + VersionFriendlyName;
            }
        }

        public string ModifierCodeString
        {
            get
            {
                var sb = new StringBuilder();
                if (IsStatic)
                    sb.Append("static ");

                if (IsPublic)
                    sb.Append("public ");
                else if (IsInternal)
                    sb.Append("internal ");
                else if (IsProtected)
                    sb.Append("protected ");
                else
                    sb.Append("private ");


                if (!IsSealed)
                {
                    if (IsAbstract)
                        sb.Append("abstract");
                    else if (IsVirtual)
                        sb.Append("virtual");
                }

                return sb.ToString().Trim();
            }
        }

        public void SetContract(string contract)
        {
            Debug.Assert(contract != null);
            _contract = contract;
        }


        string _contract = null;

        // bugbug:  Why is in MemberViewModelBase and in subclasses?
        virtual public string Contract
        {
            get
            {
                if (_contract == null)
                {
                    _contract = GetInterfaceMemberContract();
                }

                return _contract;
            }
        }

        virtual public void SetVersion(string version)
        {
            _versionChecked = true;
            Debug.Assert(version != null);
            _version = version;
        }

        public override string Version
        {
            get
            {
                if (!_versionChecked && ImportedApis.Initialized)
                {
                    _versionChecked = true;

                    if (DeclaringType.Namespace.StartsWith("Windows."))
                    {
                        if (ImportedApis.Win8.MemberExists(DeclaringType, this))
                            _version = "06020000";
                        else if (ImportedApis.WinBlue.MemberExists(DeclaringType, this))
                            _version = "06030000";
                        else if (ImportedApis.PhoneBlue.MemberExists(DeclaringType, this))
                            _version = "06030100";
                        else
                        {
                            if (DeclaringType.IsStruct)
                            {
                                _version = DeclaringType.Version;
                            }
                            else if (DeclaringType.IsEnum)
                            {
                                _version = GetVersionFromAttributes(this.CustomAttributes);
                                if (string.IsNullOrEmpty(_version))
                                    _version = DeclaringType.Version;
                            }
                            else
                            {
                                // bugbug
                                if (TypeViewModel.IsPhoneContractHack(this.DeclaringType))
                                    _version = "0A000000";
                                else
                                    _version = GetInterfaceMemberVersion();

                                if (_version.CompareTo(DeclaringType.Version) < 0)
                                {
                                    // An old interface was added to a new class.
                                    _version = DeclaringType.Version;
                                }
                            }
                        }

                    }
                    else
                    {
                        _version = DeclaringType.Version;
                    }
                }

                return _version;



                //if (!_versionChecked)
                //{
                //    if (DeclaringType.IsStruct)
                //    {
                //        _version = DeclaringType.Version;
                //    }
                //    else if (DeclaringType.IsEnum)
                //    {
                //        _version = GetVersionFromAttributes(this.CustomAttributes);
                //        if (_version == null)
                //            _version = DeclaringType.Version;
                //    }
                //    else
                //    {
                //        _version = GetInterfaceMemberVersion();
                //    }

                //    _versionChecked = true;

                //} // !_versinochecked

                //return _version;
            }
        }


        bool? _isPreview = null;
        public void SetIsPreview(bool value)
        {
            _isPreview = value;
        }
        public bool IsPreview
        {
            get
            {
                if (_isPreview == null)
                {
                    _isPreview = DeclaringType.IsWindows && UwpBuild == ReleaseInfo.PreviewBuildString;
                }
                return _isPreview == true;
            }
        }

        public void SetUwpBuild(string v)
        {
            _uwpBuild = v;
        }

        string _uwpBuild = null;
        public string UwpBuild
        {
            get
            {
                if (_uwpBuild == null)
                {
                    _uwpBuild = ReleaseInfo.FriendlyBuildNameFromUglyVersionName(Version);
                }

                return _uwpBuild;
            }
        }

        //string _previewBuildString = "(Prerelease)";


        public string IfExperimentalSaySo
        {
            get
            {
                if (IsExperimental)
                    return " [experimental]";
                else
                    return null;
            }
        }


        bool? _isExperimental = null;
        virtual public bool IsExperimental
        {
            get
            {
                if (_isExperimental == null)
                {
                    if (this.DeclaringType.IsExperimental)
                    {
                        // This handles the case of an implicit interface definition on an experimental type. For example:
                        //
                        //  [Experimental]
                        //  public class Foo : INotifyPropertyChanged
                        //
                        // Foo is going to have a PropertyChanged member, but it's not going to carry the [Experimental]
                        // attribute, because it's not (it's declared as part of INPC). But let's still mark this
                        // as IsExperimental, since you can't get to this member without going through the Experimental
                        // host class.

                        _isExperimental = true;
                    }
                    else
                    {

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

                        if (_isExperimental == null && !(this is TypeViewModel))
                        {
                            if (DeclaringType.IsStruct || DeclaringType.IsEnum)
                                _isExperimental = DeclaringType.IsExperimental;
                            else
                            {
                                // bugbug
                                if (TypeViewModel.IsPhoneContractHack(this.DeclaringType))
                                    _isExperimental = false;
                                else
                                    _isExperimental = CheckMemberInterfaceIsExperimental();
                            }
                        }

                        if (_isExperimental == null)
                        {
                            _isExperimental = false;
                        }
                    }
                }

                return _isExperimental == true;
            }
        }


        bool? _versionIsUnknown = null;
        public bool VersionIsUnknown
        {
            get
            {
                if (_versionIsUnknown == null)
                {
                    _versionIsUnknown = !ReleaseInfo.IsKnownVersion(Version);
                }

                return _versionIsUnknown == true;
            }
        }




        public bool IsDeprecated
        {
            get
            {
                if (!_isDeprecatedChecked)
                {
                    _isDeprecatedChecked = true;
                    _isDeprecated = GetIsDeprecated(out _deprecationString);
                }

                return _isDeprecated;
            }
        }

        /// <summary>
        /// Determine if this member is deprecated. This is virtual because properties require special handling
        /// </summary>
        /// <param name="deprecationString"></param>
        /// <returns></returns>
        internal virtual bool GetIsDeprecated(out string deprecationString)
        {
            var isDeprecated = false;
            deprecationString = "";

            // bugbug
            if (TypeViewModel.IsPhoneContractHack(this.DeclaringType))
            {
                return false;
            }

            var attrs = this.CustomAttributes;
            if (attrs != null)
            {
                foreach (var a in attrs)
                {
                    if (a.FullName == "Windows.Foundation.Metadata.DeprecatedAttribute")
                    {
                        isDeprecated = true;
                        var args = a.ConstructorArguments;
                        if (args != null && args.Count > 0)
                        {
                            deprecationString = args[0].Value as string;
                        }
                        else
                        {
                            deprecationString = "(Can't retrieve deprecation text)";
                        }

                        break;
                    }
                }
            }

            return isDeprecated;
        }

        public string Deprecation
        {
            get 
            {
                // Ensure the string
                _ = IsDeprecated;

                return _deprecationString; 
            }
        }


        protected string GetInterfaceMemberVersion()
        {
            var matchingInterface = GetMemberInterface();

            if (matchingInterface != null)
            {
                if (matchingInterface.Name == "IDisposable" && matchingInterface.Namespace == "System" ||
                    matchingInterface.Name == "IClosable" && matchingInterface.Namespace == "Windows.Foundation")
                {
                    return DeclaringType.Version;
                }
                else
                {
                    while (matchingInterface.IsGenericType && !matchingInterface.IsGenericTypeDefinition)
                    {
                        matchingInterface = matchingInterface.GetGenericTypeDefinition();
                    }

                    return matchingInterface.Version;
                }
            }
            else
                return DeclaringType.Version;
        }

        protected string GetInterfaceMemberContract()
        {
            var matchingInterface = GetMemberInterface();

            if (matchingInterface != null)
            {
                if (matchingInterface.Name == "IDisposable" && matchingInterface.Namespace == "System" ||
                    matchingInterface.Name == "IClosable" && matchingInterface.Namespace == "Windows.Foundation")
                {
                    // Guess that IDisposable has always been there. There's no way to tell from the 
                    // metadata when it was added
                    return DeclaringType.Contract;
                }
                else
                {
                    return matchingInterface.Contract;
                }
            }
            else
                return DeclaringType.Contract;
        }



        protected bool CheckMemberInterfaceIsExperimental()
        {
            var matchingInterface = GetMemberInterface();
            if (matchingInterface == null)
                return false;
            else
                return matchingInterface.IsExperimental;
        }

        public TypeViewModel GetMemberInterface()
        {
            IEnumerable<TypeViewModel> interfaces = null;
            TypeViewModel matchingInterface = null;

            var constructor = false;
            if (Name == ".ctor")
            {
                constructor = true;
                interfaces = DeclaringType.GetConstructorInterfaces();
            }
            else
            {
                interfaces = DeclaringType.GetAllInterfaces();
            }

            foreach (var iface in interfaces)
            {
                IEnumerable<MemberOrTypeViewModelBase> interfaceMembers = null;

                if (iface == null)
                    continue;

                if (constructor)
                    interfaceMembers = iface.Members;
                else
                {
                    var rawMembers = iface.RawMembers.ToList();
                    interfaceMembers = from member in rawMembers
                                       where Name == member.Name
                                       select member;
                }


                if (interfaceMembers != null && interfaceMembers.Count() != 0)
                {
                    foreach (var interfaceMember in interfaceMembers)
                    {
                        if (!constructor && interfaceMember.MemberType != MemberType)
                            continue;

                        if (interfaceMember.MemberType != MyMemberTypes.Method)
                        {
                            matchingInterface = iface;
                            break;
                        }
                        else
                        {
                            var interfaceMethod = interfaceMember as MethodViewModel;
                            var interfaceMethodParameters = interfaceMethod.Parameters;

                            IList<ParameterViewModel> thisMethodParameters = null;
                            if (this is MethodViewModel)
                                thisMethodParameters = (this as MethodViewModel).Parameters;
                            else
                                thisMethodParameters = (this as ConstructorViewModel).Parameters;

                            if ((interfaceMethodParameters == null || interfaceMethodParameters.Count == 0)
                                &&
                                (thisMethodParameters == null || thisMethodParameters.Count == 0))
                            {
                                matchingInterface = iface;
                                break;
                            }

                            if (interfaceMethodParameters.Count == thisMethodParameters.Count)
                            {
                                var parameterTypesMatch = true;
                                for (int i = 0; i < interfaceMethodParameters.Count; i++)
                                {
                                    // bugbug: FullName?
                                    if (interfaceMethodParameters[i].ParameterType.PrettyFullName
                                        != thisMethodParameters[i].ParameterType.PrettyFullName)
                                    {
                                        parameterTypesMatch = false;
                                        break;
                                    }
                                }

                                if (parameterTypesMatch)
                                {
                                    matchingInterface = iface;
                                    break;
                                }
                            }
                        }
                    }

                    if (matchingInterface != null)
                        break;

                }

            } // foreach i in all interfaces

            return matchingInterface;
        }



        public string ExclusiveToInterface
        {
            get
            {
                var type = GetMemberInterface();
                if (type == null)
                    return "";
                else
                    return type.Name;
            }
        }



        public override string FlexibleName
        {
            get
            {
                if (Manager.Settings.IsGrouped)
                {
                    var typeViewModel = this as TypeViewModel;
                    if (typeViewModel == null)
                    {
                        if (this is ConstructorViewModel)
                        {
                            var c = this as ConstructorViewModel;
                            var sb = new StringBuilder();

                            var name = DeclaringType.Name;
                            var grav = name.IndexOf('`');
                            if (grav != -1)
                                name = name.Substring(0, grav);

                            sb.Append(name + "(");
                            if (c.Parameters.Count == 0)
                                sb.Append(" ");
                            else
                            {
                                for (int i = 0; i < c.Parameters.Count; i++)
                                {
                                    if (i != 0)
                                        sb.Append(", ");
                                    sb.Append(c.Parameters[i].ParameterType.PrettyName);
                                }
                            }

                            sb.Append(")");

                            return sb.ToString();
                        }
                        else if (this is FieldViewModel && this.DeclaringType.IsEnum)
                        {
                            return $"{Name} ({(this as FieldViewModel).RawConstantValueString})";
                        }
                        else
                            return Name;
                    }
                    else
                    {
                        return typeViewModel.PrettyName;
                    }
                }
                else
                {
                    if (Manager.Settings.Flat)
                    {
                        if (this is TypeViewModel)
                            return (this as TypeViewModel).Namespace + "." + (this as TypeViewModel).PrettyName;
                        //(this as TypeViewModel).FullName;
                        else
                            return DeclaringType.Namespace + "." + DeclaringType.PrettyName + "." + Name;
                    }
                    else
                        return DeclaringType.PrettyName + "." + Name;
                }
            }
        }

    }



}
