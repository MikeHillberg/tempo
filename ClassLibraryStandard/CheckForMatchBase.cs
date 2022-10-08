using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommonLibrary;
using System.Text.RegularExpressions;

namespace Tempo
{

    public class CheckForMatch
    {

        public virtual void TypeCheck(
            TypeViewModel t,
            SearchExpression filter,
            out bool matches,
            out bool meaningful,
            out bool abortType,
            ref bool abort)
        {
            meaningful = abortType = false;
            matches = true;
            return;
        }

        public virtual void MemberCheck(
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
            matches = meaningful = true;
            return;
        }

        static public MemberViewModel GetMemberViewModel(
            PropertyViewModel propertyVM,
            MethodViewModel methodVM,
            EventViewModel eventVM,
            ConstructorViewModel constructorVM,
            FieldViewModel fieldVM)
        {
            if (propertyVM != null)
                return propertyVM;
            else if (methodVM != null)
                return methodVM;
            else if (eventVM != null)
                return eventVM;
            else if (constructorVM != null)
                return constructorVM;
            else if (fieldVM != null)
                return fieldVM;
            else
                throw new Exception("GetMemberViewModel");
        }
    }


    public class CheckForTypeRestrictions : CheckForMatch
    {
        public override void TypeCheck(TypeViewModel t, SearchExpression filter, out bool matches, out bool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, filter, out matches, out meaningful, out abortType, ref abort);


            if (Manager.MatchesTypeModifiers(t, ref meaningful)
                && Manager.MatchesTypeKind(t, ref meaningful)
                && Manager.MatchesDependencyObject(t, ref meaningful))
            {
                matches = true;
                return;
            }
            else
                abortType = true;

        }
    }




}
