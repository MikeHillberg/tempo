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
            MemberViewModel memberVM,
            SearchExpression filter)
        {
            if (filter.SearchCondition == null)
                return true;

            return Check(memberVM, filter);

        }

        static public bool TypeCheck(TypeViewModel t, SearchExpression filter)
        {
            if (filter.SearchCondition == null)
                return true;

            return Check(t, filter);

        }

        static bool Check(MemberViewModel memberVM, SearchExpression filter)
        {

            if (filter.SearchCondition.Evaluate(
                memberVM,
                Manager.Settings.CaseSensitive,
                filter,
                (string key) =>
                {
                    if (!memberVM.TryGetVMProperty(key, out var value))
                        return SearchCondition.Unset;

                    return value;
                }))
            {
                return true;
            }

            return false;
        }
    }
}
