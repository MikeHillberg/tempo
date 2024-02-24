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
                // (IsWildcardSyntax affects search, but we still don't need to run the detailed checkers.)
                if (prop.Name == nameof(IsDefault) 
                    || prop.Name == nameof(IsPreview)
                    || prop.Name.StartsWith("Language")
                    || prop.Name == nameof(IsWildcardSyntax)
                    )
                {
                    continue;
                }

                // This property _does_ affect search, but we don't want it to 
                // make settings appear dirty, because it doesn't show up in the Filters dialog,
                // so it shouldn't make the Filters button be highlighted

                if(prop.Name == nameof(CompareToBaseline))
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
            //System.Diagnostics.Debugger.Break();

            //if (_convertForJS)
            //    _isDefault = false;
            //else
            //    _isDefault = true;
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


        static WeakEventHandler<PropertyChangedEventHandler> _propertyChanged 
            = new WeakEventHandler<PropertyChangedEventHandler>();

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                _propertyChanged.Add(value);
            }
            remove 
            {
                _propertyChanged.Remove(value);
            }
        }


        public void NotifyChange(bool isReset = false, [CallerMemberName] string name = null)
        {
            DebugLog.Append($"Settings change ({isReset}, {name})");
            _isDefault = isReset ? (bool?)true : null;

            var args = new PropertyChangedEventArgs(name);

            // Instance changed event (for x:Bind)

            _propertyChanged.Raise((handler) => (handler as PropertyChangedEventHandler).Invoke(this, args));

            // Static changed event (because there's only one instance of Settings)
            if (Changed != null)
            {
                Changed.Invoke(this, args);
            }

            Manager.SettingsHack.Reset();
        }

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
            set { Set(ref _isFinal, value); }
        }

        bool _isGrouped = true;
        public bool IsGrouped
        {
            get 
            { 
                return _isGrouped && !Flat; 
            }
            set { Set(ref _isGrouped, value); }
        }

        bool _namespaceInclusive = true;
        public bool NamespaceInclusive
        {
            get { return _namespaceInclusive; }
            set { Set(ref _namespaceInclusive, value); }
        }

        bool _namespaceExclusive = false;
        public bool NamespaceExclusive
        {
            get { return _namespaceExclusive; }
            set { Set(ref _namespaceExclusive, value); }
        }

        bool _namespaceExcluded = false;
        public bool NamespaceExcluded
        {
            get { return _namespaceExcluded; }
            set { Set(ref _namespaceExcluded, value); }
        }


        bool? _isOverride = null;
        public bool? IsOverride
        {
            get { return _isOverride; }
            set { Set(ref _isOverride, value); }
        }

        // Compare the current typeset to a baseline typeset (for finding deltas)
        static bool _compareToBaselineStatic = false;
        bool _compareToBaseline = _compareToBaselineStatic;
        public bool CompareToBaseline
        {
            get { return _compareToBaseline; }
            set 
            {
                _compareToBaselineStatic = value;
                Set(ref _compareToBaseline, value); 
            }
        }

        bool? _isAsync = null;
        public bool? IsAsync
        {
            get { return _isAsync; }
            set { Set(ref _isAsync, value); }
        }

        bool? _isRemoteAsync = null;
        public bool? IsRemoteAsync
        {
            get { return _isRemoteAsync; }
            set { Set(ref _isRemoteAsync, value); }
        }

        bool? _isFirstWordAVerb = null;
        public bool? IsFirstWordAVerb
        {
            get { return _isFirstWordAVerb; }
            set { Set(ref _isFirstWordAVerb, value); }
        }

        bool? _isRestricted = null;
        public bool? IsRestricted
        {
            get { return _isRestricted; }
            set { Set(ref _isRestricted, value); }
        }

        public bool? _hasApiDesignNotes = null;
        public bool? HasApiDesignNotes
        {
            get { return _hasApiDesignNotes; }
            set { Set(ref _hasApiDesignNotes, value); }
        }


        bool _filterOnDeclaringType = false;
        public bool FilterOnDeclaringType
        {
            get { return _filterOnDeclaringType; }
            set { Set(ref _filterOnDeclaringType, value); }
        }

        bool _caseSensitive = false;
        public bool CaseSensitive
        {
            get { return _caseSensitive; }
            set { Set(ref _caseSensitive, value); }
        }

        static bool _isWildcardSyntaxDefault = false;
        bool _isWildcardSyntax = _isWildcardSyntaxDefault;
        public bool IsWildcardSyntax
        {
            get { return _isWildcardSyntax; }
            set 
            {
                _isWildcardSyntaxDefault = value;
                Set(ref _isWildcardSyntax, value); 
            }
        }

        // Update a property and raise a notification, if it changed
        void Set( ref bool _current, bool value, 
                  [CallerMemberName] string memberName = null)
        {
            if(_current != value)
            {
                _current = value;
                NotifyChange(isReset: false, name: memberName);
            }
        }

        // bugbug: Is there a way to write a generic version of that that handles any type? Even any value type?
        void Set(ref bool? _current, bool? value,
          [CallerMemberName] string memberName = null)
        {
            if (_current != value)
            {
                _current = value;
                NotifyChange(isReset: false, name: memberName);
            }
        }

        bool _internalInterfaces = false;
        public bool InternalInterfaces
        {
            get { return _internalInterfaces; }
            set 
            { 
                if(_internalInterfaces == value)
                {
                    return;
                }
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
            set { Set(ref _dualApi, value); }
        }

        bool? _nameAndNamespaceConflict = null;
        public bool? NameAndNamespaceConflict
        {
            get { return _nameAndNamespaceConflict; }
            set { Set(ref _nameAndNamespaceConflict, value); }
        }

        bool? _oneWordName = null;
        public bool? OneWordName
        {
            get { return _oneWordName; }
            set { Set(ref _oneWordName, value); }
        }


        bool? _isMuse = null;
        public bool? IsMuse
        {
            get { return _isMuse; }
            set { Set(ref _isMuse, value); }
        }

        bool? _isUac = null;
        public bool? IsUac
        {
            get { return _isUac; }
            set { Set(ref _isUac, value); }
        }

        bool? _typeInWindows = null;
        public bool? TypeInWindows
        {
            get { return _typeInWindows; }
            set { Set(ref _typeInWindows, value);}
        }



        bool? _typeInPhone= null;
        public bool? TypeInPhone
        {
            get { return _typeInPhone; }
            set { Set(ref _typeInPhone, value); }
        }



        bool? _typeInCustom = null;
        public bool? TypeInCustom
        {
            get { return _typeInCustom; }
            set { Set(ref _typeInCustom, value); }
        }

        bool? _typeInWpf = null;
        public bool? TypeInWpf
        {
            get { return _typeInWpf; }
            set { Set(ref _typeInWpf, value); }
        }

        bool? _hasMatchingPropertyAndSetMethod = null;
        public bool? HasMatchingPropertyAndSetMethod
        {
            get { return _hasMatchingPropertyAndSetMethod;  }
            set { Set(ref _hasMatchingPropertyAndSetMethod, value); }
        }

        bool? _duplicateEnumValues = null;
        public bool? DuplicateEnumValues
        {
            get { return _duplicateEnumValues; }
            set { Set(ref _duplicateEnumValues, value); }
        }

        bool? _conflictingOverrides = null;
        public bool? ConflictingOverrides
        {
            get { return _conflictingOverrides; }
            set { Set(ref _conflictingOverrides, value); }
        }

        bool? _isFlagsEnum = null;
        public bool? IsFlagsEnum
        {
            get { return _isFlagsEnum; }
            set { Set(ref _isFlagsEnum, value); }
        }

        bool? _memberInWindows = null;
        public bool? MemberInWindows
        {
            get { return _memberInWindows; }
            set { Set(ref _memberInWindows, value); }
        }

        bool? _memberInCustom = null;
        public bool? MemberInCustom
        {
            get { return _memberInCustom; }
            set { Set(ref _memberInCustom, value); }
        }

        bool? _memberInWpf = null;
        public bool? MemberInWpf
        {
            get { return _memberInWpf; }
            set { Set(ref _memberInWpf, value); }
        }

        bool? _memberInPhone = null;
        public bool? MemberInPhone
        {
            get { return _memberInPhone; }
            set { Set(ref _memberInPhone, value); }
        }



        bool? _deprecated = null;
        public bool? Deprecated
        {
            get { return _deprecated; }
            set { Set(ref _deprecated, value); }
        }

        bool? _propertyNameMatchesTypeName = null;
        public bool? PropertyNameMatchesTypeName
        {
            get { return _propertyNameMatchesTypeName; }
            set { Set(ref _propertyNameMatchesTypeName, value); }
        }

        bool? _returnsHostType = null;
        public bool? ReturnsHostType
        {
            get { return _returnsHostType; }
            set { Set(ref _returnsHostType, value); }
        }

        bool? _customInParameters = null;
        public bool? CustomInParameters
        {
            get { return _customInParameters; }
            set { Set(ref _customInParameters, value); }
        }

        bool? _markerInterfaces = null;
        public bool? MarkerInterfaces
        {
            get { return _markerInterfaces; }
            set { Set(ref _markerInterfaces, value); }
        }

        bool? _untypedArgs = null;
        public bool? UntypedArgs
        {
            get { return _untypedArgs; }
            set { Set(ref _untypedArgs, value); }
        }

        bool _unobtainableType = false;
        public bool UnobtainableType
        {
            get { return _unobtainableType; }
            set { Set(ref _unobtainableType, value); }
        }

        bool _duplicateTypeName = false;
        public bool DuplicateTypeName
        {
            get { return _duplicateTypeName; }
            set { Set(ref _duplicateTypeName, value); }
        }

        bool? _isAbstract = null;
        public bool? IsAbstract
        {
            get { return _isAbstract; }
            set { Set(ref _isAbstract, value); }
        }


        bool? _isExplicit = null;
        public bool? IsExplicit
        {
            get { return _isExplicit; }
            set { Set(ref _isExplicit, value); }
        }


        bool? _hasOutParameter = null;
        public bool? HasOutParameter
        {
            get { return _hasOutParameter; }
            set { Set(ref _hasOutParameter, value); }
        }

        bool? _hasAddedSetter = null;
        public bool? HasAddedSetter
        {
            get { return _hasAddedSetter; }
            set { Set(ref _hasAddedSetter, value); }
        }

        bool? _hasMultipleOutParameters = null;
        public bool? HasMultipleOutParameters
        {
            get { return _hasMultipleOutParameters; }
            set { Set(ref _hasMultipleOutParameters, value); }
        }


        bool? _implementsInternalInterface = null;
        public bool? ImplementsInternalInterface
        {
            get { return _implementsInternalInterface; }
            set { Set(ref _implementsInternalInterface, value); }
        }

        bool? _hasInParameter = null;
        public bool? HasInParameter
        {
            get { return _hasInParameter; }
            set { Set(ref _hasInParameter, value); }
        }

        bool? _hasAgileParameter = null;
        public bool? HasAgileParameter
        {
            get { return _hasAgileParameter; }
            set { Set(ref _hasAgileParameter, value); }
        }

        bool? _hasInterfaceParameter = null;
        public bool? HasInterfaceParameter
        {
            get { return _hasInterfaceParameter; }
            set { Set(ref _hasInterfaceParameter, value); }
        }

        bool? _hasDelegateParameter = null;
        public bool? HasDelegateParameter
        {
            get { return _hasDelegateParameter; }
            set { Set(ref _hasDelegateParameter, value); }
        }

        bool? _hasMutableParameter = null;
        public bool? HasMutableParameter
        {
            get { return _hasMutableParameter; }
            set { Set(ref _hasMutableParameter, value); }
        }

        bool? _hasRefParameter = null;
        public bool? HasRefParameter
        {
            get { return _hasRefParameter; }
            set { Set(ref _hasRefParameter, value); }
        }

        bool? _hasReturnValue = null;
        public bool? HasReturnValue
        {
            get { return _hasReturnValue; }
            set { Set(ref _hasReturnValue, value);  }
        }

        bool? _hasPrimitiveReturnValue = null;
        public bool? HasPrimitiveReturnValue
        {
            get { return _hasPrimitiveReturnValue; }
            set { Set(ref _hasPrimitiveReturnValue, value); }
        }

        bool? _isAddedMember = null;
        public bool? IsAddedMember
        {
            get { return _isAddedMember; }
            set { Set(ref _isAddedMember, value); }
        }

        bool _filterOnFullName = true;
        public bool FilterOnFullName
        {
            get { return _filterOnFullName || _flat; }
            set { Set(ref _filterOnFullName, value); }
        }

        bool _filterOnReturnType = true;
        public bool FilterOnReturnType
        {
            get { return _filterOnReturnType && !_flat; }
            set { Set(ref _filterOnReturnType, value); }
        }

        bool _flat = false;
        public bool Flat
        {
            get { return _flat; }
            set { Set(ref _flat, value); }
        }

        public bool NotFlat
        {
            get { return !Flat; }
        }


        bool _filterOnName = true;
        public bool FilterOnName
        {
            get { return _filterOnName; }
            set { Set(ref _filterOnName, value); }
        }

        bool _filterOnBaseType = true;
        public bool FilterOnBaseType
        {
            get { return _filterOnBaseType && !_flat; }
            set { Set(ref _filterOnBaseType, value); }
        }

        // Used to default this to false because in .Net Reflection it was really slow
        bool _filterOnAttributes = true;
        public bool FilterOnAttributes
        {
            get { return _filterOnAttributes; }
            set { Set(ref _filterOnAttributes, value); }
        }

        bool _filterOnDllPath = true;
        public bool FilterOnDllPath
        {
            get { return _filterOnDllPath; }
            set { Set(ref _filterOnDllPath, value); }
        }

        bool _filterOnParameters = true;
        public bool FilterOnParameters
        {
            get { return _filterOnParameters && !_flat; }
            set { Set(ref _filterOnParameters, value); }
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
            set { Set(ref _isActivatable, value); }
        }

        public bool ShowTypes
        {
            get { return MemberKind == MemberKind.Type || MemberKind == MemberKind.Any; }
        }

        public bool OnlyShowTypes
        {
            get { return MemberKind == MemberKind.Type; }
        }


        bool? _isStatic = null;
        public bool? IsStatic
        {
            get { return _isStatic; }
            set { Set(ref _isStatic, value); }
        }

        bool? _hasDefaultConstructor = null;
        public bool? HasDefaultConstructor
        {
            get { return _hasDefaultConstructor; }
            set { Set(ref _hasDefaultConstructor, value); }
        }

        bool? _hasNonDefaultConstructor = null;
        public bool? HasNonDefaultConstructor
        {
            get { return _hasNonDefaultConstructor; }
            set { Set(ref _hasNonDefaultConstructor, value); }
        }


        bool? _hasProtectedConstructors = null;
        public bool? HasProtectedConstructors
        {
            get { return _hasProtectedConstructors; }
            set { Set(ref _hasProtectedConstructors, value); }
        }


        bool? _hasPublicConstructors = null;
        public bool? HasPublicConstructors
        {
            get { return _hasPublicConstructors; }
            set { Set(ref _hasPublicConstructors, value); }
        }


        bool? _hasStaticConstructor = null;
        public bool? HasStaticConstructor
        {
            get { return _hasStaticConstructor; }
            set { Set(ref _hasStaticConstructor, value); }
        }

        bool? _hasBaseType = null;
        public bool? HasBaseType
        {
            get { return _hasBaseType; }
            set { Set(ref _hasBaseType, value); }
        }


        bool? _indexedProperty = null;
        public bool? IndexedProperty
        {
            get { return _indexedProperty; }
            set { Set(ref _indexedProperty, value); }
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
            set { Set(ref _isGeneric, value); }
        }

        bool? _isStaticClass = null;
        public bool? IsStaticClass
        {
            get { return _isStaticClass; }
            set { Set(ref _isStaticClass, value); }
        }

        bool? _isMutableType = null;
        public bool? IsMutableType
        {
            get { return _isMutableType;  }
            set { Set(ref _isMutableType, value); }
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
            set { Set(ref _isWebHostHiden, value); }
        }

        bool? _experimental = null;
        public bool? Experimental
        {
            get { return _experimental; }
            set { Set(ref _experimental, value); }
        }

        bool _isWebHostHiddenEnabled = true;
        public bool IsWebHostHiddenEnabled
        {
            get { return _isWebHostHiddenEnabled; }
            set { Set(ref _isWebHostHiddenEnabled, value); }
        }

        bool? _isSealedType = null;
        public bool? IsSealedType
        {
            get { return _isSealedType; }
            set { Set(ref _isSealedType, value); }
        }

        bool? _isDelegateType = null;
        public bool? IsDelegateType
        {
            get { return _isDelegateType; }
            set { Set(ref _isDelegateType, value); }
        }

        bool? _isEventArgsType = null;
        public bool? IsEventArgsType
        {
            get { return _isEventArgsType; }
            set { Set(ref _isEventArgsType, value); }
        }

        bool? _isMultiVersion = null;
        public bool? IsMultiVersion
        {
            get { return _isMultiVersion;  }
            set { Set(ref _isMultiVersion, value); }
        }

        bool? _isAbstractType = null;
        public bool? IsAbstractType
        {
            get { return _isAbstractType; }
            set { Set(ref _isAbstractType, value); }
        }

        bool? _hasInterfaces;
        public bool? HasInterfaces
        {
            get { return _hasInterfaces; }
            set { Set(ref _hasInterfaces, value); }
        }





        bool? _isProtected = null;
        public bool? IsProtected
        {
            get { return _isProtected; }
            set { Set(ref _isProtected, value); }
        }


        bool? _isVirtual = null;
        public bool? IsVirtual
        {
            get { return _isVirtual; }
            set { Set(ref _isVirtual, value); }
        }


        bool? _isOverloaded = null;
        public bool? IsOverloaded
        {
            get { return _isOverloaded; }
            set { Set(ref _isOverloaded, value); }
        }


        bool? _canWrite = null;
        public bool? CanWrite
        {
            get { return _canWrite; }
            set { Set(ref _canWrite, value); }
        }


        bool? _isDO = null;
        public bool? IsDO
        {
            get { return _isDO; }
            set { Set(ref _isDO, value); }
        }


        bool? _isDP = null;
        public bool? IsDP
        {
            get { return _isDP; }
            set { Set(ref _isDP, value); }
        }

        bool? _isRoutedEvent = null;
        public bool? IsRoutedEvent
        {
            get { return _isRoutedEvent; }
            set { Set(ref _isRoutedEvent, value); }
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

        public bool OnlyShowProperties => MemberKind == MemberKind.Property;


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

        public bool OnlyShowMethods => MemberKind == MemberKind.Method;

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
        public bool OnlyShowEvents => MemberKind == MemberKind.Event;


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
        public bool OnlyShowFields => MemberKind == MemberKind.Field;

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
        public bool OnlyShowConstructors => MemberKind == MemberKind.Constructor;



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







