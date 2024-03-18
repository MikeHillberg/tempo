using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

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
                WhereCondition = null;
                _aqsExpression = null;


                RaisePropertyChanged();
                Changed?.Invoke(null, null);
            }
        }

        public event EventHandler Changed;

        public bool IsTwoPart
        {
            get { return TypeRegex != MemberRegex; }
        }

        /// <summary>
        /// Indicates that the search expression has a syntax error
        /// </summary>
        static public event EventHandler<string> SearchExpressionError;

        /// <summary>
        /// Raise `SearchExpressionError` event
        /// </summary>
        static public void RaiseSearchExpressionError(string message)
        {
            SearchExpressionError?.Invoke(null, message);
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
        AqsExpression _aqsExpression = null;

        /// <summary>
        /// Evaluate the AQS expression, if any.
        /// True if it matches, false if it doesn't, null if was uninteresting (like no AQS present)
        /// </summary>
        /// <param name="propertyEvaluator">Called to evaluate a key</param>
        /// <param name="customEvaluator">Called to evaluate custom operand</param>
        /// <returns>Boolean evaluation of the AQS</returns>
        public bool? EvaluateAqsExpression(Func<string, string> propertyEvaluator, Func<CustomOperand,bool> customEvaluator)
        {
            if (_aqsExpression == null)
            {
                return null;
            }

            return _aqsExpression.Evaluate(propertyEvaluator, (o) => customEvaluator(o as CustomOperand));
        }

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

            ParseRawValue();

            _ensured = true;
        }


        /// <summary>
        /// Parse `_rawValue`
        /// </summary>
        public void ParseRawValue()
        {
            _typeRegex = _memberRegex = null;
            SearchSlowly = false;

            if (string.IsNullOrEmpty(_rawValue))
            {
                return;
            }

            var searchString = _rawValue;
            if (searchString != null)
            {
                searchString = searchString.Trim();
            }

            // Testing aid
            if (searchString.Contains("-slowly"))
            {
                SearchSlowly = true;
                searchString = searchString.Replace("-slowly", "");
            }

            // A "::" means Type::Member syntax, a single ":" means AQS syntax
            // To avoid confusion, convert "::" to "%"
            searchString = searchString.Replace("::", "%");

            //// Check for -where clause
            //if (!string.IsNullOrEmpty(searchString) && _rawValue.Contains("-where"))
            //{
            //    var split = searchString.MySplit("-where");
            //    if (split.Length > 1)
            //    {
            //        WhereCondition = null;
            //        searchString = split[0].Trim();

            //        WhereCondition = TryParseExpression(split[1]);
            //    }
            //}



            // Parse the search string
            _aqsExpression = TryParseAqs(searchString, CustomOperandCallback);
            if (_aqsExpression == null) 
            {
                return;
            }


            // See if there's any custom operands in the parsed string.
            // For example, for search string "button IsClass:true",
            // "button" is the custom operand. This will parse to
            // "CUSTOM:button AND IsClass:True", and "button"
            // will be the customOperand.
            // We want to find this so that we can hightlight "button" in the search results.

            var searchSubstring = "";
            var customOperands = _aqsExpression.CustomOperands;
            if (customOperands != null && customOperands.Length > 0)
            {
                // Bugbug: handle the multiple case.
                // E.g. "button OR toggle"
                searchSubstring = customOperands[0];
            }

            if (!searchSubstring.Contains("%"))
            {
                // Typical case, no "Type::Member" syntax
                _typeRegex = string.IsNullOrEmpty(searchSubstring) ? null : SplitFilterForGenerics(searchSubstring);
                _memberRegex = _typeRegex;
            }
            else
            {
                // "Type::Member" syntax
                var split = searchSubstring.Split('%');
                _typeRegex = SplitFilterForGenerics(split[0]);
                _memberRegex = CreateRegex(split[1]);
            }

        }

        // This is called by the AQS expression parser when it sees a custom operand.
        // Return it as a regex, or two regex in the case of type::member syntax
        object CustomOperandCallback(string operand)
        {
            if (!operand.Contains("%"))
            {
                var regex = CreateRegex(operand);
                return new CustomOperand(regex, regex);
            }

            var parts = operand.Split('%');
            return new CustomOperand(CreateRegex(parts[0]), CreateRegex(parts[1]));
        }

        public class CustomOperand
        {
            public CustomOperand(Regex typeRegex, Regex memberRegex)
            {
                this.TypeRegex = typeRegex;
                this.MemberRegex = memberRegex;
            }

            public Regex TypeRegex { get; }
            public Regex MemberRegex { get; }
        }


        AqsExpression TryParseAqs(string searchString, Func<string,object> customOperandCallback)
        {
            // Make sure that all AQS operators are space-separated
            searchString = searchString.Replace(":", " : ");
            searchString = searchString.Replace("(", " ( ");
            searchString = searchString.Replace(")", " ) ");
            searchString = searchString.Replace("!", " ! ");
            searchString = searchString.Replace("||", " || ");
            searchString = searchString.Replace("&&", " && ");

            return AqsExpression.TryParse(
                searchString,
                Manager.Settings.CaseSensitive,
                Manager.Settings.IsWildcardSyntax,
                AqsKeyValidator,
                customOperandCallback);
        }


        IList<string> _validAqsKeys = null;

        /// <summary>
        /// Validate AQS keys, like "Name" or "Namespace"
        /// </summary>
        bool AqsKeyValidator(string key)
        {
            if (_validAqsKeys == null)
            {
                // Bugbug: share these with TryGetVMProperty

                var propertyInfos = typeof(TypeViewModel).GetProperties()
                    .Union(typeof(PropertyViewModel).GetProperties())
                    .Union(typeof(MethodViewModel).GetProperties())
                    .Union(typeof(EventViewModel).GetProperties())
                    .Union(typeof(FieldViewModel).GetProperties())
                    .Union(typeof(ConstructorViewModel).GetProperties());

                _validAqsKeys = (from i in propertyInfos select i.Name.ToUpper()).ToList();
            }

            key = MemberViewModelBase.NormalizePropertyNameForQueries(key);
            return _validAqsKeys.Contains(key.ToUpper());
        }


        private WhereCondition TryParseExpression(string expressionString)
        {
            var parts = expressionString.Trim().Split(' ');

            Stack<CompoundSearchCondition> stack = null;

            int i = 0;
            while (i < parts.Length)
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

                    while (stack.Count != 0)
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
                    booleanCondition.Operator = (CompoundSearchOperator)nextOp;
                    booleanCondition.Operand1 = comparisson;

                    stack = new Stack<CompoundSearchCondition>();
                    stack.Push(booleanCondition);
                }
                else
                {
                    var peek = stack.Peek();
                    var booleanCondition = new CompoundSearchCondition();

                    if (nextOp > peek.Operator)
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

        private WhereCondition TryParseExpression_old(string expressionString)
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

        /// <summary>
        /// Create a regex, or return null if invalid
        /// </summary>
        static internal Regex CreateRegex(string value)
        {
            return AqsExpression.CreateRegex(
                value,
                Manager.Settings.CaseSensitive,
                Manager.Settings.IsWildcardSyntax);
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

        public bool HasAqsExpression => _aqsExpression != null;

        public WhereCondition WhereCondition { get; private set; }

        // This is a testing aid. When set, do a sleep during search
        public bool SearchSlowly = false;

        public event PropertyChangedEventHandler PropertyChanged;
        void RaisePropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }


    abstract public class WhereCondition
    {
        public abstract bool Evaluate(MemberOrTypeViewModelBase memberVM, bool caseSensitive,
            SearchExpression filter,
            Func<string, object> getter);

        public static object Unset = typeof(WhereCondition);

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

                var first = true;
                foreach (var item in list)
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }
                    first = false;

                    sb.Append(ToWhereString(item));
                }

                return sb.ToString();
            }

            else
            {
                return value.ToString();
            }
        }
    }

    public class SearchComparisson : WhereCondition
    {
        public string Key;
        public string Value;
        public SearchConditionOperator Operator;

        public override bool Evaluate(MemberOrTypeViewModelBase memberVM, bool caseSensitive,
            SearchExpression filter,
            Func<string, object> getter)
        {
            //var propInfo = memberVM.GetType().GetProperty(Key);
            //if (propInfo == null)
            //    return false;
            //var value = propInfo.GetValue(memberVM);

            var value = getter(Key);
            if (value != WhereCondition.Unset)
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

                        if (Manager.MatchesFilterString(regex, type, true, true, Manager.Settings, ref meaningfulMatch))
                            return true;

                        foreach (var member in type.Members)
                        {
                            var filtersMatch = true;
                            Manager.MemberMatchesFilters(member, regex, ref abort, out filtersMatch, out meaningfulMatch);
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

    public class CompoundSearchCondition : WhereCondition
    {
        public WhereCondition Operand1 { get; set; }
        public WhereCondition Operand2 { get; set; }

        public CompoundSearchOperator Operator { get; set; }

        public override bool Evaluate(MemberOrTypeViewModelBase memberVM, bool caseSensitive,
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
