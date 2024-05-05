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
            out bool matches,
            out DebuggaBool meaningful,
            out bool abortType,
            ref bool abort)
        {
            meaningful = abortType = false;
            matches = true;
            return;
        }

        public virtual void MemberCheck(
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
            //matches = meaningful = true;
            matches = true;
            meaningful = false;

            return;
        }

        static public MemberOrTypeViewModelBase GetMemberViewModel(
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
        public override void TypeCheck(TypeViewModel t, out bool matches, out DebuggaBool meaningful, out bool abortType, ref bool abort)
        {
            base.TypeCheck(t, out matches, out meaningful, out abortType, ref abort);


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
