namespace Tempo
{
    public class CheckForNamespace : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

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


    public class CheckForFilterOnType : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            if (Manager.Settings.ShowTypes == false)
            {
                // We're not supposed to check types, so we can skip this check. The exception is if we're doing a
                // "type:member" search, like "Button:Click". In that case, still check the type name.

                if (!filter.IsTwoPart)
                {
                    return;
                }
            }


            if (Manager.TypeMatchesFilters(
                    t, filter.TypeRegex,
                    Manager.Settings.FilterOnBaseType,
                    Manager.Settings,
                    ref abort,
                    ref meaningful))
            {
                matches = true;
            }
            else
                matches = false;

        }
    }

    // Compare APIs with a baseline set of APIs, for creating a delta list
    public class CheckForNotInBaseline : CheckForMatch
    {

        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);

            var settings = Manager.Settings;

            if (settings.CompareToBaseline == null)
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


            if (Manager.ThreeStateCheck(
                Manager.Settings.CompareToBaseline, !inBaseline))
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

            if (settings.CompareToBaseline == null)
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
                        inBaseline = true;
                        break;
                    }
                }
            }

            if (Manager.ThreeStateCheck(settings.CompareToBaseline, !inBaseline))
            {
                matches = true;
                meaningful = true;
            }
        }

    }



}
