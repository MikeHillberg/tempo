using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public static class EvaluateWhere
    {
        static public bool MemberCheck(
            TypeViewModel t,
            MemberOrTypeViewModelBase memberVM,
            SearchExpression filter)
        {
            if (filter.WhereCondition == null)
                return true;

            return Check(memberVM, filter);

        }

        static public bool TypeCheck(TypeViewModel t, SearchExpression filter)
        {
            if (filter.WhereCondition == null)
                return true;

            return Check(t, filter);

        }

        static bool Check(MemberOrTypeViewModelBase memberVM, SearchExpression filter)
        {

            if (filter.WhereCondition.Evaluate(
                memberVM,
                Manager.Settings.CaseSensitive,
                filter,
                (string key) =>
                {
                    if (!memberVM.TryGetVMProperty(key, out var value))
                        return WhereCondition.Unset;

                    return value;
                }))
            {
                return true;
            }

            return false;
        }
    }
}
