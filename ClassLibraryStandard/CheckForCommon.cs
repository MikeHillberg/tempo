﻿using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Tempo
{
    public class CheckForNamespace : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, out bool matches, out DebuggaBool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, out matches, out meaningful, out abortType, ref abort);

            if (Manager.Settings.Namespace == null)
                return;

            var selectedNamespace = Manager.Settings.Namespace;
            if (selectedNamespace != null)
            {
                var selectedNamespacePlusDot = selectedNamespace + ".";
                var exact = selectedNamespace == t.Namespace;
                var under = t.Namespace.StartsWith(selectedNamespacePlusDot);

                var settings = Manager.Settings;

                if (settings.NamespaceInclusive && !(exact || under)
                    ||
                    settings.NamespaceExclusive && !exact
                    ||
                    settings.NamespaceExcluded && (exact || under))
                {
                    matches = false;
                    abortType = true;
                }
            }
        }





    }



    // Compare APIs with a baseline set of APIs, for creating a delta list
    public class CheckForNotInBaseline : CheckForMatch
    {

        public override void TypeCheck(TypeViewModel t, out bool matches, out DebuggaBool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;

            if (!settings.CompareToBaseline)
            {
                return;
            }

            bool inBaseline;
            var tMatch = Manager.GetMatchingType(Manager.BaselineTypeSet.Types, t);
            if (tMatch != null)
            {
                inBaseline = true;
            }
            else
            {
                inBaseline = false;
            }


            if (!inBaseline)
            {
                matches = true;
                meaningful = true;
            }
            else
            {
                matches = false;
            }
        }


        public override void MemberCheck(
            TypeViewModel t,
            PropertyViewModel propertyInfo,
            EventViewModel eventInfo,
            FieldViewModel fieldInfo,
            ConstructorViewModel constructorInfo,
            MethodViewModel methodInfo,
            DuckMethod effectiveMethod,
            out bool matches,
            out DebuggaBool meaningful,
            ref bool abort)
        {
            base.MemberCheck(t, propertyInfo, eventInfo, fieldInfo, constructorInfo, methodInfo, effectiveMethod, out matches, out meaningful, ref abort);

            var settings = Manager.Settings;

            if (!settings.CompareToBaseline)
            {
                return;
            }

            bool inBaseline = false;
            matches = false;

            var tMatch = Manager.GetMatchingType(Manager.BaselineTypeSet.Types, t); // bugbug (re-use the type from type check)
            if (tMatch != null)
            {
                foreach (var aMember in tMatch.Members)
                {

                    if (propertyInfo != null && propertyInfo.Name == aMember.Name && aMember.MemberKind == MemberKind.Property
                        ||
                        eventInfo != null && eventInfo.Name == aMember.Name && aMember.MemberKind == MemberKind.Event
                        ||
                        fieldInfo != null && fieldInfo.Name == aMember.Name && aMember.MemberKind == MemberKind.Field)
                    {
                        inBaseline = true;
                    }

                    else if (constructorInfo != null && constructorInfo.Name == aMember.Name && aMember.MemberKind == MemberKind.Constructor
                            ||
                            methodInfo != null && methodInfo.Name == aMember.Name && aMember.MemberKind == MemberKind.Method)
                    {
                        IList<ParameterViewModel> aParameters = null;
                        IList<ParameterViewModel> parameters = null;

                        if (constructorInfo != null)
                        {
                            parameters = constructorInfo.Parameters;
                            aParameters = (aMember as ConstructorViewModel).Parameters;
                        }
                        else
                        {
                            parameters = methodInfo.Parameters;
                            aParameters = (aMember as MethodViewModel).Parameters;
                        }

                        if (parameters.Count != aParameters.Count)
                        {
                            continue;
                        }

                        int i;
                        for (i = 0; i < parameters.Count; i++)
                        {
                            if (parameters[i].ParameterType != aParameters[i].ParameterType)
                            {
                                break;
                            }
                        }
                        if (i == parameters.Count)
                        {
                            inBaseline = true;
                        }

                    }

                }
            }

            if (!inBaseline)
            {
                matches = true;
                meaningful = true;
            }
        }

    }



}
