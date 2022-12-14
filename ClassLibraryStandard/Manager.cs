using CommonLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
//using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Tempo;

namespace Tempo
{
    public class Manager
    {


        static public bool IsSystem = false;

        // bugbug
        static public int _referenceIndex = 0;


        static public TypeViewModel GetMatchingType(IEnumerable<TypeViewModel> types, TypeViewModel typeVM)
        {
            // perfbugbug
            if (types == null)
                return null;

            string typeName = typeVM.FullName;
            string typeName2 = null;

            bool isWinMD = false;
            if (Manager.WinmdTypeSet != null && types == Manager.WinmdTypeSet.Types)
                isWinMD = true;

            if (isWinMD && typeName.StartsWith("System.Windows"))
            {
                typeName2 = typeName.Replace("System.Windows", "Windows.UI.Xaml");
            }
            else if (!isWinMD && typeName.StartsWith("Windows.UI.Xaml"))
            {
                typeName2 = typeName.Replace("Windows.UI.Xaml", "System.Windows");
            }

            foreach (var t in types)
            {
                if (typeName == t.FullName || typeName2 == t.FullName)
                {
                    return t;
                }
            }

            return null;
        }

        static public bool MatchesTypeKind(TypeViewModel t, ref bool meaningfulMatch)
        {
            return MatchesSetting(Settings.IsClass, t.IsClass, ref meaningfulMatch)
                   &&
                   MatchesSetting(Settings.IsInterface, t.IsInterface, ref meaningfulMatch)
                   &&
                   MatchesSetting(Settings.IsEnum, t.IsEnum, ref meaningfulMatch)
                   &&
                   MatchesSetting(Settings.IsStruct, !t.IsEnum && t.IsValueType, ref meaningfulMatch);
        }

        static public bool MatchesTypeModifiers(TypeViewModel t, ref bool meaningfulMatch)
        {


            return MatchesSetting(Settings.IsGeneric, t.IsGenericTypeDefinition, ref meaningfulMatch)
                   &&
                   MatchesSetting(Settings.IsAbstractType, t.IsAbstract && !t.IsSealed, ref meaningfulMatch)
                   &&
                   (Settings.IsSealedType == null
                     ||
                     Settings.IsSealedType == true
                     && MatchesSetting(true, t.IsSealed && !t.IsAbstract, ref meaningfulMatch)
                     ||
                     Settings.IsSealedType == false
                     && MatchesSetting(false, t.IsSealed, ref meaningfulMatch)
                   )
                   &&
                   MatchesSetting(Settings.IsStaticClass, t.IsSealed && t.IsAbstract, ref meaningfulMatch)
                   &&
                   MatchesSetting(Settings.HasInterfaces, () => TypeHasInterfaces(t), ref meaningfulMatch);

        }

        static public bool WPEnabled { get; set; }
        static public bool WinMDEnabled { get; set; }

        // Callback to get onto the UI thread (WinAppSDK is different than WPF)
        public static Action<Action> PostToUIThread { get; set; }

        static private bool TypeHasInterfaces(TypeViewModel t)
        {
            var l = t.Interfaces;// Type2Interfaces.GetInterfaces(t);
            if (l == null)
                return false;

            var count = l.Count();
            if (!Settings.InternalInterfaces)
            {
                foreach (var i in l)
                {
                    if (!i.IsPublic)
                        count--;
                }
            }
            if (count == 0)
                return false;

            return true;
        }

        static public bool MatchesDependencyObject(TypeViewModel t, ref bool meaningfulMatch)
        {
            if (Settings.IsDO == null)
                return true;

            while (t != null)
            {
                if (t.FullName == "System.Windows.DependencyObject"
                    ||
                    t.FullName == "Windows.UI.Xaml.DependencyObject")
                {

                    break;
                }
                t = t.BaseType;
            }

            if (Settings.IsDO == true && t != null
                ||
                Settings.IsDO == false && t == null)
            {
                meaningfulMatch = true;
                return true;
            }

            return false;


        }

        static bool MatchesAttributes(BaseViewModel t, Regex filter, Settings settings, ref bool abort, ref bool meaningfulMatch)
        {
            if (Settings.FilterOnAttributes == false)
                return false;

            IList<CustomAttributeViewModel> attrs = null;

            try
            {
                attrs = t.CustomAttributes;// CustomAttributesData;
            }
            catch (Exception e)
            {
                if (!e.Message.Contains("GCPressure"))
                    throw;
            }


            if (attrs == null)
                return false;

            foreach (var attr in attrs)
            {
                if (attr.FullName == "Windows.Foundation.Metadata.StaticAttribute")
                    // [static] shows up as class members instead
                    continue;

                if (attr.FullName == "Windows.Foundation.Metadata.ComposableAttribute")
                    // [composable] shows up as constructor instead
                    continue;

                // ((System.Reflection.RuntimeConstructorInfo)(attr.m_ctor)).DeclaringType
                var attributeClassName = attr.Name;// attr.Constructor.DeclaringType.Name;
                if (MatchesFilter(filter, attributeClassName, settings, ref abort, ref meaningfulMatch))
                    return true;

                foreach (var parameter in attr.ConstructorArguments)// AttributeView.SafeGetCustomAttributeConstructorArguments(attr))
                {
                    if (MatchesFilter(filter, parameter.ArgumentType.Name, settings, ref abort, ref meaningfulMatch))
                        return true;

                    if (parameter.Value != null)
                    {
                        if (MatchesFilter(filter, parameter.Value.ToString(), settings, ref abort, ref meaningfulMatch))
                            return true;
                    }
                }
            }

            return false;
        }



        // bugbug: Clean up this TypeIsPublicVolatile mess

        public static bool TypeIsPublicVolatile(Type t)
        {
            return TypeIsPublicVolatileDynamic2(t);
        }

        // Check if a type is public.
        // This is "volatile" because it depends on Settings, so at different times a type will report a different answer
        static public bool TypeIsPublicVolatile(TypeViewModel t)
        {
            // bugbug: Can below checks for e.g. INCC be deleted now, due to it being handled in
            // ReflectionTypeViewModel constuctor?

            return t.IsPublic
                   || Settings.InternalInterfaces
                   || t.IsWinMD && !t.IsInterface // CLR marks some types internal, e.g. GeneratorPosition
                   || t.Namespace.StartsWith("Windows.Foundation") && !t.IsInterface;
        }

        public static bool TypeIsPublicVolatile(TypeInfo ti)
        {
            return TypeIsPublicVolatileDynamic2(ti);
        }


        static public bool TypeIsPublicVolatileDynamic2(object t1)
        {
            dynamic t = t1; // bugbug
            return t.IsPublic
                   || Settings.InternalInterfaces
                   || t.Name == "INotifyPropertyChanged"
                   || (t1 is Type) && t.Assembly.CodeBase.EndsWith(".winmd")
                   || t.FullName.StartsWith("Windows.Foundation");
        }
        static public bool TypeMatchesFilters(TypeViewModel t, Regex filter, bool filterOnBaseTypes, Settings settings, ref bool abort, ref bool meaningfulMatch)
        {
            return
                TypeIsPublicVolatile(t)
                   && (TypeMatchesFilterString(t, filter, filterOnBaseTypes, settings, ref abort, ref meaningfulMatch)
                ||
                MatchesAttributes(t, filter, settings, ref abort, ref meaningfulMatch));
        }

        static bool TypeMatchesFilterString(TypeViewModel t, Regex filter, bool filterOnBaseTypes, Settings settings, ref bool abort, ref bool meaningfulMatch)
        {
            return MatchesFilterString(filter, t, Settings.FilterOnName, filterOnBaseTypes, settings, ref abort, ref meaningfulMatch);
        }

        static bool MatchesFilter(Regex filter, string name, Settings settings, ref bool abort, ref bool meaningfulMatch)
        {
            if (filter == null || filter.ToString() == "")
                return true;

            if (name == null)
            {
                name = "";
            }

            var f = filter;
            {
                string explicitName = null;
                var index = name.LastIndexOf('.');
                if (index != -1)
                    explicitName = name.Substring(index + 1);

                if (!MatchesOneFilter(f, name, settings, ref abort)
                    &&
                    (explicitName == null || !MatchesOneFilter(f, explicitName, settings, ref abort)))
                    return false;
            }

            meaningfulMatch = true;
            return true;
        }


        static bool MatchesOneFilter(Regex filter, string name, Settings settings, ref bool abort)
        {
            if (abort)
                return false;

            var matches = filter.IsMatch(name);
            return matches;

        }

        static public bool MatchesSetting(bool? setting, bool value, ref bool meaningfulMatch)
        {
            bool matches = MatchesSetting(setting, value);
            if (matches && setting != null)
                meaningfulMatch = true;
            return matches;
        }

        static public bool MatchesSetting(bool? setting, bool value)
        {
            return setting == true ? value : setting == false ? !value : true;
        }

        static public bool MatchesSetting(bool? setting, Func<bool> valueFunc, ref bool meaningfulMatch)
        {
            if (setting == null)
                return true;
            else
                return MatchesSetting(setting, valueFunc(), ref meaningfulMatch);
        }

        static bool MatchesProtected(dynamic method)//(bool isFamily, bool isPublic)
        {
            if (Settings.IsProtected == true && BaseViewModel.CheckProtected(method)//isFamily
                || Settings.IsProtected == false && !BaseViewModel.CheckProtected(method)//!isFamily
                || Settings.IsProtected == null)
            {
                return true;
            }

            return false;
        }



        static public IList<MemberViewModel> GetMembers(SearchExpression filter, int iteration)
        {
            var m = GetMembersHelper(filter, iteration);
            return m.ToList();
        }

        static Dictionary<TypeViewModel, IEnumerable<MemberViewModel>> _memberCache
            = new Dictionary<TypeViewModel, IEnumerable<MemberViewModel>>();

        static public string LastType { get; set; }
        static public string LastMember { get; set; }


        public static MatchingStats MatchingStats { get; } = new MatchingStats();


        static Manager()
        {
            CurrentTypeSet = null;
        }

        static public event EventHandler TypeSetChanged;

        static TypeSet _currentTypeSet = null;
        static public TypeSet CurrentTypeSet
        {
            get { return _currentTypeSet; }
            set
            {
                var changed = _currentTypeSet != value;
                _currentTypeSet = value;
                if (changed)
                {
                    TypeSetChanged?.Invoke(null, null);
                }
            }
        }

        static Dictionary<string, TypeSet> _allTypeSets = new Dictionary<string, TypeSet>();
        static public IEnumerable<TypeSet> AllTypeSets => _allTypeSets.Values;
        static TypeSet GetTypeSet([CallerMemberName] string name = null)
        {
            Debug.Assert(name != null);
            if (_allTypeSets.TryGetValue(name, out var typeSet))
            {
                return typeSet;
            }
            return null;
        }

        static void SetTypeSet(TypeSet typeSet, [CallerMemberName] string name = null)
        {
            Debug.Assert(name != null);
            _allTypeSets[name] = typeSet;

            // The TypeSet can be set on a worker thread
            // This is null in the case of the PSDrive
            if (PostToUIThread != null)
            {
                PostToUIThread(() =>
                {
                    TypeSetChanged?.Invoke(null, null);
                });
            }
            else
            {
                TypeSetChanged?.Invoke(null, null);
            }
        }


        static public TypeSet SLTypeSet { get { return GetTypeSet(); } set { SetTypeSet(value); } }
        static public TypeSet WpfTypeSet { get { return GetTypeSet(); } set { SetTypeSet(value); } }

        // System32 metadata using .Net reflection
        static public TypeSet WinmdTypeSet { get { return GetTypeSet(); } set { SetTypeSet(value); } }

        // System32 metadata using MR, C++ mode
        static public TypeSet WindowsTypeSetCpp { get { return GetTypeSet(); } set { SetTypeSet(value); } }

        // System32 metadata using MR, C# mode
        static public TypeSet WindowsTypeSetCS { get { return GetTypeSet(); } set { SetTypeSet(value); } }

        // Custom type set, using .Net reflection
        static public TypeSet CustomTypeSet { get { return GetTypeSet(); } set { SetTypeSet(value); } }

        // Custom type set, using MR
        static public TypeSet CustomMRTypeSet { get { return GetTypeSet(); } set { SetTypeSet(value); } }

        static public TypeSet BaselineTypeSet { get { return GetTypeSet(); } set { SetTypeSet(value); } }
        //static public TypeSet UwpTypeSet { get { return GetTypeSet(); } set { SetTypeSet(value); } }
        static public TypeSet Custom2TypeSet { get { return GetTypeSet(); } set { SetTypeSet(value); } }
        static public TypeSet WPTypeSet { get { return GetTypeSet(); } set { SetTypeSet(value); } }
        static public TypeSet DotNetTypeSet { get { return GetTypeSet(); } set { SetTypeSet(value); } }
        static public TypeSet XamFormsTypeSet { get { return GetTypeSet(); } set { SetTypeSet(value); } }
        static public TypeSet CardsTypeSet { get { return GetTypeSet(); } set { SetTypeSet(value); } }
        static public TypeSet WinFormsTypeSet { get { return GetTypeSet(); } set { SetTypeSet(value); } }
        static public TypeSet WinUI2TypeSet { get { return GetTypeSet(); } set { SetTypeSet(value); } }
        static public TypeSet WindowsAppTypeSet { get { return GetTypeSet(); } set { SetTypeSet(value); } }

        static Settings _settings = null;
        static public Settings Settings
        {
            get { return _settings; }
            set
            {
                _settings = value;
                SettingsHack.Reset();
                Settings.NotifyChange(isReset: true, string.Empty);
            }
        }

        // Can't x:Bind to static members and get change notifications.  So create
        // an instance that can implement INPC
        static public SettingsHack SettingsHack { get; } = new SettingsHack();

        static int _typesChecked = 0;
        static public event EventHandler TypesCheckedChanged;
        static public int TypesChecked
        {
            get { return _typesChecked; }
            set
            {
                _typesChecked = value;
                if (TypesCheckedChanged != null)
                    TypesCheckedChanged(null, null);
            }
        }


        static public bool ThreeStateCheck(bool? flag, bool value)
        {
            if (flag == true && value
                || flag == false && !value
                || flag == null)
            {
                return true;
            }

            return false;
        }



        static public int RecalculateIteration { get; set; }


        static public bool ShowingSLMsdn
        {
            get { return CurrentTypeSet == Manager.WPTypeSet; }
        }


        static public bool ShowingXamFormsMsdn
        {
            get { return CurrentTypeSet == XamFormsTypeSet; }
        }

        static public bool ShowingCardsMsdn
        {
            get { return CurrentTypeSet == CardsTypeSet; }
        }

        static IEnumerable<TypeViewModel> GetTypeAndAncestorsVolatile(TypeViewModel type)
        {
            yield return type;

            if (!Settings.FilterOnBaseType)
                yield break;


            var t = type.BaseType;
            while (t != null && !t.ShouldIgnore)
            {
                yield return t;
                t = t.BaseType;
            }

            foreach (var i in type.Interfaces)
            {
                if (!i.IsPublic && !Settings.InternalInterfaces)
                    continue;

                if (i.ShouldIgnore)// (Type2Ancestors.ShouldIgnoreType(i))
                    continue;

                yield return i;
            }
        }


        static string TypeShortOrFullName(Settings settings, TypeViewModel t, Regex filter)
        {
            string s;
            if (filter == null)
                s = t.PrettyName;
            else if (settings != null && settings.FilterOnFullName)
                s = t.FullName;
            else
                s = t.PrettyName;

            Debug.Assert(s != null);
            return s;
        }


        static public int MatchGeneration = 0;

        public static bool MatchesFilterString(Regex filter, TypeViewModel type, bool filterOnName, bool filterOnBaseTypes, Settings settings, ref bool abort, ref bool meaningfulMatch)
        {
            if (!filterOnName || (filter == null || filter.ToString().Trim() == ""))
            {
                return true;
            }

            if (type.ShouldIgnore) // (Type2Ancestors.ShouldIgnoreType(type))
                return false;

            var types = filterOnBaseTypes ? GetTypeAndAncestorsVolatile(type) : new TypeViewModel[] { type };

            if (settings != null && settings.FilterOnFullName)
            {
                if (MatchesFilter(filter, type.Namespace, settings, ref abort, ref meaningfulMatch))
                {
                    return true;
                }
            }

            if (type.DllPath != null)
            {
                if (MatchesFilter(filter, type.DllPath, settings, ref abort, ref meaningfulMatch))
                {
                    return true;
                }
            }

            foreach (var t in types)
            {
                if (t.ShouldIgnore)// (Type2Ancestors.ShouldIgnoreType(t))
                    continue;

                if (filterOnName && MatchesFilter(filter, TypeShortOrFullName(settings, t, filter), settings, ref abort, ref meaningfulMatch))
                {
                    return true;
                }
                if (abort) return false;

                if (t.IsGenericType)
                {
                    foreach (var typeArg in t.GetGenericArguments())
                    {
                        if (MatchesFilter(filter, typeArg.PrettyName, settings, ref abort, ref meaningfulMatch))//, true))
                            return true;
                        if (abort) return false;
                    }

                    if (MatchesFilter(filter, t.PrettyName, settings, ref abort, ref meaningfulMatch))
                        return true;
                    if (abort) return false;
                }
            }

            return false;
        }

        static public CheckForMatch[] Checkers { get; set; } =
        {
            new CheckForStatic(),
            new CheckForVirtuals(),
            new CheckForConstructorMatch(),
            new CheckForDuplicateTypeName(),
            new CheckForMarshalingBehavior(),
            new CheckForThreadingModel(),
            new CheckForNamespace(),
            new CheckForContract(),
            new CheckForTypeRestrictions(),
            new CheckForFilterOnType(),
            new CheckForHasBaseType(),
            new CheckForDuplicateEnumValues(),
            new CheckForCustomInParameters(),
            new CheckForUnobtainableType(),
            new CheckForHasAgileParameter(),
            new CheckForWebHostHidden(),
            new CheckForInParameters(),
            new CheckForReturnValue(),
            new CheckForPrimitiveReturnValue(),
            new CheckForAddedMember(),
            new CheckForMarkerInterface(),
            new CheckForObjectArgs(),
            new CheckForImplementsInternalInterface(),
            new CheckForVersion(),
            new CheckForIsRemoteAsync(),
            new CheckForTrustLevel(),
            new CheckForPlatform(),
            new CheckForDeprecated(),
            //new CheckForAttribute("DeprecatedAttribute", () => MainWindow.InstanceOld.Settings.Deprecated ),
            //new CheckForAttribute("ExperimentalAttribute", () => MainWindow.InstanceOld.Settings.Experimental ),
            new CheckForExperimental(),
            new CheckForInterfaceParameters(),
            new CheckForHasDelegateParameter(),
            new CheckForHasMutableParameter(),
            new CheckForIsRestricted(),
            new CheckForFirstWordIsVerb(),
            new CheckForMutableType(),
            new CheckForActivatable(),
            new CheckForDelegateType(),
            new CheckForEventArgsType(),
            new CheckForDualApi(),
            new CheckForNameAndNamespaceConflict(),
            new CheckForOneWordName(),
            new CheckForMuse(),
            new CheckForUac(),
            new CheckForReturnsHostType(),
            new CheckForMultiVersion(),
            new CheckForInWindows(),
            new CheckForInCustom(),
            new CheckForInWpf(),
            new CheckForFlagsEnum(),
            new CheckForMatchingPropertyAndSetMethod(),
//            new CheckForInPhone(),
            new CheckForBaseDerivedConflict(),
            new CheckForHasApiDesignNotes(),
            new CheckForOutParameters(),
            new CheckForAddedSetters(),
            //new CheckForCondition(),
            new CheckForNotInBaseline()
        };

        static public IEnumerable<MemberViewModel> GetMembersHelper(SearchExpression searchExpression, int iteration)
        {
            DebugLog.Append($"GetMembersHelper ({iteration})");
            LastType = null;
            LastMember = null;

            MatchGeneration++;

            var typesChecked = 0;

            ResetMatchCounts();

            if (CurrentTypeSet == null)
                yield break;

            // Optimized case: no filters or anything else is set, don't need to check anything except the search string

            if (Settings.IsDefault
                && searchExpression.HasNoSearchString
                && searchExpression.SearchCondition == null
                && Settings.Contract == null)
            {
                foreach (var type in CurrentTypeSet.Types)
                {
                    if (iteration == RecalculateIteration)
                    {
                        typesChecked++;
                        TypesChecked = typesChecked;
                    }
                    else
                        yield break;


                    //if (!type.IsPublic && !Settings.InternalInterfaces)
                    if (!TypeIsPublicVolatile(type))
                    {
                        continue;
                    }

                    MatchingStats.MatchingTypes++;
                    type.ReallyMatchedInSearch = true;
                    yield return type;

                    foreach (var m in type.Members)
                    {
                        // Ignore all special names except operator overrides & constructors
                        if (m.IsSpecialName
                            && !m.Name.StartsWith("op_")
                            && !m.IsConstructor)
                        {
                            continue;
                        }

                        // Ignore the constructor of a delegate
                        if (m.IsConstructor && type.IsDelegate)
                        {
                            continue;
                        }

                        UpdateMatchCounts(m);
                        yield return m;
                    }
                }

                // Update the result stats
                PostToUIThread(() => MatchingStats.RaiseAllPropertiesChanged());

                // And we're done
                yield break;
            }

            // Non trivial case: some search flag or filter or some such is set

            var types = CurrentTypeSet.Types;
            for (int i = 0; i < types.Count; i++)
            {
                bool returnedType = false;
                types[i].ReallyMatchedInSearch = false;

                bool abort = false;
                var typeMatchesFilters = false;

                LastType = types[i].Name;

                if (iteration == RecalculateIteration)
                {
                    typesChecked++;
                    TypesChecked = typesChecked;
                }
                else
                {
                    // Another search was started before this one completed, so abort
                    yield break;
                }

                if (!TypeIsPublicVolatile(types[i]))
                {
                    // Internal type, carry on
                    continue;
                }

                bool meaningfulMatch = false;
                var abortType = false;
                var typeMatches = false;
                var meaningfulTypeMatch = false;

                if (Settings.IgnoreAllSettings == true)
                {
                    // We're ignoring settings, so yet the type matches settings
                    typeMatches = meaningfulMatch = true;
                }
                else
                {
                    var matchesCheckers = true;

                    // Validate the type against all the settings and the search expression
                    RunTypeCheckers(searchExpression, types[i], ref abort, ref typeMatchesFilters, ref meaningfulMatch, ref abortType, ref matchesCheckers);

                    // We keep searching even if the type doesn't match, because maybe a member matches.
                    // But sometimes a type mismatch means we don't even look at the members (for example Settings is set
                    // to only show DependencyObject types)
                    if (abortType)
                    {
                        continue;
                    }

                    typeMatches = matchesCheckers;
                }

                if (typeMatches && meaningfulMatch || searchExpression.HasNoSearchString && searchExpression.SearchCondition != null)
                {
                    if (EvaluateWhere.TypeCheck(types[i], searchExpression))
                    {
                        typeMatches = true;
                        meaningfulMatch = true;
                        typeMatchesFilters = true;
                    }
                    else
                    {
                        meaningfulMatch = false;
                        typeMatches = false;
                    }
                }



                meaningfulTypeMatch = meaningfulMatch;

                // If the type doesn't match and this is a "type:member" search string, carry on
                if (!typeMatches && searchExpression.IsTwoPart)
                {
                    continue;
                }

                // If the type matched so far, set a flag on it so that we can show in the UI that it 
                // really matched (as opposed to types we show because a member matched, and the members are
                // grouped by type).

                if (typeMatchesFilters)
                {
                    types[i].SetMatchGeneration();
                }

                bool someMemberMatches = false;
                int membersChecked = 0;
                IEnumerable<MemberViewModel> members;
                members = types[i].Members;

                // Search all the members

                foreach (var member in members)
                {
                    // Ignore the constructor of a delegate
                    if (member.IsConstructor && types[i].IsDelegate)
                    {
                        continue;
                    }

                    abort = false;

                    membersChecked++;

                    LastMember = member.Name;

                    if (!CheckMemberCheckers(
                        member,
                        types[i],
                        searchExpression,
                        ref membersChecked,
                        ref meaningfulMatch,
                        ref abort))
                    {
                        // The member doesn't match for some reason, carry on to the next member
                        continue;
                    }

                    if (meaningfulMatch || searchExpression.MemberRegex == null)
                    {
                        if (!EvaluateWhere.MemberCheck(types[i], member, searchExpression))
                        {
                            meaningfulMatch = false;
                            continue;
                        }
                    }

                    someMemberMatches = true;

                    if (meaningfulMatch && !Settings.IgnoreAllSettings || Settings.IsMemberRequired())
                    {
                        // Flag this as a matching member so that we can highlight it in the UI
                        var memberViewModel = member as MemberViewModel;
                        memberViewModel.SetMatchGeneration();
                    }

                    // bugbug: If this wasn't confusing already, the rest of it is sure to be

                    // If the member matches but the type didn't, we haven't returned the type yet. 
                    // Now we know we need to return it.
                    if (!returnedType /*&& Settings.IsGrouped*/ )
                    {
                        if (ShouldCountAsMatchingType(meaningfulTypeMatch, typeMatchesFilters, Settings))
                        {
                            MatchingStats.MatchingTypes++;
                            types[i].ReallyMatchedInSearch = true;
                        }

                        returnedType = true;
                        yield return types[i];
                    }

                    UpdateMatchCounts(member);

                    yield return member;
                }

                if (typeMatches && searchExpression.IsTwoPart)
                {
                    ;
                }
                else
                {
                    if (typeMatches && !returnedType
                        && (typeMatchesFilters && (someMemberMatches || !Settings.IsMemberRequired())
                            || someMemberMatches
                            || !Settings.IsMemberRequired()
                            || Settings.IgnoreAllSettings)
                        && (Settings.ShowTypes || Settings.IgnoreAllSettings)
                        && (meaningfulTypeMatch || someMemberMatches || searchExpression.SearchCondition == null)
                        )
                    {
                        if (ShouldCountAsMatchingType(meaningfulTypeMatch, typeMatchesFilters, Settings))
                        {
                            MatchingStats.MatchingTypes++;
                            types[i].ReallyMatchedInSearch = true;
                        }
                        else if (Settings.MemberKind == MemberKind.Type)
                        {
                            if (Settings.AreAllMembersDefault(nameof(Settings.MemberKind)))
                            {
                                MatchingStats.MatchingTypes++;
                                types[i].ReallyMatchedInSearch = true;
                            }

                        }

                        returnedType = true;
                        yield return types[i];
                    }
                }
            }

            PostToUIThread(() => MatchingStats.RaiseAllPropertiesChanged());
        }

        private static bool ShouldCountAsMatchingType(bool meaningfulTypeMatch, bool typeMatchesFilters, Settings settings)
        {
            //var ret = settings.IsDefault || typeMatchesFilters;
            var ret = meaningfulTypeMatch;
            return ret;
        }

        public static void RunTypeCheckers(SearchExpression searchExpression, TypeViewModel type, ref bool abort, ref bool typeMatchesFilters, ref bool meaningfulMatch, ref bool abortType, ref bool matchesCheckers)
        {
            foreach (var checker in Checkers)
            {
                var matchesT = false;
                var meaningfulMatchT = false;
                var abortTypeT = false;

                checker.TypeCheck(type, searchExpression, out matchesT, out meaningfulMatchT, out abortTypeT, ref abort);

                matchesCheckers &= matchesT;
                meaningfulMatch |= meaningfulMatchT;
                abortType |= abortTypeT;

                if(abortType)
                {
                    return;
                }

                // bugbug
                if (checker.GetType().Name == "CheckForFilterOnType")
                //if (checker is CheckForFilterOnType)
                {
                    typeMatchesFilters = matchesT && meaningfulMatchT;
                }
            }
        }

        static bool CheckMemberCheckers(
            MemberViewModel member,
            TypeViewModel typeOfMember,
            SearchExpression searchExpression,
            ref int membersChecked,
            ref bool meaningfulMatch,
            ref bool abort)
        {
            // Why is this check here?
            if (member is TypeViewModel)
                return false;


            // When is this not true?
            if (member.DeclaringType != typeOfMember)
                return false;

            if (member is PropertyViewModel && Settings.ShowProperties == false)
                return false;

            if (member is EventViewModel && Settings.ShowEvents == false)
                return false;

            if (member is MethodViewModel && Settings.ShowMethods == false)
                return false;

            if (member is FieldViewModel && Settings.ShowFields == false)
                return false;

            if (member is ConstructorViewModel && Settings.ShowConstructors == false)
                return false;

            var method = new DuckMethod(member);

            var methodInfo = member as MethodViewModel; // MethodInfo;
            var propertyInfo = member as PropertyViewModel; // PropertyInfo;
            var eventInfo = member as EventViewModel; // EventInfo;
            var fieldInfo = member as FieldViewModel; // FieldInfo;

            if (method == null)
                return false;

            bool isExplicit = false;

            if (!ThreeStateCheck(Settings.IsAsync,
                                  (member is MethodViewModel) && (member as MethodViewModel).ReturnType.GetInterface("IAsyncInfo") != null))
            {
                return false;
            }

            if (Settings.PropertyNameMatchesTypeName != null)
            {
                if (!ThreeStateCheck(Settings.PropertyNameMatchesTypeName,
                                    propertyInfo != null && propertyInfo.Name == propertyInfo.PropertyType.Name))
                {
                    return false;
                }
            }

            if (method.IsPrivate)
            {
                var methodName = method.Name as string;
                if (member is PropertyViewModel)
                    methodName = member.Name;
                else if (member is EventViewModel)
                    methodName = member.Name;

                if (methodName.Substring(1).Contains("."))
                {
                    var interfaceName = methodName.Substring(0, methodName.LastIndexOf('.'));
                    var iface = method.DeclaringType.GetInterface(interfaceName);
                    if (iface == null || iface.IsNotPublic && !Settings.InternalInterfaces)
                    {
                        return false;
                    }

                    // explicit interface 
                    isExplicit = true;
                }
                else if (!Settings.InternalInterfaces)
                {
                    return false;
                }
            }

            else if (((member is MethodViewModel) || (member is FieldViewModel))
                            && method.IsSpecialName // Ignore e.g. get_Foo property methods
                            && !method.Name.StartsWith("op_"))// But operator overloads are OK
            {
                membersChecked--;
                return false;
            }

            if (Settings.IsProtected == true && !method.IsProtected
                     || Settings.IsProtected == false && method.IsProtected)
            {
                return false;
            }

            if (Settings.IsProtected == null
                && (!(method.IsPublic || method.IsProtected) && !isExplicit && !Settings.InternalInterfaces))
            {
                membersChecked--;
                return false;
            }

            if (!ThreeStateCheck(Settings.IsExplicit, isExplicit))
            {
                return false;
            }

            if (Settings.IndexedProperty != null)
            {
                if (!ThreeStateCheck(Settings.IndexedProperty, (member is PropertyViewModel) && (member as PropertyViewModel).IndexParameters.Count() != 0))
                {
                    return false;
                }
            }

            if (Settings.CanWrite != null)
            {
                if (!ThreeStateCheck(
                    Settings.CanWrite,
                    (member is PropertyViewModel) && (member as PropertyViewModel).CanWrite
                    ||
                    (member is FieldViewModel) && (member as FieldViewModel).IsInitOnly))
                {
                    return false;
                }
            }



            // 
            bool matchesT = false;
            bool meaningfulT = false;
            bool abortT = false;

            for (int j = 0; j < Checkers.Length; j++)
            {
                Checkers[j].MemberCheck(
                    typeOfMember, searchExpression,
                    propertyInfo,
                    eventInfo,
                    fieldInfo,
                    member as ConstructorViewModel,
                    methodInfo,
                    method,
                    out matchesT,
                    out meaningfulT,
                    ref abortT);

                if (!matchesT)
                    break;
            }

            if (!matchesT)
                return false;

            bool filtersMatch = true;
            if (searchExpression.MemberRegex != null)
            {
                CheckMemberName(member, searchExpression.MemberRegex, ref abort, out filtersMatch, out meaningfulMatch);
            }

            bool isMatch = filtersMatch;

            if (isMatch && Settings.IsOverloaded != null)
            {
                if (!ThreeStateCheck(Settings.IsOverloaded, member.IsOverloaded))
                    isMatch = false;
            }


            if (isMatch && Settings.IsDP != null)//&& m is PropertyInfo)
            {
                if (!(member is PropertyViewModel))
                {
                    isMatch = Settings.IsDP == false;
                }
                else
                {
                    if (!ThreeStateCheck(Settings.IsDP, propertyInfo.IsDependencyProperty))
                        isMatch = false;
                }
            }


            if (isMatch && Settings.IsRoutedEvent != null)
            {
                if (!(member is EventViewModel))
                {
                    if (Settings.IsRoutedEvent == true)
                        isMatch = false;
                }
                else
                {
                    var field = typeOfMember.GetField(member.Name + "Event" /*, BindingFlags.Static | BindingFlags.Public*/); // .Net case
                    if (field != null && field.FieldType.FullName != "System.Windows.RoutedEvent")
                        field = null;

                    var prop = typeOfMember.GetProperty(member.Name + "Event" /*, BindingFlags.Static | BindingFlags.Public*/ ); // WinMD case
                    if (prop != null && prop.PropertyType.FullName != "Windows.UI.Xaml.RoutedEvent" && prop.PropertyType.FullName != "Microsoft.UI.Xaml.RoutedEvent")
                    {
                        prop = null;
                    }

                    if (!ThreeStateCheck(Settings.IsRoutedEvent, field != null || prop != null))
                        isMatch = false;
                }
            }




            if (!isMatch || abort)
                return false;


            return true;
        }

        public static void CheckMemberName(MemberViewModel member, Regex memberRegex, ref bool abort, out bool filtersMatch, out bool meaningfulMatch)
        {
            var method = new DuckMethod(member);
            filtersMatch = false;

            //for (int j = 0; j < 1; j++)
            {

                bool notSearch = false; //= f[0] == '\''; // bugbug
                meaningfulMatch = false;

                if (Settings.FilterOnName && MatchesFilter(memberRegex, member.Name, Settings, ref abort, ref meaningfulMatch))
                {
                    filtersMatch = true;
                }

                if (!filtersMatch || !notSearch)
                {
                    if (Settings.FilterOnReturnType)
                    {
                        if (member is PropertyViewModel
                            && MatchesFilterString(memberRegex, (member as PropertyViewModel).PropertyType, true, /*filterOnBaseTypes*/ true, Settings, ref abort, ref meaningfulMatch))
                        {
                            filtersMatch = true;
                        }
                        else if (member is MethodViewModel
                                 && MatchesFilterString(memberRegex, (member as MethodViewModel).ReturnType, true, /*filterOnBaseTypes*/ true, Settings, ref abort, ref meaningfulMatch))
                        {
                            filtersMatch = true;
                        }
                        else if (member is FieldViewModel
                                 && !(member as FieldViewModel).DeclaringType.IsEnum
                                 && MatchesFilterString(memberRegex, (member as FieldViewModel).FieldType, true, /*filterOnBaseTypes*/ true, Settings, ref abort, ref meaningfulMatch))
                        {
                            filtersMatch = true;
                        }
                    }

                    if (Settings.FilterOnAttributes)
                    {
                        if (MatchesAttributes(member as BaseViewModel, memberRegex, Settings, ref abort, ref meaningfulMatch))
                        {
                            filtersMatch = true;
                        }
                    }

                }


                if (!filtersMatch && !(member is FieldViewModel) && Settings.FilterOnParameters)
                {
                    var parameters = new List<ParameterViewModel>(method.GetParameters());

                    // For events, include the sender & args as parameters
                    if (member is EventViewModel)
                    {
                        var eventVM = member as EventViewModel;
                        var invoker = eventVM.Invoker;
                        if (invoker != null)
                        {
                            parameters.AddRange(eventVM.Invoker.Parameters);
                        }
                    }
                    else
                    {
                        // For delegate-typed properties, include the delegate's parameters for this member's 
                        // parameter search.

                        var propertyVM = member as PropertyViewModel;
                        if (propertyVM != null && IsReallyADelegate(propertyVM.ReturnType))
                        {
                            var invokeMethod = propertyVM.ReturnType.Methods[0];
                            parameters.AddRange(invokeMethod.Parameters);
                        }

                        // And for delegate-typed method parameters, include those delegate parameters

                        var copyParameters = new ParameterViewModel[parameters.Count];
                        parameters.CopyTo(copyParameters, 0);

                        foreach (var parameter in copyParameters)
                        {
                            if (IsReallyADelegate(parameter.ParameterType))
                            {
                                var invokeMethod = parameter.ParameterType.Methods[0];
                                parameters.AddRange(invokeMethod.Parameters);
                            }
                        }
                    }

                    foreach (var parameter in parameters)
                    {
                        if (Settings.FilterOnName && MatchesFilter(memberRegex, parameter.Name, Settings, ref abort, ref meaningfulMatch)
                            ||
                            MatchesFilterString(memberRegex, parameter.ParameterType, true, /*filterOnBaseTypes*/ true, Settings, ref abort, ref meaningfulMatch))
                        {
                            filtersMatch = true;
                        }
                    }
                }

                if (!filtersMatch && Settings.FilterOnDeclaringType)
                {
                    if (MatchesFilterString(memberRegex, member.DeclaringType, true, /*filterOnBaseTypes*/ true, Settings, ref abort, ref meaningfulMatch))
                    {
                        filtersMatch = true;
                    }
                }

                //if (!filtersMatch)
                //    break;
            }

            return;
        }

        private static bool IsReallyADelegate(TypeViewModel t)
        {
            // bugbug: when does a Delegate not have an Invoke method?
            // Maybe this is an issue where it shows up in reflection on WinMDs but not assemblies?
            return t.IsDelegate && t.HasMethods;
        }

        public static bool MatchesSetting(bool? setting, object v)
        {
            throw new NotImplementedException();
        }

        static public void ResetMatchCounts()
        {
            MatchingStats.Reset();
        }

        public static void UpdateMatchingTypeCount()
        {
            MatchingStats.MatchingTypes++;
            Debug.WriteLine(MatchingStats.MatchingTypes);
        }
        public static void UpdateMatchCounts(BaseViewModel m)
        {
            if (m is MethodViewModel)
                MatchingStats.MatchingMethods++;
            else if (m is PropertyViewModel)
                MatchingStats.MatchingProperties++;
            else if (m is ConstructorViewModel)
                MatchingStats.MatchingConstructors++;
            else if (m is EventViewModel)
                MatchingStats.MatchingEvents++;
            else if (m is FieldViewModel)
                MatchingStats.MatchingFields++;
        }


    }

    public class SettingsHack : INotifyPropertyChanged
    {
        public Settings Settings { get { return Manager.Settings; } }

        public event PropertyChangedEventHandler PropertyChanged;

        PropertyChangedEventArgs _args = new PropertyChangedEventArgs("");
        public void Reset()
        {
            PropertyChanged?.Invoke(this, _args);
        }
    }


    public class MatchingStats : INotifyPropertyChanged
    {
        public int MatchingTypes { get; set; }
        public int MatchingProperties { get; set; }
        public int MatchingEvents { get; set; }
        public int MatchingMethods { get; set; }
        public int MatchingFields { get; set; }
        public int MatchingConstructors { get; set; }

        public int MatchingTotal
        {
            get
            {
                return MatchingTypes + MatchingProperties + MatchingMethods + MatchingEvents + MatchingFields + MatchingConstructors;
            }
        }

        public int MatchingDocPages
        {
            get
            {
                return MatchingTotal - MatchingFields;
            }
        }

        public void Reset()
        {
            // bugbug: not thread safe
            MatchingTypes = MatchingProperties = MatchingEvents = MatchingMethods = MatchingFields = MatchingConstructors = 0;

        }

        PropertyChangedEventArgs _args = new PropertyChangedEventArgs("");
        public void RaiseAllPropertiesChanged()
        {
            PropertyChanged?.Invoke(this, _args);
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
