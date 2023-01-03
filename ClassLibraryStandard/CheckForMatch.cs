using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Tempo
{

    public class CheckForConstructorMatch : CheckForMatch
    {
        public override void TypeCheck(
            TypeViewModel t,
            SearchExpression filter,
            out bool matches,
            out bool meaningful,
            out bool abortType,
            ref bool abort)
        {
            meaningful = abortType = false;
            matches = true;

            var settings = Manager.Settings;

            var constructorSettings = new Collection<Nullable<bool>>()
                {
                    settings.HasProtectedConstructors,
                    settings.HasPublicConstructors,
                    settings.HasStaticConstructor,
                    settings.HasDefaultConstructor,
                    settings.HasNonDefaultConstructor
                };

            bool somethingToDo = false;
            foreach (var val in constructorSettings)
            {
                if (val != null)
                    somethingToDo = true;
            }
            if (!somethingToDo)
                return;


            //bool foundMatch = false;

            var constructors = t.Constructors; //t.GetConstructors(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (constructors.Count == 0)
            {
                //bool foundMatch = true;

                foreach (var s in constructorSettings)
                {
                    if (s == true)
                    {
                        matches = false;
                        meaningful = true;
                        abortType = true;
                        return;
                    }
                }

                return;
            }


            bool hasProtectedConstructor = false;
            bool hasPublicConstructor = false;
            bool hasDefaultConstructor = false;
            bool hasNonDefaultConstructor = false;
            bool hasStaticConstructor = false;

            foreach (var c in constructors)
            {
                if (c.IsPrivate && !c.IsStatic || c.IsAssembly) // Static constructors are private
                    continue;

                if (c.IsFamily)
                    hasProtectedConstructor = true;

                if (c.IsPublic)
                    hasPublicConstructor = true;

                if (c.Parameters == null || c.Parameters.Count == 0)
                    hasDefaultConstructor = true;

                if (c.Parameters != null && c.Parameters.Count != 0)
                    hasNonDefaultConstructor = true;

                if (c.IsStatic)
                    hasStaticConstructor = true;

                //bool? check;

                //check = CheckConstructorSetting(Settings.HasProtectedConstructors, c.IsFamily);
                //if (check == null)
                //    return false;
                //else
                //    foundMatch |= (bool)check;


                //check = CheckConstructorSetting(Settings.HasPublicConstructors, c.IsPublic);
                //if (check == null)
                //    return false;
                //else
                //    foundMatch |= (bool)check;

                //if (c.IsPublic)
                //{
                //    check = CheckConstructorSetting(Settings.HasDefaultConstructor, c.GetParameters().Length == 0);
                //    if (check == null)
                //        return false;
                //}
                //else
                //    foundMatch |= (bool)check;

                //check = CheckConstructorSetting(Settings.HasStaticConstructor, c.IsStatic);
                //if (check == null)
                //    return false;
                //else
                //    foundMatch |= (bool)check;

            }

            if (Manager.ThreeStateCheck(settings.HasProtectedConstructors, hasProtectedConstructor)
                && Manager.ThreeStateCheck(settings.HasPublicConstructors, hasPublicConstructor)
                && Manager.ThreeStateCheck(settings.HasDefaultConstructor, hasDefaultConstructor)
                && Manager.ThreeStateCheck(settings.HasNonDefaultConstructor, hasNonDefaultConstructor)
                && Manager.ThreeStateCheck(settings.HasStaticConstructor, hasStaticConstructor))
            {
                meaningful = true;
                matches = true;
                return;
            }

            matches = false;
            abortType = true;
            return;

            //if (foundMatch)
            //    meaningfulMatch = true;

            //return foundMatch;
        }


    }



    public class CheckForDuplicateTypeName : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;
            if (settings.DuplicateTypeName == false)
                return;

            foreach (var type in Manager.CurrentTypeSet.Types)
            {
                if (type.Name == t.Name && type != t)
                {
                    matches = true;
                    meaningful = true;
                    return;
                }
            }

            abortType = true;
        }
    }



    public class CheckForMarshalingBehavior : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;
            if (settings.MarshalingBehavior == Settings.MarshalingBehaviorDefault
                || !t.IsWinMD)
            {
                return;
            }

            //bool found = false;

            matches = false;

            //IList<CustomAttributeViewModel> attrs = null;

            //try
            //{
            //    attrs = t.CustomAttributes;// t.CustomAttributesData;
            //}
            //catch (Exception e)
            //{
            //    if (!e.Message.Contains("GCPressure"))
            //        throw;
            //}

            //if (attrs == null)
            //    return;

            ////var attrs = t.GetCustomAttributes(Settings.FilterOnBaseType);

            //foreach (var attr in attrs)
            //{
            //    var attributeClassName = attr.Name;// attr.Constructor.DeclaringType.Name;
            //    if (attributeClassName != "MarshalingBehaviorAttribute")
            //        continue;

            //    if (settings.MarshalingBehavior == Settings.MarshalingBehaviorUnspecified)
            //    {
            //        found = true;
            //        break;
            //    }

            //    var argument = attr.ConstructorArguments[0];
            //    if ((int)argument.Value == settings.MarshalingBehaviorValue)
            //    {
            //        matches = true;
            //        meaningful = true;
            //        return;
            //    }
            //}


            if (t.MarshalingType == (TempoMarshalingType)settings.MarshalingBehaviorValue)
            {
                matches = true;
                meaningful = true;
            }

            else if (t.MarshalingType == TempoMarshalingType.Unspecified && settings.MarshalingBehavior == Settings.MarshalingBehaviorUnspecified)
            {
                matches = true;
                meaningful = true;
            }

            else if (t.MarshalingType != TempoMarshalingType.Agile && settings.MarshalingBehavior == Settings.MarshalingBehaviorNonAgile)
            {
                matches = true;
                meaningful = true;
            }

            if (!matches)
                abortType = true;
        }

    }




    public class CheckForThreadingModel : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;
            if (settings.ThreadingModel == Settings.ThreadingModelDefault
                || !t.IsWinMD)
            {
                return;
            }


            matches = false;


            //IList<CustomAttributeViewModel> attrs = null;

            //try
            //{
            //    attrs = t.CustomAttributes;// CustomAttributesData;
            //}
            //catch (Exception e)
            //{
            //    if (!e.Message.Contains("GCPressure"))
            //        throw;
            //}

            //if (attrs == null)
            //    return;



            //var attrs = t.GetCustomAttributes(Settings.FilterOnBaseType);

            //bool found = false;
            //foreach (var attr in attrs)
            //{
            //    var attributeClassName = attr.Name;// attr.Constructor.DeclaringType.Name;
            //    if (attributeClassName != "ThreadingAttribute")
            //        continue;

            //    if (settings.ThreadingModel == Settings.ThreadingModelUnspecified)
            //    {
            //        found = true;
            //        break;
            //    }

            //    var argument = attr.ConstructorArguments[0];
            //    if ((int)argument.Value == settings.ThreadingModelValue)
            //    {
            //        matches = true;
            //        meaningful = true;
            //        return;
            //    }
            //}


            //if (!found && !matches && settings.ThreadingModel == Settings.ThreadingModelUnspecified)
            //{
            //    matches = true;
            //    meaningful = true;
            //}

            if (t.ThreadingModelValue == settings.ThreadingModelValue)
            {
                matches = true;
                meaningful = true;
                return;
            }

            if (!matches)
                abortType = true;
        }

    }



    public class CheckForPlatform : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;
            if (settings.SdkPlatform == SdkPlatform.Any
                || !t.IsWinMD)
            {
                return;
            }

            if(settings.SdkPlatform == t.SdkPlatform)
            {
                matches = true;
                meaningful = true;
            }
            else
            {
                matches = false;
                abortType = true;
            }
        }

    }



    public class CheckForTrustLevel : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;
            if (settings.TrustLevel == TrustLevel.Any
                || !t.IsWinMD)
            {
                return;
            }

            matches = false;

            var acidInfo = MRTypeViewModel.GetAcidInfo(t);
            if (acidInfo == null && settings.TrustLevel == TrustLevel.Any
                || acidInfo.TrustLevelValue == settings.TrustLevel)
            {
                matches = true;
                meaningful = true;
            }
            else
                abortType = true;

        }

    }



    public class CheckForVersion : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;
            if (settings.VersionString == null)
            {
                return;
            }

            matches = false;

            if (t.Version != null && t.Version.Contains(settings.VersionString))
            {
                matches = meaningful = true;
                return;
            }


        }



        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            var versionString = Manager.Settings.VersionString;

            if (versionString == null)
            {
                return;
            }


            matches = false;

            var memberViewModel = MemberOrTypeViewModelBase.FirstNotNull(propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo);
            Debug.Assert(memberViewModel != null);
            if (memberViewModel.Version.Contains(versionString))
            //||
            //memberViewModel.VersionIsUnknown && settings.VersionString == Settings.OtherVersionString)
            {
                matches = meaningful = true;
                return;
            }




        }
    }



    public class CheckForContract : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;
            var selectedContract = settings.Contract;

            if (selectedContract == null || selectedContract.Count == 0)
                return;

            matches = selectedContract.Contains(t.Contract);
        }

        public override void MemberCheck(TypeViewModel t, SearchExpression filter, PropertyViewModel propertyInfo, EventViewModel eventInfo, FieldViewModel fieldInfo, ConstructorViewModel constructorInfo, MethodViewModel methodInfo, DuckMethod effectiveMethod, out bool matches, out bool meaningful, ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            var settings = Manager.Settings;
            var selectedContract = settings.Contract;

            if (selectedContract == null || selectedContract.Count == 0)
                return;

            matches = selectedContract.Contains(effectiveMethod.Contract);
        }
    }




    public class CheckForOneWordName : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;
            if (settings.OneWordName == null)
                return;

            matches = false;


            if (Manager.ThreeStateCheck(settings.OneWordName, t.WordCount == 1))
            {
                matches = true;
                meaningful = true;
            }
            else
                abortType = true;

        }

        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            var settings = Manager.Settings;
            if (settings.OneWordName == null)
                return;

            matches = false;

            if (Manager.ThreeStateCheck(Manager.Settings.OneWordName, effectiveMethod.WordCount == 1))
            {
                matches = true;
                meaningful = true;
            }
        }

    }






    public class CheckForNameAndNamespaceConflict : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;
            if (settings.NameAndNamespaceConflict == null)
                return;

            matches = false;
            var found = false;

            foreach (var type in t.TypeSet.Types)
            {
                if (type.Namespace.EndsWith("." + t.Name))
                    found = true;
            }



            if (Manager.ThreeStateCheck(settings.NameAndNamespaceConflict, found))
            {
                matches = true;
                meaningful = true;
            }
            else
                abortType = true;

        }
    }



    public class CheckForDualApi : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;
            if (settings.DualApi == null)
                return;

            matches = false;


            //var attrs = t.CustomAttributes;// CustomAttributesData;

            //bool found = false;
            //if (attrs != null)
            //{
            //    foreach (var attr in attrs)
            //    {
            //        var attributeClassName = attr.Name;// attr.Constructor.DeclaringType.Name;
            //        if (attributeClassName == "DualApiPartitionAttribute")
            //        {
            //            found = true;
            //            break;
            //        }
            //    }
            //}

            if (Manager.ThreeStateCheck(settings.DualApi, t.IsDualApi))
            {
                matches = true;
                meaningful = true;
            }
            else
                abortType = true;

        }
    }


    public class CheckForMuse : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;
            if (settings.IsMuse == null)
                return;

            matches = false;


            if (Manager.ThreeStateCheck(settings.IsMuse, t.IsMuse))
            {
                matches = true;
                meaningful = true;
            }
            else
                abortType = true;

        }
    }


    public class CheckForMutableType : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;
            if (settings.IsMutableType == null)
                return;

            matches = false;
            if (Manager.ThreeStateCheck(settings.IsMutableType, t.IsMutable))
            {
                matches = true;
                meaningful = true;
            }
            else
                abortType = true;

        }
    }




    public class CheckForUac : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;
            if (settings.IsUac == null)
                return;

            matches = false;


            if (Manager.ThreeStateCheck(settings.IsUac, t.IsUac))
            {
                matches = true;
                meaningful = true;
            }
            else
                abortType = true;

        }
    }


    public delegate bool? CheckSetting();

    public class CheckForDeprecated : CheckForMatch
    {

        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            if (Manager.Settings.Deprecated == null)
                return;

            matches = false;

            if (Manager.ThreeStateCheck(Manager.Settings.Deprecated, t.IsDeprecated))
            {
                matches = true;
                meaningful = true;
            }

        }

        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            if (Manager.Settings.Deprecated == null)
                return;

            matches = false;

            if (Manager.ThreeStateCheck(Manager.Settings.Deprecated, effectiveMethod.IsDeprecated))
            {
                matches = true;
                meaningful = true;
            }
        }

    }


    public class CheckForExperimental : CheckForMatch
    {
        public CheckForExperimental()
        {
        }

        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;
            if (settings.Experimental == null)
                return;

            matches = false;


            if (Manager.ThreeStateCheck(settings.Experimental, t.IsExperimental))
            {
                matches = true;
                meaningful = true;
            }

        }

        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            var settings = Manager.Settings;
            if (settings.Experimental == null)
                return;

            matches = false;


            if (Manager.ThreeStateCheck(settings.Experimental, effectiveMethod.IsExperimental))
            {
                matches = true;
                meaningful = true;
            }
        }

    }


    public class CheckForHasBaseType : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;
            if (settings.HasBaseType == null)
                return;


            var hasBaseType = t.BaseType != null && !t.BaseType.ShouldIgnore;// Type2Ancestors.ShouldIgnoreType(t.BaseType);

            if (settings.HasBaseType == true && hasBaseType
                || settings.HasBaseType == false && !hasBaseType)
            {
                matches = true;
                return;
            }
            else
                abortType = true;
        }
    }


    public class CheckForImplementsInternalInterface : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;
            if (settings.ImplementsInternalInterface == null)
                return;

            var implementsInternalInterface = false;
            var interfaces = t.CalculateInterfacesFromType(includeInternal: true); // t.GetInterfaces();
            foreach (var i in interfaces)
            {
                if (!i.IsPublic)
                {
                    implementsInternalInterface = true;
                    break;
                }
            }


            if (Manager.MatchesSetting(
                    Manager.Settings.ImplementsInternalInterface,
                    implementsInternalInterface))
            {
                matches = true;
            }
            else
            {
                matches = false;
                abortType = true;
            }

        }
    }




    public class CheckForActivatable : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            if (Manager.Settings.IsActivatable == null)
                return;


            if (Manager.ThreeStateCheck(Manager.Settings.IsActivatable, t.IsActivatable))
            {
                matches = true;
                meaningful = true;
            }
            else
            {
                matches = false;
                abortType = true;
            }

            return;
        }
    }



    public class CheckForFlagsEnum : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            if (Manager.Settings.IsFlagsEnum == null)
                return;


            if (Manager.ThreeStateCheck(Manager.Settings.IsFlagsEnum, t.IsFlagsEnum))
            {
                matches = true;
                meaningful = true;
            }
            else
            {
                matches = false;
                abortType = true;
            }

            return;
        }
    }


    public class CheckForMarkerInterface : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            var isMarker = true;

            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            if (Manager.Settings.MarkerInterfaces == null)
                return;


            if (!t.IsInterface)
                isMarker = false;
            else
            {
                if (!Manager.TypeIsPublicVolatile(t))
                    isMarker = false;
                else
                {
                    var interfaces = t.Interfaces;//.GetInterfaces();
                    if (interfaces != null && interfaces.Count() != 0)
                        isMarker = false;
                    else
                    {
                        var members = t.Members; // .GetMembers();
                        if (members != null && members.Count() != 0)
                            isMarker = false;
                    }
                }
            }

            if (isMarker)
            {
                if (HasDerivedInterfaceVolatile(t))
                    isMarker = false;
            }

            if (Manager.MatchesSetting(
                    Manager.Settings.MarkerInterfaces,
                    isMarker))
            {
                matches = true;
            }
            else
            {
                matches = false;
                abortType = true;
            }
        }

        internal bool HasDerivedInterfaceVolatile(TypeViewModel type)
        {
            if (!type.IsInterface)
                return false;

            foreach (var checkType in Manager.CurrentTypeSet.Types)
            {
                if (!checkType.IsInterface)
                    continue;

                if (!Manager.TypeIsPublicVolatile(checkType/*.Type*/))
                    continue;

                foreach (var i in checkType.Interfaces) //.GetInterfaces())
                {
                    if (i == type)
                        return true;
                }

            }

            return false;
        }

    }


    public class CheckForDelegateType : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            if (Manager.Settings.IsDelegateType == null)
                return;

            if (Manager.ThreeStateCheck(
                Manager.Settings.IsDelegateType, t.IsDelegate))
            {
                matches = true;
                meaningful = true;
            }
            else
            {
                matches = false;
                abortType = true;
            }


        }
    }

    public class CheckForEventArgsType : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            if (Manager.Settings.IsEventArgsType == null)
                return;

            if (Manager.ThreeStateCheck(
                Manager.Settings.IsEventArgsType, t.IsEventArgs))
            {
                matches = true;
                meaningful = true;
            }
            else
            {
                matches = false;
                abortType = true;
            }


        }
    }



    public class CheckForMultiVersion : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            var isMultiVersion = false;

            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            if (Manager.Settings.IsMultiVersion == null)
                return;

            if (!t.IsEnum && !t.IsClass)
            {
                abortType = true;
                return;
            }

            foreach (var m in t.Members)
            {
                if (m.Version != t.Version)
                {
                    isMultiVersion = true;
                    break;
                }
            }

            if (Manager.ThreeStateCheck(
                Manager.Settings.IsMultiVersion, isMultiVersion))
            {
                matches = true;
                meaningful = true;
            }
            else
            {
                matches = false;
                abortType = true;
            }


        }
    }


    public class CheckForUnobtainableType : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            if (Manager.Settings.UnobtainableType == false)
                return;

            if (t.IsValueType)
            {
                matches = false;
                abortType = true;
                return;
            }

            if (!t.IsSealed)
            {
                matches = false;
                abortType = true;
                return;
            }


            //var members = Type2Methods.GetMethods(t, false).AsEnumerable<MemberInfo>()
            //                .Union(Type2Properties.GetProperties(t, false).AsEnumerable<MemberInfo>())
            //                .Union(Type2Fields.GetFields(t, false).AsEnumerable<MemberInfo>())
            //                .Union(Type2Events.GetEvents(t, false).AsEnumerable<MemberInfo>());

            var members = t.Members;

            foreach (var member in members)
            {
                //dynamic dm = member;
                //if (member is PropertyInfo)
                //{
                //    dm = (member as PropertyInfo).GetGetMethod();
                //}
                //else if (member is EventInfo)
                //{
                //    dm = (member as EventInfo).GetAddMethod();
                //}


                if (member != null && member.IsStatic)
                {
                    matches = false;
                    abortType = true;
                    return;
                }
            }



            var constructors = t.Constructors;
            if (constructors != null && constructors.Any())
            {
                matches = false;
                abortType = true;
                return;
            }

            var referencingTypes = TypeReferenceHelper.FindReferencingTypes(t, -1);
            if (referencingTypes != null && referencingTypes.Any())
            {
                matches = false;
                abortType = true;
                return;
            }

            matches = true;
            return;

        }
    }



    public class CheckForIsRemoteAsync : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            if (Manager.Settings.IsRemoteAsync == null)
                return;

            matches = false;
            bool isRemoteAsync = false;

            var memberViewModel = GetMemberViewModel(propertyInfo, methodInfo, eventInfo, constructorInfo, fieldInfo);
            if (memberViewModel != null && memberViewModel.IsRemoteAsync)
                isRemoteAsync = true;

            if (Manager.MatchesSetting(Manager.Settings.IsRemoteAsync, isRemoteAsync))
                matches = true;

        }

    }


    public class CheckForFirstWordIsVerb : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            if (Manager.Settings.IsFirstWordAVerb == null)
                return;

            matches = false;

            if (Manager.MatchesSetting(Manager.Settings.IsFirstWordAVerb, t.IsFirstWordAVerb))
            {
                matches = true;
            }
            else
            {
                matches = false;
                meaningful = true;
            }

        }

        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            if (Manager.Settings.IsFirstWordAVerb == null)
                return;

            matches = false;
            bool isFirstWordAVerb = false;

            var memberViewModel = GetMemberViewModel(propertyInfo, methodInfo, eventInfo, constructorInfo, fieldInfo);
            if (memberViewModel != null && memberViewModel.IsFirstWordAVerb)
                isFirstWordAVerb = true;

            if (Manager.MatchesSetting(Manager.Settings.IsFirstWordAVerb, isFirstWordAVerb))
                matches = true;

        }

    }

    public class CheckForIsRestricted : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var isRestrictedSetting = Manager.Settings.IsRestricted;

            if (isRestrictedSetting == null)
                return;

            if (Manager.MatchesSetting(isRestrictedSetting, t.IsRestricted))
            {
                matches = true;
            }
            else
            {
                matches = false;
                meaningful = true;
                abortType = true;
            }

        }


        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            var isRestrictedSetting = Manager.Settings.IsRestricted;

            if (isRestrictedSetting == null)
                return;


            if (Manager.MatchesSetting(isRestrictedSetting, effectiveMethod.IsRestricted))
            {
                matches = true;
            }

        }


    }

    public class CheckForHasApiDesignNotes : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var setting = Manager.Settings.HasApiDesignNotes;

            if (setting == null)
                return;

            ApiComments.EnsureSync(t.TypeSet.Types);
            
            if (Manager.MatchesSetting(setting, !string.IsNullOrEmpty(t.ApiDesignNotes)))
            {
                matches = true;
            }
            else
            {
                matches = false;
                meaningful = true;
                abortType = true;
            }

        }


        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            var setting = Manager.Settings.IsRestricted;

            if (setting == null)
                return;

            if (Manager.MatchesSetting(
                setting, 
                !string.IsNullOrEmpty(GetMemberViewModel(propertyInfo, methodInfo, eventInfo, constructorInfo, fieldInfo).ApiDesignNotes)))
            {
                matches = true;
            }
        }
    }



    public class CheckForHasInterfaceParameter : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            if (Manager.Settings.HasInterfaceParameter == null)
                return;

            matches = false;
            bool has = false;

            if (methodInfo != null)
            {
                foreach (var parameter in methodInfo.Parameters)
                {
                    if (parameter.ParameterType.IsInterface)
                    {
                        has = true;
                        break;
                    }
                }
            }

            if (Manager.MatchesSetting(Manager.Settings.HasInterfaceParameter, has))
                matches = true;

        }

    }



    public class CheckForHasDelegateParameter : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            if (Manager.Settings.HasDelegateParameter == null)
                return;

            matches = false;
            bool has = false;

            if (methodInfo != null)
            {
                foreach (var parameter in methodInfo.Parameters)
                {
                    if (parameter.ParameterType.IsDelegate)
                    {
                        has = true;
                        break;
                    }
                }
            }

            if (Manager.MatchesSetting(Manager.Settings.HasDelegateParameter, has))
                matches = true;

        }

    }

    public class CheckForHasMutableParameter : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            if (Manager.Settings.HasMutableParameter == null)
                return;

            matches = false;
            bool has = false;

            if (methodInfo != null)
            {
                foreach (var parameter in methodInfo.Parameters)
                {
                    if (parameter.ParameterType.IsMutable)
                    {
                        has = true;
                        break;
                    }
                }
            }

            if (Manager.MatchesSetting(Manager.Settings.HasMutableParameter, has))
                matches = true;

        }

    }







    public class CheckForHasAgileParameter : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            if (Manager.Settings.HasAgileParameter == null)
                return;

            matches = false;
            bool has = false;

            if (methodInfo != null)
            {
                foreach (var parameter in methodInfo.Parameters)
                {
                    if (parameter.ParameterType.MarshalingType == TempoMarshalingType.Agile)
                    {
                        has = true;
                        break;
                    }
                }
            }

            if (Manager.MatchesSetting(Manager.Settings.HasAgileParameter, has))
                matches = true;

        }

    }






    public class CheckForCustomInParameters : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            if (Manager.Settings.CustomInParameters == null)
                return;

            matches = false;
            TypeViewModel type = null;
            IEnumerable<ParameterViewModel> parameters = null;
            bool has = false;

            if (propertyInfo != null)
            {
                type = propertyInfo.PropertyType;
            }
            else if (methodInfo != null)
            {
                parameters = methodInfo.Parameters;// GetParameters();
            }
            else if (constructorInfo != null)
            {
                parameters = constructorInfo.Parameters;//.GetParameters();
            }
            else if (fieldInfo != null)
            {
                type = fieldInfo.FieldType;
            }

            if (type != null)
            {
                has = !IsFoundational(type);
            }

            if (!has && parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    if (parameter.IsIn && !IsFoundational(parameter.ParameterType))
                    {
                        has = true;
                        break;
                    }

                }
            }


            if (Manager.MatchesSetting(Manager.Settings.CustomInParameters, has))
                matches = true;

        }

        bool IsFoundational(TypeViewModel t)
        {
            if (t.FullName == "System.Object")
                return false;

            if (t.IsEnum || t.IsValueType)
                return true;

            return
                t.Namespace.StartsWith("Windows")
                || t.Namespace.StartsWith("System");
        }
    }


    public class CheckForMatchingPropertyAndSetMethod : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            if (Manager.Settings.HasMatchingPropertyAndSetMethod == null)
                return;

            matches = false;

            var found = false;
            if (propertyInfo != null)
            {
                var name = "Set" + propertyInfo.Name;
                foreach (var method in t.Methods)
                {
                    if (name == method.Name)
                    {
                        found = true;
                        break;
                    }
                }
            }
            else if (methodInfo != null)
            {
                if (methodInfo.Name.StartsWith("Set"))
                {
                    var name = methodInfo.Name.Substring(3);
                    foreach (var property in t.Properties)
                    {
                        if (name == property.Name)
                        {
                            found = true;
                            break;
                        }
                    }
                }
            }


            if (Manager.MatchesSetting(Manager.Settings.HasMatchingPropertyAndSetMethod, found))
                matches = true;

        }

    }


    public class CheckForBaseDerivedConflict : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            if (Manager.Settings.ConflictingOverrides == null)
                return;

            matches = false;
            var conflict = false;

            if (constructorInfo != null)
                return;


            var searchMember = GetMemberViewModel(propertyInfo, methodInfo, eventInfo, constructorInfo, fieldInfo);

            var type = t.BaseType;
            while (type != null)
            {
                foreach (var member in type.Members)
                {
                    if (member.Name == searchMember.Name)
                    {
                        var method = member as MethodViewModel;
                        if (method == null || methodInfo == null || method.Parameters.Count == methodInfo.Parameters.Count)
                        {
                            conflict = true;
                            break;
                        }
                    }
                }

                type = type.BaseType;
            }



            if (Manager.MatchesSetting(Manager.Settings.ConflictingOverrides, conflict))
                matches = true;

        }

    }





    public class CheckForDuplicateEnumValues : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            if (Manager.Settings.DuplicateEnumValues == null)
                return;

            matches = false;
            var found = false;

            if (t.IsEnum && fieldInfo != null)
            {
                var value = fieldInfo.RawConstantValue;

                foreach (var member in t.Members)
                {
                    var field = member as FieldViewModel;
                    if (field == fieldInfo)
                        break;

                    // Have to do Equals, because it's a boxed value, and boxes do reference equality.
                    // (And it can't be a type cast, because sometimes the fields are an int and sometimes a unint.)
                    if (field.RawConstantValue.Equals(value))
                    {
                        found = true;
                        break;
                    }
                }
            }


            if (Manager.MatchesSetting(Manager.Settings.DuplicateEnumValues, found))
                matches = true;

        }

    }




    public class CheckForWebHostHidden : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            if (Manager.Settings.IsWebHostHidden == null)
                return;

            //IList<CustomAttributeViewModel> attrs = null;

            if (Manager.MatchesSetting(
                Manager.Settings.IsWebHostHidden,
                t.IsWebHostHidden))
            {
                matches = true;
            }
            else
            {
                matches = false;
                meaningful = true;
                abortType = true;
            }


        }
    }



    public class CheckForInParameters : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            if (Manager.Settings.HasInParameter == null)
                return;

            matches = false;
            IEnumerable<ParameterViewModel> parameters = null;
            bool has = false;

            if (methodInfo != null)
            {
                parameters = methodInfo.Parameters;// GetParameters();
            }
            else if (constructorInfo != null)
            {
                parameters = constructorInfo.Parameters;//.GetParameters();
            }

            if (parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    if (parameter.IsIn)
                    {
                        has = true;
                        break;
                    }

                }
            }


            if (Manager.MatchesSetting(Manager.Settings.HasInParameter, has))
                matches = true;

        }

    }



    public class CheckForReturnValue : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);
            
            if (Manager.Settings.HasReturnValue == null)
                return;


            matches = false;

            if (Manager.MatchesSetting(
                    Manager.Settings.HasReturnValue,
                    (methodInfo != null && !methodInfo.ReturnType.IsVoid
                     || propertyInfo != null && propertyInfo.CanRead)
                    ))
            {
                matches = true;
            }


        }

    }



    public class CheckForPrimitiveReturnValue : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            if (Manager.Settings.HasPrimitiveReturnValue == null)
                return;

            matches = false;
            bool isPrimitive = false;

            TypeViewModel returnType = null;
            if (methodInfo != null)
                returnType = methodInfo.ReturnType;
            else if (propertyInfo != null && propertyInfo.CanRead)
                returnType = propertyInfo.PropertyType;

            if (returnType != null)
            {
                if (returnType.IsValueType)
                    isPrimitive = true;
                else if (returnType.FullName == "System.String"
                         || returnType.FullName == "String") // MR metadata case
                {
                    isPrimitive = true;
                }
            }


            if (Manager.MatchesSetting(
                    Manager.Settings.HasPrimitiveReturnValue,
                    isPrimitive))
            {
                matches = true;
            }


        }

    }




    public class CheckForAddedMember : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            if (Manager.Settings.IsAddedMember == null)
                return;


            matches = false;


            //if (t.Name == "OrientationSensor")
            //{
            //    var v = propertyInfo.Getter.Version;
            //}

            // For some reason the getter method shows up as an earlier version (the declarying type's version?)
            // Rather than the property's version, so we can't rely on effectiveMethod.Version.

            if (Manager.MatchesSetting(
                    Manager.Settings.IsAddedMember,
                    effectiveMethod.Version != effectiveMethod.DeclaringType.Version))
            {
                matches = true;
            }


        }

    }



    public class CheckForVirtuals : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            var settings = Manager.Settings;

            if (settings.IsVirtual == null
                && settings.IsAbstract == null
                && settings.IsOverride == null
                && settings.IsFinal == null)
            {
                return;
            }


            matches = false;

            if (fieldInfo != null || constructorInfo != null)
            {
                if (settings.IsVirtual == true
                    || settings.IsAbstract == true
                    || settings.IsOverride == true
                    || settings.IsFinal == true)
                {
                    return;
                }
            }
            else
            {
                if (!Manager.ThreeStateCheck(settings.IsVirtual, effectiveMethod.IsVirtual))
                    return;

                else if (!Manager.ThreeStateCheck(settings.IsFinal, effectiveMethod.IsVirtual && effectiveMethod.IsFinal))
                    return;

                else if (!Manager.ThreeStateCheck(settings.IsAbstract, effectiveMethod.IsAbstract))
                    return;

                else if (!Manager.ThreeStateCheck(
                                settings.IsOverride,
                                effectiveMethod.IsVirtual
                                    && ((MethodAttributes)effectiveMethod.Attributes & MethodAttributes.VtableLayoutMask) != MethodAttributes.NewSlot))
                {
                    return;
                }
            }

            matches = true;
            meaningful = true;
        }

    }


    public class CheckForStatic : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            if (Manager.Settings.IsStatic == null)
                return;


            matches = false;


            if (Manager.Settings.IsStatic == true && t.IsEnum)
            {
                return;
            }
            else
            {
                if (Manager.ThreeStateCheck(Manager.Settings.IsStatic, effectiveMethod.IsStatic))
                {
                    matches = true;
                    meaningful = true;
                }
            }

        }

    }



    public class CheckForInWindows : CheckForMatch
    {

        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;

            if (settings.TypeInWindows == null || t.IsWindows)
                return;

            bool inWindows = false;
            var tMatch = Manager.GetMatchingType(Manager.WinmdTypeSet.Types, t);
            if (tMatch != null)
            {
                inWindows = true;
            }
            else
            {
                inWindows = false;
            }


            if (Manager.ThreeStateCheck(
                Manager.Settings.TypeInWindows, inWindows))
            {
                matches = true;
                meaningful = true;
            }
            else
            {
                matches = false;
                abortType = true;
            }
        }


        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            var settings = Manager.Settings;

            if (settings.MemberInWindows == null || t.IsWindows)
                return;

            bool inWindows = false;
            matches = false;

            var tMatch = Manager.GetMatchingType(Manager.WinmdTypeSet.Types, t); // bugbug (re-use the type from type check)
            if (tMatch != null)
            {
                foreach (var aMember in tMatch.Members)
                {
                    if (propertyInfo != null && propertyInfo.Name == aMember.Name
                        ||
                        eventInfo != null && eventInfo.Name == aMember.Name
                        ||
                        fieldInfo != null && fieldInfo.Name == aMember.Name
                        ||
                        constructorInfo != null && constructorInfo.Name == aMember.Name
                        ||
                        methodInfo != null && methodInfo.Name == aMember.Name)
                    {
                        inWindows = true;
                        break;
                    }
                }
            }

            if (Manager.ThreeStateCheck(settings.MemberInWindows, inWindows))
            {
                matches = true;
                meaningful = true;
            }
        }

    }

    public class CheckForInCustom : CheckForMatch
    {

        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;

            if (settings.TypeInCustom == null || t.IsCustom)
                return;

            bool inCustom = false;
            var tMatch = Manager.GetMatchingType(Manager.CustomTypeSet.Types, t);
            if (tMatch != null)
            {
                inCustom = true;
            }
            else
            {
                inCustom = false;
            }


            if (Manager.ThreeStateCheck(
                Manager.Settings.TypeInCustom, inCustom))
            {
                matches = true;
                meaningful = true;
            }
            else
            {
                matches = false;
                abortType = true;
            }
        }


        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            var settings = Manager.Settings;

            if (settings.MemberInCustom == null || t.IsCustom)
                return;

            bool inCustom = false;
            matches = false;

            var tMatch = Manager.GetMatchingType(Manager.CustomTypeSet.Types, t); // bugbug (re-use the type from type check)
            if (tMatch != null)
            {
                foreach (var aMember in tMatch.Members)
                {
                    if (propertyInfo != null && propertyInfo.Name == aMember.Name
                        ||
                        eventInfo != null && eventInfo.Name == aMember.Name
                        ||
                        fieldInfo != null && fieldInfo.Name == aMember.Name
                        ||
                        constructorInfo != null && constructorInfo.Name == aMember.Name
                        ||
                        methodInfo != null && methodInfo.Name == aMember.Name)
                    {
                        inCustom = true;
                        break;
                    }
                }
            }


            if (Manager.ThreeStateCheck(settings.MemberInCustom, inCustom))
            {
                matches = true;
                meaningful = true;
            }
        }

    }


    public class CheckForOutParameters : CheckForMatch
    {

        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            var settings = Manager.Settings;

            // bugbug: No check for HasRefParameter
            if (settings.HasOutParameter == null && settings.HasRefParameter == null && settings.HasMultipleOutParameters == null)
                return;

            matches = false;

            var outCount = 0;
            if (methodInfo != null)
            {
                IList<ParameterViewModel> parameters = methodInfo.Parameters;
                if (parameters != null && parameters.Count > 0)
                {
                    foreach (var parameter in parameters)
                    {
                        if (parameter.IsOut)
                        {
                            outCount++;
                        }
                    }
                }
            }

            if (Manager.ThreeStateCheck(settings.HasOutParameter, outCount != 0)
                && Manager.ThreeStateCheck(settings.HasMultipleOutParameters, outCount > 1))
            {
                matches = true;
                //meaningful = true;
            }

        }
    }


    public class CheckForAddedSetters : CheckForMatch
    {

        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            var settings = Manager.Settings;

            if (settings.HasAddedSetter == null )
                return;

            matches = false;

            if (Manager.ThreeStateCheck(
                            settings.HasAddedSetter, 
                            propertyInfo != null && !string.IsNullOrWhiteSpace(propertyInfo.SetterVersionFriendlyNameOverride)))
            {
                matches = true;
                //meaningful = true;
            }

        }
    }


    public class CheckForInWpf : CheckForMatch
    {

        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;

            if (settings.TypeInWpf == null || t.IsWpf)
                return;

            bool inWpf = false;
            var tMatch = Manager.GetMatchingType(Manager.WpfTypeSet.Types, t);
            if (tMatch != null)
            {
                inWpf = true;
            }
            else
            {
                inWpf = false;
            }


            if (Manager.ThreeStateCheck(
                Manager.Settings.TypeInWpf, inWpf))
            {
                matches = true;
                meaningful = true;
            }
            else
            {
                matches = false;
                abortType = true;
            }
        }


        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            var settings = Manager.Settings;

            if (settings.MemberInWpf == null || t.IsWpf)
                return;

            bool inWpf = false;
            matches = false;

            var tMatch = Manager.GetMatchingType(Manager.WpfTypeSet.Types, t); // bugbug (re-use the type from type check)
            if (tMatch != null)
            {
                foreach (var aMember in tMatch.Members)
                {
                    if (propertyInfo != null && propertyInfo.Name == aMember.Name
                        ||
                        eventInfo != null && eventInfo.Name == aMember.Name
                        ||
                        fieldInfo != null && fieldInfo.Name == aMember.Name
                        ||
                        constructorInfo != null && constructorInfo.Name == aMember.Name
                        ||
                        methodInfo != null && methodInfo.Name == aMember.Name)
                    {
                        inWpf = true;
                        break;
                    }
                }
            }

            if (Manager.ThreeStateCheck(settings.MemberInWpf, inWpf))
            {
                matches = true;
                meaningful = true;
            }
        }

    }


        public class CheckForObjectArgs : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            var settings = Manager.Settings;

            if (settings.UntypedArgs == null)
                return;

            bool isUntyped = false;
            matches = false;

            if (eventInfo != null)
            {
                isUntyped = eventInfo.IsUntyped;
                //var invokeMethod = eventInfo.EventHandlerType.GetMethod("Invoke");
                //var parameters = invokeMethod.GetParameters();

                //if (parameters.Length < 2)
                //    isUntyped = true;
                //else if (parameters[1].ParameterType.FullName == "System.Object")
                //    isUntyped = true;
            }

            if (Manager.ThreeStateCheck(settings.UntypedArgs, isUntyped))
            {
                matches = true;
                meaningful = true;
            }
        }

    }




    public class CheckForInterfaceParameters : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            if (Manager.Settings.HasInterfaceParameter == null)
                return;

            matches = false;
            IEnumerable<ParameterViewModel> parameters = null;
            TypeViewModel returnType = null;
            bool has = false;

            if (methodInfo != null)
            {
                parameters = methodInfo.Parameters;// GetParameters();
                returnType = methodInfo.ReturnType;
            }
            else if (constructorInfo != null)
            {
                parameters = constructorInfo.Parameters;//.GetParameters();
            }

            if (returnType != null && IsSignificantInterface(returnType))
                has = true;

            if (!has && parameters != null)
            {
                foreach (var parameter in parameters)
                {
                    if (IsSignificantInterface(parameter.ParameterType))
                    {
                        has = true;
                        break;
                    }
                }
            }


            if (Manager.MatchesSetting(Manager.Settings.HasInterfaceParameter, has))
                matches = true;

        }

        bool IsSignificantInterface(TypeViewModel type)
        {
            return
                type.IsInterface
                && !type.Name.StartsWith("IList`")
                && !type.Name.StartsWith("IEnumerable`")
                && !type.Name.StartsWith("IAsyncAction")
                && !type.Name.StartsWith("IReadOnlyList")
                && !type.Name.StartsWith("IAsyncOperation")
                && !type.Name.StartsWith("IRandomAccessStream")
                && !type.Name.StartsWith("IInputStream")
                && !type.Name.StartsWith("IOutputStream")
                && !type.Name.StartsWith("IBackground")
                && !type.Name.StartsWith("IBuffer");
        }

    }



    public class CheckForReturnsHostType : CheckForMatch
    {
        public override void MemberCheck(
            TypeViewModel t,
            SearchExpression filter,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out bool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, filter, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            if (Manager.Settings.ReturnsHostType == null)
                return;

            matches = false;

            bool sameType = false;

            TypeViewModel returnType = null;
            TypeViewModel hostType = null;

            if (methodInfo != null)
            {
                returnType = methodInfo.ReturnType;
                hostType = methodInfo.DeclaringType;

            }
            else if (propertyInfo != null)
            {
                returnType = propertyInfo.PropertyType;
                hostType = propertyInfo.DeclaringType;
            }

            if (returnType != null && returnType.IsGenericType)
            {
                // Handle IAsync, IList, IDictionary cases
                var args = returnType.GetGenericArguments();
                foreach (var arg in args)
                {
                    if (hostType == arg)
                    {
                        sameType = true;
                        break;
                    }
                }
            }
            else if (returnType != null)
            {
                sameType = returnType == hostType;
            }

            //if (methodInfo != null && methodInfo.ReturnType == methodInfo.DeclaringType )
            //{
            //    sameType = true;
            //}
            //else if (propertyInfo != null && propertyInfo.PropertyType == propertyInfo.DeclaringType )
            //{
            //    sameType = true;
            //}

            if (Manager.MatchesSetting(Manager.Settings.ReturnsHostType, sameType))
                matches = true;

        }


    }


}


