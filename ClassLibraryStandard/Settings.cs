using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using Microsoft.Windows.Controls.Ribbon;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Reflection;
using System.Collections;
using System.Runtime.CompilerServices;
using CommonLibrary;

namespace Tempo
{


    public class Settings : INotifyPropertyChanged
    {
        public static Settings DefaultValues;
        static Settings()
        {
            // This has to be here, rather than an initializer, to ensure that other static fields get initialized
            // before the constructor runs
            DefaultValues = new Settings();
        }

        public Settings Clone()
        {
            return MemberwiseClone() as Settings;
        }

        bool? _isDefault = true;

        static public bool IsInUnitTest = false;

        // bugbug: This is wierd, what's it used for?
        public bool IsDefault
        {
            set { _isDefault = value; }
            get
            {
                if (_isDefault == null)
                {
                    _isDefault = AreAllMembersDefault();
                }

                return _isDefault == true;
            }
        }

        // Do all work synchrnously, don't fork threads.
        static public bool SyncMode { get; set; } = false;

        public bool AreAllMembersDefault()
        {
            return AreMembersEqual(DefaultValues);
        }

        public bool AreAllMembersDefault(params string[] skips)
        {
            return AreMembersEqual(DefaultValues, skips);
        }

        public bool AreMembersEqual(Settings other, params string[] skips)
        {
            var properties = other.GetType().GetTypeInfo().DeclaredProperties;
            foreach( var prop in properties)
            {
                if (!prop.CanWrite)
                {
                    continue;
                }

                // Ignore these properties when checking if settings are default or not;
                // they don't affect search
                if (prop.Name == nameof(IsDefault) 
                    || prop.Name == nameof(IsPreview)
                    || prop.Name.StartsWith("Language"))
                {
                    continue;
                }

                var isSkipProperty = false;
                foreach(var skip in skips)
                {
                    if (prop.Name == skip)
                    {
                        isSkipProperty = true;
                        break;
                    }
                }
                if (isSkipProperty)

                    continue;

                var val1 = prop.GetValue(this);
                var val2 = prop.GetValue(other);

                var equal = false;
                if (val1 == null)
                    equal = val1 == val2;
                else
                    equal = val1.Equals(val2);

                if (!equal)
                    return false;
            }

            return true;
        }


        public Settings() 
        {
            //if (_convertForJS)
            //    _isDefault = false;
            //else
            //    _isDefault = true;
        }

        public void NotifyChange(bool isReset = false, [CallerMemberName] string name = null)
        {
            DebugLog.Append($"Settings change ({isReset}, {name})");
            _isDefault = isReset ? (bool?)true : null;

            // Instance changed event (for x:Bind)
            if (PropertyChanged != null)
            {
                var args = new PropertyChangedEventArgs(name);
                PropertyChanged.Invoke(this, args);
            }

            // Static changed event (because there's only one instance of Settings)
            if (Changed != null)
            {
                var args = new PropertyChangedEventArgs(name);
                Changed.Invoke(this, args);
            }

            Manager.SettingsHack.Reset();
        }

        // Static version of PropertyChanged event
        static public event EventHandler<PropertyChangedEventArgs> Changed;


        string _namespace = null;
        public string Namespace
        {
            get { return _namespace; }
            set { _namespace = value; NotifyChange(); }
        }

        IList _contract;
        public IList Contract
        {
            get { return _contract; }
            set { _contract = value; NotifyChange(); }
        }


        static bool _languageJSStatic = false;
        bool _languageJS = _languageJSStatic;
        public bool LanguageJS
        {
            get { return _languageJS; }
            set 
            { 
                _languageJS = _languageJSStatic = value;
                _isWebHostHiddenEnabled = !_languageJS;
                RaiseViewChange(); 
                NotifyChange(); 
            }
        }
        public static event EventHandler ViewChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        static public void RaiseViewChange()
        {
            if (ViewChanged != null)
                ViewChanged(null, null);
        }


        static bool _languageCSStatic = false;
        bool _languageCS = _languageCSStatic;

        public bool LanguageCS
        {
            get { return _languageCS; }
            set
            {
                // Reduce spurious change notifications
                if (_languageCS != value)
                {
                    _languageCS = _languageCSStatic = value;
                    RaiseViewChange();
                    NotifyChange();
                }
            }
        }

        static bool _languageCPPStatic = false;
        bool _languageCPP = _languageCPPStatic;

        public bool LanguageCPP
        {
            get { return _languageCPP; }
            set
            {
                // Reduce spurious change notifications
                if (_languageCPP != value)
                {
                    _languageCPP = _languageCPPStatic = value;
                    RaiseViewChange(); // bugbug: why two events?
                    NotifyChange();
                }
            }
        }

        static bool _isPreviewStatic = false; // So that master reset doesn't change this
        bool _isPreview = _isPreviewStatic;
        public bool IsPreview => true; // True to get MR version, use that everywhere now
        //{
        //    get { return _isPreview; }
        //    set
        //    {
        //        _isPreview = _isPreviewStatic = value;

        //        RaiseViewChange();
        //        NotifyChange();
        //    }
        //}


        bool _ignoreAllSettings = false;
        public bool IgnoreAllSettings
        {
            get { return _ignoreAllSettings; }
            set { _ignoreAllSettings = value; }
        }

        bool? _isFinal = null;
        public bool? IsFinal
        {
            get { return _isFinal; }
            set { _isFinal = value; NotifyChange(); }
        }

        bool _isGrouped = true;
        public bool IsGrouped
        {
            get 
            { 
                return _isGrouped && !Flat; 
            }
            set { _isGrouped = value; NotifyChange(); }
        }

        bool _namespaceInclusive = true;
        public bool NamespaceInclusive
        {
            get { return _namespaceInclusive; }
            set { _namespaceInclusive = value; NotifyChange(); }
        }

        bool _namespaceExclusive = false;
        public bool NamespaceExclusive
        {
            get { return _namespaceExclusive; }
            set { _namespaceExclusive = value; NotifyChange(); }
        }

        bool _namespaceExcluded = false;
        public bool NamespaceExcluded
        {
            get { return _namespaceExcluded; }
            set { _namespaceExcluded = value; NotifyChange(); }
        }


        bool? _isOverride = null;
        public bool? IsOverride
        {
            get { return _isOverride; }
            set { _isOverride = value; NotifyChange(); }
        }

        // Compare the current typeset to a baseline typeset (for finding deltas)
        bool? _compareToBaseline = null;
        public bool? CompareToBaseline
        {
            get { return _compareToBaseline; }
            set { _compareToBaseline = value; NotifyChange(); }
        }

        bool? _isAsync = null;
        public bool? IsAsync
        {
            get { return _isAsync; }
            set { _isAsync = value; NotifyChange(); }
        }

        bool? _isRemoteAsync = null;
        public bool? IsRemoteAsync
        {
            get { return _isRemoteAsync; }
            set { _isRemoteAsync = value; NotifyChange(); }
        }

        bool? _isFirstWordAVerb = null;
        public bool? IsFirstWordAVerb
        {
            get { return _isFirstWordAVerb; }
            set { _isFirstWordAVerb = value; NotifyChange(); }
        }

        bool? _isRestricted = null;
        public bool? IsRestricted
        {
            get { return _isRestricted; }
            set { _isRestricted = value;  NotifyChange(); }
        }

        public bool? _hasApiDesignNotes = null;
        public bool? HasApiDesignNotes
        {
            get { return _hasApiDesignNotes; }
            set { _hasApiDesignNotes = true; NotifyChange(); }
        }


        bool _filterOnDeclaringType = false;
        public bool FilterOnDeclaringType
        {
            get { return _filterOnDeclaringType; }
            set { _filterOnDeclaringType = value; NotifyChange(); }
        }

        bool _caseSensitive = false;
        public bool CaseSensitive
        {
            get { return _caseSensitive; }
            set { _caseSensitive = value; NotifyChange(); }
        }

        bool _internalInterfaces = false;
        public bool InternalInterfaces
        {
            get { return _internalInterfaces; }
            set 
            { 
                _internalInterfaces = value;

                // Special notification for this property changing, so that ViewModels can
                // clear caches to be set up to recalculate next time
                ReportSettingsInternalChanged();

                NotifyChange();  
            }
        }

        public void ReportSettingsInternalChanged()
        {
            TypeViewModel.ReportSettingsInternalChanged();
        }

        bool? _dualApi = null;
        public bool? DualApi
        {
            get { return _dualApi; }
            set { _dualApi = value; NotifyChange(); }
        }

        bool? _nameAndNamespaceConflict = null;
        public bool? NameAndNamespaceConflict
        {
            get { return _nameAndNamespaceConflict; }
            set { _nameAndNamespaceConflict = value; NotifyChange(); }
        }

        bool? _oneWordName = null;
        public bool? OneWordName
        {
            get { return _oneWordName; }
            set { _oneWordName = value; NotifyChange(); }
        }


        bool? _isMuse = null;
        public bool? IsMuse
        {
            get { return _isMuse; }
            set { _isMuse = value; NotifyChange(); }
        }

        bool? _isUac = null;
        public bool? IsUac
        {
            get { return _isUac; }
            set { _isUac = value; NotifyChange(); }
        }

        bool? _typeInWindows = null;
        public bool? TypeInWindows
        {
            get { return _typeInWindows; }
            set { _typeInWindows = value; NotifyChange(); }
        }



        bool? _typeInPhone= null;
        public bool? TypeInPhone
        {
            get { return _typeInPhone; }
            set { _typeInPhone = value; NotifyChange(); }
        }



        bool? _typeInCustom = null;
        public bool? TypeInCustom
        {
            get { return _typeInCustom; }
            set { _typeInCustom = value; NotifyChange(); }
        }

        bool? _typeInWpf = null;
        public bool? TypeInWpf
        {
            get { return _typeInWpf; }
            set { _typeInWpf = value; NotifyChange(); }
        }

        bool? _hasMatchingPropertyAndSetMethod = null;
        public bool? HasMatchingPropertyAndSetMethod
        {
            get { return _hasMatchingPropertyAndSetMethod;  }
            set { _hasMatchingPropertyAndSetMethod = value; NotifyChange(); }
        }

        bool? _duplicateEnumValues = null;
        public bool? DuplicateEnumValues
        {
            get { return _duplicateEnumValues; }
            set { _duplicateEnumValues = value; NotifyChange(); }
        }

        bool? _conflictingOverrides = null;
        public bool? ConflictingOverrides
        {
            get { return _conflictingOverrides; }
            set { _conflictingOverrides = value; NotifyChange(); }
        }

        bool? _isFlagsEnum = null;
        public bool? IsFlagsEnum
        {
            get { return _isFlagsEnum; }
            set { _isFlagsEnum = value; NotifyChange(); }
        }

        bool? _memberInWindows = null;
        public bool? MemberInWindows
        {
            get { return _memberInWindows; }
            set { _memberInWindows = value; NotifyChange(); }
        }

        bool? _memberInCustom = null;
        public bool? MemberInCustom
        {
            get { return _memberInCustom; }
            set { _memberInCustom = value; NotifyChange(); }
        }

        bool? _memberInWpf = null;
        public bool? MemberInWpf
        {
            get { return _memberInWpf; }
            set { _memberInWpf = value; NotifyChange(); }
        }

        bool? _memberInPhone = null;
        public bool? MemberInPhone
        {
            get { return _memberInPhone; }
            set { _memberInPhone = value; NotifyChange(); }
        }



        bool? _deprecated = null;
        public bool? Deprecated
        {
            get { return _deprecated; }
            set { _deprecated = value; NotifyChange(); }
        }

        bool? _propertyNameMatchesTypeName = null;
        public bool? PropertyNameMatchesTypeName
        {
            get { return _propertyNameMatchesTypeName; }
            set { _propertyNameMatchesTypeName = value; NotifyChange(); }
        }

        bool? _returnsHostType = null;
        public bool? ReturnsHostType
        {
            get { return _returnsHostType; }
            set { _returnsHostType = value; NotifyChange(); }
        }

        bool? _customInParameters = null;
        public bool? CustomInParameters
        {
            get { return _customInParameters; }
            set { _customInParameters = value; NotifyChange(); }
        }

        bool? _markerInterfaces = null;
        public bool? MarkerInterfaces
        {
            get { return _markerInterfaces; }
            set { _markerInterfaces = value; NotifyChange(); }
        }

        bool? _untypedArgs = null;
        public bool? UntypedArgs
        {
            get { return _untypedArgs; }
            set { _untypedArgs= value; NotifyChange(); }
        }

        bool _unobtainableType = false;
        public bool UnobtainableType
        {
            get { return _unobtainableType; }
            set { _unobtainableType = value; NotifyChange(); }
        }

        bool _duplicateTypeName = false;
        public bool DuplicateTypeName
        {
            get { return _duplicateTypeName; }
            set { _duplicateTypeName = value; NotifyChange(); }
        }

        bool? _isAbstract = null;
        public bool? IsAbstract
        {
            get { return _isAbstract; }
            set { _isAbstract = value; NotifyChange(); }
        }


        bool? _isExplicit = null;
        public bool? IsExplicit
        {
            get { return _isExplicit; }
            set { _isExplicit = value; NotifyChange(); }
        }


        bool? _hasOutParameter = null;
        public bool? HasOutParameter
        {
            get { return _hasOutParameter; }
            set { _hasOutParameter = value; NotifyChange(); }
        }

        bool? _hasAddedSetter = null;
        public bool? HasAddedSetter
        {
            get { return _hasAddedSetter; }
            set { _hasAddedSetter = value; NotifyChange(); }
        }

        bool? _hasMultipleOutParameters = null;
        public bool? HasMultipleOutParameters
        {
            get { return _hasMultipleOutParameters; }
            set { _hasMultipleOutParameters = value;  NotifyChange(); }
        }


        bool? _implementsInternalInterface = null;
        public bool? ImplementsInternalInterface
        {
            get { return _implementsInternalInterface; }
            set { _implementsInternalInterface = value; NotifyChange(); }
        }

        bool? _hasInParameter = null;
        public bool? HasInParameter
        {
            get { return _hasInParameter; }
            set { _hasInParameter = value; NotifyChange(); }
        }

        bool? _hasAgileParameter = null;
        public bool? HasAgileParameter
        {
            get { return _hasAgileParameter; }
            set { _hasAgileParameter = value; NotifyChange(); }
        }

        bool? _hasInterfaceParameter = null;
        public bool? HasInterfaceParameter
        {
            get { return _hasInterfaceParameter; }
            set { _hasInterfaceParameter = value; NotifyChange(); }
        }

        bool? _hasDelegateParameter = null;
        public bool? HasDelegateParameter
        {
            get { return _hasDelegateParameter; }
            set { _hasDelegateParameter = value; NotifyChange(); }
        }

        bool? _hasMutableParameter = null;
        public bool? HasMutableParameter
        {
            get { return _hasMutableParameter; }
            set { _hasMutableParameter = value; NotifyChange(); }
        }

        bool? _hasRefParameter = null;
        public bool? HasRefParameter
        {
            get { return _hasRefParameter; }
            set { _hasRefParameter = value; NotifyChange(); }
        }

        bool? _hasReturnValue = null;
        public bool? HasReturnValue
        {
            get { return _hasReturnValue; }
            set { _hasReturnValue = value; NotifyChange();  }
        }

        bool? _hasPrimitiveReturnValue = null;
        public bool? HasPrimitiveReturnValue
        {
            get { return _hasPrimitiveReturnValue; }
            set { _hasPrimitiveReturnValue = value; NotifyChange(); }
        }

        bool? _isAddedMember = null;
        public bool? IsAddedMember
        {
            get { return _isAddedMember; }
            set { _isAddedMember = value; NotifyChange(); }
        }

        bool _filterOnFullName = true;
        public bool FilterOnFullName
        {
            get { return _filterOnFullName || _flat; }
            set { _filterOnFullName = value; NotifyChange(); }
        }

        bool _filterOnReturnType = true;
        public bool FilterOnReturnType
        {
            get { return _filterOnReturnType && !_flat; }
            set { _filterOnReturnType = value; NotifyChange(); }
        }

        bool _flat = false;
        public bool Flat
        {
            get { return _flat; }
            set { _flat = value; NotifyChange(); }
        }

        public bool NotFlat
        {
            get { return !Flat; }
        }


        bool _filterOnName = true;
        public bool FilterOnName
        {
            get { return _filterOnName; }
            set { _filterOnName = value; NotifyChange(); }
        }

        bool _filterOnBaseType = true;
        public bool FilterOnBaseType
        {
            get { return _filterOnBaseType && !_flat; }
            set { _filterOnBaseType = value; NotifyChange(); }
        }

        // Used to default this to false because in .Net Reflection it was really slow
        bool _filterOnAttributes = true;
        public bool FilterOnAttributes
        {
            get { return _filterOnAttributes; }
            set { _filterOnAttributes = value; NotifyChange(); }
        }

        bool _filterOnParameters = true;
        public bool FilterOnParameters
        {
            get { return _filterOnParameters && !_flat; }
            set { _filterOnParameters = value; NotifyChange(); }
        }


        //bool _showProperties = true;
        //public bool ShowProperties
        //{
        //    get { return _showProperties; }
        //    set { _showProperties = value; Reset(); }
        //}

        bool? _isActivatable = null;
        public bool? IsActivatable
        {
            get { return _isActivatable; }
            set { _isActivatable = value; NotifyChange(); }
        }

        public bool ShowTypes
        {
            get { return MemberKind == MemberKind.Type || MemberKind == MemberKind.Any; }
        }


        bool? _isStatic = null;
        public bool? IsStatic
        {
            get { return _isStatic; }
            set { _isStatic = value; NotifyChange(); }
        }

        bool? _hasDefaultConstructor = null;
        public bool? HasDefaultConstructor
        {
            get { return _hasDefaultConstructor; }
            set { _hasDefaultConstructor = value; NotifyChange(); }
        }

        bool? _hasNonDefaultConstructor = null;
        public bool? HasNonDefaultConstructor
        {
            get { return _hasNonDefaultConstructor; }
            set { _hasNonDefaultConstructor = value; NotifyChange(); }
        }


        bool? _hasProtectedConstructors = null;
        public bool? HasProtectedConstructors
        {
            get { return _hasProtectedConstructors; }
            set { _hasProtectedConstructors = value; NotifyChange(); }
        }


        bool? _hasPublicConstructors = null;
        public bool? HasPublicConstructors
        {
            get { return _hasPublicConstructors; }
            set { _hasPublicConstructors = value; NotifyChange(); }
        }


        bool? _hasStaticConstructor = null;
        public bool? HasStaticConstructor
        {
            get { return _hasStaticConstructor; }
            set { _hasStaticConstructor = value; NotifyChange(); }
        }

        bool? _hasBaseType = null;
        public bool? HasBaseType
        {
            get { return _hasBaseType; }
            set { _hasBaseType = value; NotifyChange(); }
        }


        bool? _indexedProperty = null;
        public bool? IndexedProperty
        {
            get { return _indexedProperty; }
            set { _indexedProperty = value; NotifyChange(); }
        }

        public bool? IsEnum
        {
            get
            {
                if (TypeKind == TypeKind.Enum)
                    return true;
                else if (TypeKind == TypeKind.Any)
                    return null;
                else
                    return false;
            }
        }

        public bool? IsClass
        {
            get
            {
                if (TypeKind == TypeKind.Class)
                    return true;
                else if (TypeKind == TypeKind.Any)
                    return null;
                else
                    return false;
            }
        }

        public bool? IsStruct
        {
            get
            {
                if (TypeKind == TypeKind.Struct)
                    return true;
                else if (TypeKind == TypeKind.Any)
                    return null;
                else
                    return false;
            }
        }


        public bool? IsInterface
        {
            get
            {
                if (TypeKind == TypeKind.Interface)
                    return true;
                else if (TypeKind == TypeKind.Any)
                    return null;
                else
                    return false;
            }
        }



        bool? _isGeneric = null;
        public bool? IsGeneric
        {
            get { return _isGeneric; }
            set { _isGeneric = value; NotifyChange(); }
        }

        bool? _isStaticClass = null;
        public bool? IsStaticClass
        {
            get { return _isStaticClass; }
            set { _isStaticClass = value; NotifyChange(); }
        }

        bool? _isMutableType = null;
        public bool? IsMutableType
        {
            get { return _isMutableType;  }
            set { _isMutableType = value; NotifyChange(); }
        }

        bool? _isWebHostHiden = null;
        public bool? IsWebHostHidden
        {
            get 
            {
                if (!_isWebHostHiddenEnabled)
                    return false;
                else
                    return _isWebHostHiden; 
            }
            set { _isWebHostHiden = value; NotifyChange(); }
        }

        bool? _experimental = null;
        public bool? Experimental
        {
            get { return _experimental; }
            set { _experimental = value; NotifyChange(); }
        }

        bool _isWebHostHiddenEnabled = true;
        public bool IsWebHostHiddenEnabled
        {
            get { return _isWebHostHiddenEnabled; }
            set { _isWebHostHiddenEnabled = value; NotifyChange(); }
        }

        bool? _isSealedType = null;
        public bool? IsSealedType
        {
            get { return _isSealedType; }
            set { _isSealedType = value; NotifyChange(); }
        }

        bool? _isDelegateType = null;
        public bool? IsDelegateType
        {
            get { return _isDelegateType; }
            set { _isDelegateType = value; NotifyChange();  }
        }

        bool? _isEventArgsType = null;
        public bool? IsEventArgsType
        {
            get { return _isEventArgsType; }
            set { _isEventArgsType = value; NotifyChange(); }
        }

        bool? _isMultiVersion = null;
        public bool? IsMultiVersion
        {
            get { return _isMultiVersion;  }
            set { _isMultiVersion = value; NotifyChange(); }
        }

        bool? _isAbstractType = null;
        public bool? IsAbstractType
        {
            get { return _isAbstractType; }
            set { _isAbstractType = value; NotifyChange(); }
        }

        bool? _hasInterfaces;
        public bool? HasInterfaces
        {
            get { return _hasInterfaces; }
            set { _hasInterfaces = value; NotifyChange(); }
        }





        bool? _isProtected = null;
        public bool? IsProtected
        {
            get { return _isProtected; }
            set { _isProtected = value; NotifyChange(); }
        }


        bool? _isVirtual = null;
        public bool? IsVirtual
        {
            get { return _isVirtual; }
            set { Update(ref _isVirtual, value); }
        }


        bool? _isOverloaded = null;
        public bool? IsOverloaded
        {
            get { return _isOverloaded; }
            set { _isOverloaded = value; NotifyChange(); }
        }


        bool? _canWrite = null;
        public bool? CanWrite
        {
            get { return _canWrite; }
            set { _canWrite = value; NotifyChange(); }
        }


        bool? _isDO = null;
        public bool? IsDO
        {
            get { return _isDO; }
            set { _isDO = value; NotifyChange(); }
        }


        bool? _isDP = null;
        public bool? IsDP
        {
            get { return _isDP; }
            set { _isDP = value; NotifyChange(); }
        }

        bool? _isRoutedEvent = null;
        public bool? IsRoutedEvent
        {
            get { return _isRoutedEvent; }
            set { _isRoutedEvent = value; NotifyChange(); }
        }



        public static string MarshalingBehaviorDefault = "Any marshaling";
        public static string MarshalingBehaviorUnspecified = "(Unspecified)";
        public static string MarshalingBehaviorNonAgile = "(Not agile)";

        // Match MarshalingType
        static string[] _marshalingBehaviorValues = new string[] 
        { 
            MarshalingBehaviorDefault, 
            "None", 
            "Agile", 
            "Standard", 
            MarshalingBehaviorUnspecified,
            MarshalingBehaviorNonAgile
        };

        public IEnumerable<string> MarshalingBehaviorValues
        {
            get
            {
                return _marshalingBehaviorValues;
            }
        }


        public object[] _trustLevelValues =
        {
            new NameValue() { Name ="Any trust level", Value = Tempo.TrustLevel.Any },
            new NameValue() { Name="Base", Value = Tempo.TrustLevel.Base },
            new NameValue() { Name="Partial", Value = Tempo.TrustLevel.Partial},
            new NameValue() { Name="Full", Value = Tempo.TrustLevel.Full },
            new NameValue() { Name="Unset", Value = Tempo.TrustLevel.Unset }
        };

        public object[] TrustLevelValues
        {
            get { return _trustLevelValues; }
        }

        public TrustLevel _trustLevel = TrustLevel.Any;
        public TrustLevel TrustLevel
        {
            get { return _trustLevel; }
            set 
            { 
                // The setter gets called with no change when it's loaded in the view, and the 
                // change notification would trigger a Reset()
                if (value != _trustLevel) 
                { 
                    _trustLevel = value; NotifyChange(); 
                } 
            }
        }

        public object[] _sdkPlatformValues =
        {
            new { Name = "All platforms", Value = Tempo.SdkPlatform.Any },
            new { Name ="Universal", Value = Tempo.SdkPlatform.Universal},
            new { Name="Desktop", Value = Tempo.SdkPlatform.Desktop},
            new { Name="IoT", Value = Tempo.SdkPlatform.Iot},
            new { Name="Mobile", Value = Tempo.SdkPlatform.Mobile},
            new { Name="Team", Value = Tempo.SdkPlatform.Team},
            new {Name = "Unknown", Value = Tempo.SdkPlatform.Unknown},
        };

        public object[] SdkPlatformValues
        {
            get { return _sdkPlatformValues; }
        }

        public SdkPlatform _sdkPlatform = SdkPlatform.Any;
        public SdkPlatform SdkPlatform
        {
            get { return _sdkPlatform; }
            set
            {
                // The setter gets called with no change when it's loaded in the view, and the 
                // change notification would trigger a Reset()
                if (value != _sdkPlatform)
                {
                    _sdkPlatform = value; NotifyChange();
                }
            }
        }

        public object[] _typeKindValues = 
        {
            new { Name = "All type kinds", Value = TypeKind.Any },
            new { Name = "Class", Value = TypeKind.Class },
            new { Name = "Interface", Value = TypeKind.Interface },
            new { Name = "Struct", Value = TypeKind.Struct },
            new { Name = "Enum", Value = TypeKind.Enum }
        };

        public object[] TypeKindValues
        {
            get { return _typeKindValues; }
        }

        public TypeKind _typeKind = TypeKind.Any;
        public TypeKind TypeKind
        {
            get { return _typeKind; }
            set
            {
                if(_typeKind != value)
                {
                    _typeKind = value;
                    NotifyChange();
                }
            }
        }

        void Update(ref bool? current, bool? newValue) 
        {
            if(current != newValue) 
            {
                current = newValue;
                NotifyChange();
            }
        }


        private object[] _memberKindValues =
        {
            new { Name = "All member kinds", Value = MemberKind.Any },
            new { Name = "Types (Control+T)", Value = MemberKind.Type },
            new { Name = "Properties (Control+P)", Value= MemberKind.Property },
            new { Name = "Methods (Control+H)",  Value = MemberKind.Method },
            new { Name = "Events (Control+N)", Value = MemberKind.Event },
            new { Name = "Fields", Value = MemberKind.Field },
            new { Name = "Constructors", Value = MemberKind.Constructor }
        };
        private object[] _memberKindValues2 =
        {
            new { Name = "All member kinds", Value = MemberKind.Any },
            new { Name = "Types", Value = MemberKind.Type },
            new { Name = "Properties", Value= MemberKind.Property },
            new { Name = "Methods",  Value = MemberKind.Method },
            new { Name = "Events", Value = MemberKind.Event },
            new { Name = "Fields", Value = MemberKind.Field },
            new { Name = "Constructors", Value = MemberKind.Constructor }
        };



        public object[] MemberKindValues
        {
            get { return _memberKindValues; }
        }
        public object[] MemberKindValues2
        {
            get { return _memberKindValues2; }
        }

        MemberKind _memberKind = MemberKind.Any;
        public MemberKind MemberKind
        {
            get { return _memberKind; }
            set 
            {
                if (_memberKind != value)
                {
                    _memberKind = value;
                    NotifyChange();
                }
            }
        }

        public bool ShowProperties
        {
            get
            {
                if (MemberKind == MemberKind.Any|| MemberKind == MemberKind.Property)
                    return true;
                else
                    return false;
            }
        }


        public bool ShowMethods
        {
            get
            {
                if (MemberKind == MemberKind.Method || MemberKind == MemberKind.Any)
                    return true;
                else
                    return false;
            }
        }

        public bool ShowEvents
        {
            get
            {
                if (MemberKind == MemberKind.Event || MemberKind == MemberKind.Any)
                    return true;
                else
                    return false;
            }
        }


        public bool ShowFields
        {
            get
            {
                if (MemberKind == MemberKind.Field || MemberKind == MemberKind.Any)
                    return true;
                else
                    return false;
            }
        }

        public bool ShowConstructors
        {
            get
            {
                if (MemberKind == MemberKind.Constructor || MemberKind == MemberKind.Any)
                    return true;
                else
                    return false;
            }
        }



        private int _marshalingBehaviorValue = 0;
        public int MarshalingBehaviorValue
        {
            get { return _marshalingBehaviorValue; }
        }


        string _marshalingBehavior = MarshalingBehaviorDefault;
        public string MarshalingBehavior
        {
            get { return _marshalingBehavior; }
            set
            {
                if (value == null)
                {
                    value = MarshalingBehaviorDefault;
                }

                var oldValue = _marshalingBehavior;
                _marshalingBehavior = value;
                if (oldValue != value)
                {
                    if (value == MarshalingBehaviorDefault)
                        _marshalingBehaviorValue = 0;
                    else if (value == MarshalingBehaviorUnspecified)
                        _marshalingBehaviorValue = -1;
                    else
                    {
                        int i = 0;
                        foreach (var v in _marshalingBehaviorValues)
                        {
                            if (v == value)
                            {
                                _marshalingBehaviorValue = i;
                                break;
                            }
                            ++i;
                        }
                        Debug.Assert(value == _marshalingBehaviorValues[_marshalingBehaviorValue]);
                    }

                    NotifyChange();
                }
            }
        }

        public bool IsMemberRequired()
        {
            return
                IndexedProperty == true
                || IsStatic != null
                || IsProtected != null
                || IsOverloaded != null
                || CanWrite != null
                || IsDP != null
                || IsRoutedEvent != null
                || IsExplicit != null
                || HasOutParameter != null
                || HasAddedSetter != null
                || HasMultipleOutParameters != null
                || HasRefParameter != null
                || HasReturnValue != null
                || HasPrimitiveReturnValue != null
                || HasInParameter != null
                || HasAgileParameter != null
                || HasInterfaceParameter != null
                || HasDelegateParameter != null
                || HasMutableParameter != null
                || IsAddedMember != null
                || IsAsync != null
                || IsRemoteAsync != null
                || IsVirtual != null
                || IsFinal != null
                || IsOverride != null
                || IsAbstract != null
                || PropertyNameMatchesTypeName != null
                || CustomInParameters != null
                || UntypedArgs != null
                || HasInterfaceParameter != null
                || ReturnsHostType != null
                || FilterOnName == false
                || MemberInWindows != null
                || MemberInCustom != null
                || MemberInPhone != null
                || MemberInWpf != null
                || HasMatchingPropertyAndSetMethod != null
                || ConflictingOverrides != null
                || DuplicateEnumValues != null
                ;

        }


        

        public IEnumerable<string> VersionFriendlyNameValues
        { 
            get 
            {
                return ReleaseInfo.VersionFriendlyNameValues;
            } 
        }

        string _selectedVersionFriendlyName = ReleaseInfo.AnyVersionFriendlyName[0];
        public string SelectedVersionFriendlyName
        {
            get { return _selectedVersionFriendlyName; }
            set 
            {
                if (value == null) // bugbug
                    return;

                if (_selectedVersionFriendlyName == value)
                    return; 

                _selectedVersionFriendlyName = value;

                string  newValue = null;
                foreach( var kvp in ReleaseInfo.VersionFriendlyNames)
                {
                    if (kvp.Value == value)
                    {
                        newValue = kvp.Key;
                        break;
                    }
                }


                if (VersionString != newValue)
                {
                    VersionString = newValue;
                    NotifyChange();
                }

            }
        }

        public bool _versionStringLock = false;
        string _versionString = null;
        public string VersionString
        {
            get { return _versionString; }
            private set { _versionString = value; }
        }

        
        public static string ThreadingModelDefault = "Any threading model";
        public static string ThreadingModelUnspecified = "(Unspecified)";
        static string[] _threadingModelValues = new string[] 
        { ThreadingModelDefault, "STA", "MTA", "Both", ThreadingModelUnspecified };

        public IEnumerable<string> ThreadingModelValues
        {
            get
            {
                return _threadingModelValues;
            }
        }


        private int _threadingModelValue = 0;
        public int ThreadingModelValue
        {
            get { return _threadingModelValue; }
        }


        string _threadingModel = ThreadingModelDefault;
        public string ThreadingModel
        {
            get { return _threadingModel; }
            set
            {
                if (value == null)
                {
                    // jjj
                    return;
                    //value = ThreadingModelDefault;
                }

                var oldValue = _threadingModel;
                _threadingModel = value;
                if (oldValue != value)
                {
                    if (value == ThreadingModelDefault)
                        _threadingModelValue = 0;
                    else if (value == ThreadingModelUnspecified)
                        _threadingModelValue = -1;
                    else
                    {
                        int i = 0;
                        foreach (var v in _threadingModelValues)
                        {
                            if (v == value)
                            {
                                _threadingModelValue = i;
                                break;
                            }
                            ++i;
                        }
                        Debug.Assert(value == _threadingModelValues[_threadingModelValue]);
                    }

                    NotifyChange();
                }



            }
        }


    }

    public enum TrustLevel
    {
        Unset = -2,
        Any = -1,
        Base = 0,
        Partial = 1,
        Full = 2
    }


    public enum SdkPlatform
    {
        Any = 0,
        Universal = 1,
        Desktop = 2,
        Iot = 3,
        Mobile = 4,
        Team = 5,
        Unknown,
    }
    // The order of this has to match TypeKindValues
    public enum TypeKind
    {
        Any,
        Class,
        Interface,
        Struct,
        Enum
    }

    // The order of this has to match MemberKindValues
    public enum MemberKind
    {
        Any,
        Type,
        Property,
        Method,
        Event,
        Field,
        Constructor
    }

}







