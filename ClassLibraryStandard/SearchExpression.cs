using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tempo;

namespace Tempo
{
    public class SearchExpression : INotifyPropertyChanged
    {
        string _rawValue;
        public string RawValue
        {
            get { return _rawValue; }
            set
            {
                _rawValue = value;

                _ensured = false;
                _typeRegex = _memberRegex = null;
                SearchCondition = null;

                RaisePropertyChanged();
                Changed?.Invoke(null, null);
            }
        }

        public event EventHandler Changed;

        public bool IsTwoPart
        {
            get { return TypeRegex != MemberRegex; }
        }

        public bool HasNoSearchString
        {
            get
            {
                EnsureRegex();
                return _typeRegex == null && _memberRegex == null;
            }
        }

        // http://msdn.microsoft.com/en-us/library/az24scfc.aspx
        Regex _typeRegex;
        Regex _memberRegex;

        public Regex TypeRegex
        {
            get
            {
                EnsureRegex();
                return _typeRegex;
            }
        }

        public Regex MemberRegex
        {
            get
            {
                EnsureRegex();
                return _memberRegex;
            }
        }

        bool _ensured = false;

        public void EnsureRegex()
        {
            if (_ensured)
            {
                Debug.Assert(_typeRegex == null || _memberRegex != null);
                return;
            }

            SplitFilter();

            _ensured = true;
        }



        public void SplitFilter()
        {
            var searchString = _rawValue;
            if (searchString != null)
                searchString = searchString.Trim();

            if (!string.IsNullOrEmpty(searchString) && _rawValue.Contains("-where"))
            {
                var split = searchString.MySplit("-where");
                if (split.Length > 1)
                {
                    SearchCondition = null;
                    searchString = split[0].Trim();

                    SearchCondition = TryParseExpression(split[1]);
                }
            }

            if (string.IsNullOrEmpty(searchString) || !_rawValue.Contains(":"))
            {
                _typeRegex = string.IsNullOrEmpty(searchString) ? null : SplitFilterForGenerics(searchString);
                _memberRegex = _typeRegex;
            }
            else
            {
                var split = searchString.Split(':');
                try
                {
                    _memberRegex = new Regex(
                        split[1],
                        Manager.Settings.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                }
                catch(ArgumentException)
                {
                    // User hasn't typed in a valid regex
                    _memberRegex = new Regex("");
                }
                _typeRegex = SplitFilterForGenerics(split[0]);
            }

        }

        private SearchCondition TryParseExpression(string expressionString)
        {
            var parts = expressionString.Trim().Split(' ');

            Stack<CompoundSearchCondition> stack = null;

            int i = 0;
            while(i < parts.Length)
            {
                if (parts.Length - i < 3)
                    break;

                var part = parts[i].Trim();
                if (part == "")
                    continue;

                var comparisson = TryParseComparisson(parts[i++].Trim(), parts[i++].Trim(), parts[i++].Trim());
                if (i == parts.Length)
                {
                    if (stack == null)
                        return comparisson;

                    var pop = stack.Pop() as CompoundSearchCondition;
                    pop.Operand2 = comparisson;

                    while(stack.Count != 0)
                    {
                        var pop2 = stack.Pop();
                        pop2.Operand2 = pop;
                        pop = pop2;
                    }

                    return pop;
                }

                var nextOp = ParseCompoundOperator(parts[i++]);
                if (nextOp == null)
                {
                    if (stack == null)
                        return comparisson;
                    else return stack.First();
                }

                if (stack == null)
                {
                    var booleanCondition = new CompoundSearchCondition();
                    booleanCondition.Operator = (CompoundSearchOperator) nextOp;
                    booleanCondition.Operand1 = comparisson;

                    stack = new Stack<CompoundSearchCondition>();
                    stack.Push(booleanCondition);
                }
                else
                {
                    var peek = stack.Peek();
                    var booleanCondition = new CompoundSearchCondition();

                    if (nextOp > peek.Operator )
                    {
                        booleanCondition.Operator = (CompoundSearchOperator)nextOp;
                        booleanCondition.Operand1 = comparisson;
                        stack.Push(booleanCondition);
                    }

                    else if (nextOp <= peek.Operator)
                    {
                        var pop = stack.Pop();
                        pop.Operand2 = comparisson;

                        while (stack.Count != 0)
                        {
                            // Sample that gets to this path:
                            // -where Name -contains Button -or Name -contains App -and Name -ncontains Appointment -or Name -contains CheckBox

                            var pop2 = stack.Pop();
                            pop2.Operand2 = pop;
                            pop = pop2;

                            if (pop.Operator > nextOp)
                                break;
                        }

                        booleanCondition.Operator = (CompoundSearchOperator)nextOp;
                        booleanCondition.Operand1 = pop;
                        stack.Push(booleanCondition);
                    }
                }
            }

            return stack?.First();
        }

        CompoundSearchOperator? ParseCompoundOperator(string opString)
        {
            var splitter = "-or";
            if (opString.Contains(splitter))
            {
                return CompoundSearchOperator.Or;
            }
            else
            {
                splitter = "-and";
                if (opString.Contains(splitter))
                    return CompoundSearchOperator.And;
                else
                    return null;
            }
        }

        private SearchComparisson TryParseComparisson(string key, string opString, string value)
        {
            SearchConditionOperator conditionOperator = SearchConditionOperator.Contains;

            switch (opString)
            {
                case "-eq":
                    conditionOperator = SearchConditionOperator.Equals;
                    break;

                case "-neq":
                    conditionOperator = SearchConditionOperator.NotEquals;
                    break;

                case "-contains":
                    conditionOperator = SearchConditionOperator.Contains;
                    break;

                case "-ncontains":
                    conditionOperator = SearchConditionOperator.NotContains;
                    break;

                case "-finds":
                    conditionOperator = SearchConditionOperator.Finds;
                    break;

                default:
                    return null;
            }

            var condition = new SearchComparisson();
            condition.Key = key;
            condition.Value = value;
            condition.Operator = conditionOperator;
            return condition;
        }

        private SearchCondition TryParseExpression_old(string expressionString)
        {
            expressionString = expressionString.Trim();
            var compoundOperator = CompoundSearchOperator.And;

            var splitter = "-or";
            if (expressionString.Contains(splitter))
            {
                compoundOperator = CompoundSearchOperator.Or;
            }
            else
            {
                splitter = "-and";
                if (expressionString.Contains(splitter))
                    compoundOperator = CompoundSearchOperator.And;
                else
                    splitter = null;
            }

            if (splitter != null)
            {
                var split = expressionString.MySplitOnFirst(splitter);
                var part1 = TryParseExpression(split[0]);
                var part2 = TryParseExpression(split[1]);

                if (part1 == null || part2 == null)
                    return part1 != null ? part1 : part2;

                return new CompoundSearchCondition()
                {
                    Operand1 = part1,
                    Operand2 = part2,
                    Operator = compoundOperator
                };
            }

            var expressionParts = expressionString.Split(' ');
            if (expressionParts.Length != 3)
            {
                return null;
            }

            var key = expressionParts[0].Trim();
            var opString = expressionParts[1].Trim();
            var value = expressionParts[2].Trim();
            var conditionOperator = SearchConditionOperator.Contains;

            switch (opString)
            {
                case "-eq":
                    conditionOperator = SearchConditionOperator.Equals;
                    break;

                case "-neq":
                    conditionOperator = SearchConditionOperator.NotEquals;
                    break;

                case "-contains":
                    conditionOperator = SearchConditionOperator.Contains;
                    break;

                case "-ncontains":
                    conditionOperator = SearchConditionOperator.NotContains;
                    break;

                case "-finds":
                    conditionOperator = SearchConditionOperator.Finds;
                    break;

                default:
                    return null;
            }

            var condition = new SearchComparisson();
            condition.Key = key;
            condition.Value = value;
            condition.Operator = conditionOperator;
            return condition;
        }

        public Regex SplitFilterForGenerics(string value)
        {
            if (string.IsNullOrEmpty(value))
                return new Regex("");

            var split = value.Split('<');
            if (split.Length == 1)
                return CreateRegex(value);

            var sb = new StringBuilder();

            value = SplitFilterHelper(value, split, sb, @"\<");

            split = value.Split('>');
            sb.Clear();
            value = SplitFilterHelper(value, split, sb, @"\>");


            return CreateRegex(value);
        }

        private Regex CreateRegex(string value)
        {
            try
            {
                return new Regex(value, Manager.Settings.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            }
            catch (System.ArgumentException)
            {
                // Ignore invalid regex expression (might still be typing)
                return null;
            }
        }

        private static string SplitFilterHelper(string newQuery, string[] split, StringBuilder sb, string bracket)
        {
            if (split.Length > 1)
            {
                for (int i = 0; i < split.Length; i++)
                {
                    var soFar = sb.ToString();
                    if (!string.IsNullOrEmpty(soFar))
                    {
                        if (!soFar.EndsWith(".*"))
                        {
                            sb.Append(".*");
                        }
                    }

                    sb.Append(split[i]);

                    if (i != split.Length - 1)
                    {
                        sb.Append(".*");
                        sb.Append(bracket);
                    }
                }
                newQuery = sb.ToString();
            }
            return newQuery;
        }

        public SearchCondition SearchCondition { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }


    abstract public class SearchCondition
    {
        public abstract bool Evaluate(MemberViewModel memberVM, bool caseSensitive,
            SearchExpression filter,
            Func<string, object> getter);

        public static object Unset = typeof(SearchCondition);

        public static string ToWhereString(object value)
        {
            if (value == null)
                return "null";

            else if (value is string)
                return (value as string);

            else if (value is CustomAttributeViewModel)
                return (value as CustomAttributeViewModel).Name;

            else if (value is BaseViewModel)
                return (value as BaseViewModel).ToWhereString();

            else if (value is IEnumerable)
            {
                var list = value as IEnumerable;
                var sb = new StringBuilder();

                sb.Append("{");
                foreach (var item in list)
                {
                    sb.Append(ToWhereString(item));
                    sb.Append(" \n");
                }
                sb.Append("}");

                return sb.ToString();
            }

            else
            {
                return value.ToString();
            }
        }
    }

    public class SearchComparisson : SearchCondition
    {
        public string Key;
        public string Value;
        public SearchConditionOperator Operator;

        public override bool Evaluate(MemberViewModel memberVM, bool caseSensitive, 
            SearchExpression filter,
            Func<string, object> getter)
        {
            //var propInfo = memberVM.GetType().GetProperty(Key);
            //if (propInfo == null)
            //    return false;
            //var value = propInfo.GetValue(memberVM);

            var value = getter(Key);
            if (value != SearchCondition.Unset)
            {
                if (value == null)
                    value = string.Empty;

                var valueString = ToWhereString(value);
                var conditionString = Value;

                if (!caseSensitive)
                {
                    valueString = valueString.ToUpper();
                    conditionString = conditionString.ToUpper();
                }

                if (Operator == SearchConditionOperator.Contains
                        && valueString.Contains(conditionString)
                    ||
                    Operator == SearchConditionOperator.NotContains
                        && !valueString.Contains(conditionString)
                    ||
                    Operator == SearchConditionOperator.Equals
                        && valueString == conditionString
                    ||
                    Operator == SearchConditionOperator.NotEquals
                        && valueString != conditionString
                    )
                {
                    return true;
                }

                else if (Operator == SearchConditionOperator.Finds)
                { 
                    if (value is TypeViewModel)
                    {
                        var type = value as TypeViewModel;

                        var regex = new Regex(conditionString, !caseSensitive ? RegexOptions.IgnoreCase : RegexOptions.None);

                        var abort = false;
                        var meaningfulMatch = false;

                        if (Manager.MatchesFilterString(regex, type, true, true, null, ref abort, ref meaningfulMatch))
                            return true;

                        foreach(var member in type.Members)
                        {
                            var filtersMatch = true;
                            Manager.CheckMemberName(member, regex, ref abort, out filtersMatch, out meaningfulMatch);
                            if (meaningfulMatch)
                                return true;
                        }
                    }
                }
            }

            return false;
        }
    }


    public enum CompoundSearchOperator
    {
        // Order matters
        Or,
        And
    }

    public class CompoundSearchCondition : SearchCondition
    {
        public SearchCondition Operand1 { get; set; }
        public SearchCondition Operand2 { get; set; }

        public CompoundSearchOperator Operator { get; set; }

        public override bool Evaluate(MemberViewModel memberVM, bool caseSensitive,
            SearchExpression filter,
            Func<string, object> getter)
        {
            var operand1Result = Operand1.Evaluate(memberVM, caseSensitive, filter, getter);
            bool operand2Result = false;

            if (Operand2 != null)
                operand2Result = Operand2.Evaluate(memberVM, caseSensitive, filter, getter);
            else
            {
                return operand1Result;
            }

            if (Operator == CompoundSearchOperator.And)
                return operand1Result && operand2Result;
            else
                return operand1Result || operand2Result;
        }

    }

    public enum SearchConditionOperator
    {
        Equals = 0,
        NotEquals,
        Contains,
        NotContains,
        Finds
    }
}
