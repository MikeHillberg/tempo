using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;

namespace Tempo
{
    /// <summary>
    /// Parses and evaluates an AQS Expression
    /// See https://learn.microsoft.com/en-us/windows/win32/search/-search-3x-advancedquerysyntax
    /// The 'AND' operator can be implicit
    /// </summary>
    public class AqsExpression
    {
        // General flow is: TryParse parses into _expression, which can then be used in Evaluate
        // The caller passes a property evaluator to the Evaluate method, to call back and get values
        // Supports AND, OR, NOT, and parens operators
        // If an operator is omitted, an AND is implicitly inserted

        List<AqsToken> _expression = new List<AqsToken>();
        bool _caseSensitive;
        bool _isWildcardSyntax;
        private string[] _customOperands;

        /// <summary>
        /// Custom operands in the parsed output. For example in "a OR b:True", "a" is a custom operand
        /// </summary>
        public string[] CustomOperands => _customOperands;

        private AqsExpression(bool caseSensitive, bool isWildcardSyntax)
        {
            _caseSensitive = caseSensitive;
            _isWildcardSyntax = isWildcardSyntax;
        }

        /// <summary>
        /// Parse an AQS string. Returns null if invalid
        /// </summary>
        internal static AqsExpression TryParse(
            string s,
            bool caseSensitive,
            bool isWildcardSyntax,
            Func<string, bool> keyValidator,
            Func<string, object> customOperandCallback)
        {
            var expression = new AqsExpression(caseSensitive, isWildcardSyntax);
            int i = 0;
            if (expression.TryParse(
                s.MyCompressSpaces().Split(' '),
                ref i,
                keyValidator,
                customOperandCallback,
                out var errorMessage))
            {
                return expression;
            }
            else
            {
                DebugLog.Append("AQS parse error: " + errorMessage);
                return null;
            }
        }


        /// <summary>
        /// Worker to parse an AQS string
        /// </summary>
        /// <param name="propagatingKey">Used by recursive calls from this method</param>
        /// <returns></returns>
        bool TryParse(
            string[] tokenStrings,
            ref int tokenIndex,
            Func<string, bool> keyValidator,
            Func<string, object> customOperandCallback,
            out string errorMessage,
            string propagatingKey = null)
        {
            // General structure is to convert the expression into post-fix notation.
            // Operator precedence is: NOT (highest), AND, OR
            //
            // Examples
            // a AND b OR c
            // => a, b, AND, c, OR
            //
            // a OR b AND NOT c
            // => a, b, c, NOT, AND, OR
            //
            // a AND ( b OR c )
            // => a, b, c, OR, AND


            // Operator stack is used while we're waiting for an operand, in the conversion to post-fix
            // E.g. in `a OR b` we need to remember the `OR` while we're waiting for the `b`
            Stack<AqsOperator> operatorStack = new Stack<AqsOperator>();

            // When we're expecting an AND or an OR, if we don't get one, implicitly add an AND
            var expectingAndor = false;

            List<string> customOperands = null;

            errorMessage = null;

            for (; tokenIndex < tokenStrings.Length; tokenIndex++)
            {
                var tokenString = tokenStrings[tokenIndex];

                // If the original string had multiple spaces in it we'll get an
                // empty string, which will break things if we try to Peek() ahead
                Debug.Assert(!string.IsNullOrEmpty(tokenString));

                if (tokenString == "")
                {
                    // Extra spaces, ignore
                    continue;
                }

                // Try to parse this as an operator
                var op = ParseAqsOperator(tokenString);

                if (op == AqsOperator.None)
                {
                    // It's not an operator, so it's an operand

                    // Typical case, no `propagatingKey`
                    if (propagatingKey == null)
                    {
                        if (operatorStack.MyPeekOrDefault() == AqsOperator.Matches)
                        {
                            // Last thing we saw was a ":" operator, so this must be the right hand side
                            // Add it to the expression as a Regex
                            _expression.Add(new AqsToken(CreateRegex(tokenString)));

                            // We know the next iteration will push the ":" on, so put it on now,
                            // which will make it easier on next iteration to figure out if we 
                            // need to add an implicit AND
                            var matches = operatorStack.Pop();
                            _expression.Add(new AqsToken(matches));

                            // Set this to indicate that if the next thing isn't an operator, to add an implicit AND
                            expectingAndor = true;
                        }

                        else
                        {
                            // This is a left-hand side. It could be the "a" in "a:1",
                            // or could be in just "a". To figure out, peek ahead to see if there's a ":"
                            // (If not it's treated as a Custom operand

                            var customOperand = false;

                            var next = tokenStrings.MyPeekNext(tokenIndex);
                            var nextOp = ParseAqsOperator(next);
                            if (nextOp != AqsOperator.Matches)
                            {
                                // What we have is just an operand, no operator coming up,
                                // treat it as if ":Custom" is coming up
                                customOperand = true;
                            }
                            else
                            {
                                // This is the left-hand side of "a:1"
                                // Break down the property path and validate each part.
                                // This could be "key:value" or just "value". In the latter case the call to get the
                                // value will fail.
                                foreach (var part in tokenString.Split('.'))
                                {
                                    if (!keyValidator(part))
                                    {
                                        SearchExpression.RaiseSearchExpressionError();
                                        errorMessage = $"Unknown key: {tokenString}";
                                        return false;
                                    }
                                }
                            }

                            if (expectingAndor)
                            {
                                // The last thing we saw was an operand, so there should have seen an operator
                                // before this current operand. So add an And as if we saw one (to the operator stack)
                                // This causes e.g. `a:Foo b:Bar` to become `a:Foo AND b:Bar`

                                PushOperator(operatorStack, AqsOperator.And);
                                expectingAndor = false;
                            }

                            // Add the LHS to the expression

                            if (!customOperand)
                            {
                                // Normal case of a lef-hand side
                                _expression.Add(new AqsToken(tokenString));
                            }
                            else
                            {
                                // What was expected to be an LHS isn't recognized, so assume it's a custom operand
                                // Add the (unary) operand and insert a "Custom" operator
                                // For example "a:foo && b && !c:bar" becomes
                                // a MATCHES foo && CUSTOM b && NOT (c MATCHES bar)"

                                // Let the caller modify the operand first
                                var convertedOperand = customOperandCallback(tokenString);

                                _expression.Add(new AqsToken(new CustomOperandHolder(convertedOperand)));
                                _expression.Add(new AqsToken(AqsOperator.Custom));

                                // Just like after a "NOT C" we expect an operator after a
                                // "CUSTOM B"
                                expectingAndor = true;

                                // Remember the custom operands for the CustomOperands property
                                if (customOperands == null)
                                {
                                    customOperands = new List<string>();
                                }
                                customOperands.Add(tokenString);
                            }
                        }
                    }

                    else
                    {
                        // We have a `propagatingKey`
                        //
                        // What this means is that we're propgating an operand. For example, in
                        //    a:(foo OR bar)
                        // we propagate the "a" to convert to
                        //    (a:foo OR a:bar)
                        //
                        // In postfix notation this becomes
                        // "a, foo, MATCHES, a, bar, MATCHES, OR

                        _expression.Add(new AqsToken(propagatingKey));           // LHS
                        _expression.Add(new AqsToken(CreateRegex(tokenString))); // RHS
                        _expression.Add(new AqsToken(AqsOperator.Matches));      // :
                    }
                }

                else
                {
                    // This token is some kind of operator

                    // If this is a NOT or open paren, we still need to inject an AND
                    if(expectingAndor
                        && (op == AqsOperator.Not || op == AqsOperator.OpenParen))
                    {
                        PushOperator(operatorStack, AqsOperator.And);
                    }
                    expectingAndor = false;

                    // If it's an "(", then parse inside the parens recursively
                    if (op == AqsOperator.OpenParen)
                    {
                        // No support yet for nested parens (but should be easy?)
                        if (propagatingKey != null)
                        {
                            errorMessage = "nested parens not supported";
                            return false;
                        }

                        // Figure out if this paren is for a subexpression or for a value
                        // I.e. is this something like `a:foo AND (b:bar OR c:baz)`
                        // or `a:foo AND b:(bar OR baz)`
                        // If it's the latter, we'll set `operandToPropagate` to `b`,
                        // then in the recursive call it will convert to
                        // `(b:bar OR b:baz)`

                        string operandToPropagate = null;
                        var topOp = operatorStack.MyPeekOrDefault();
                        if (topOp == AqsOperator.Matches)
                        {
                            // The last operator we saw was a ":", so we're in the case of
                            // `a:foo AND b:(bar OR baz)

                            // Remove `a` from the _expression, and `:` from the operatorStack, and pass the `a`
                            // in to a recursive call as the propagatingKey parameter

                            operandToPropagate = _expression.Last().Lhs;
                            _expression.RemoveAt(_expression.Count - 1);
                            operatorStack.Pop();
                        }

                        // Advance past the open paren and recurse
                        ++tokenIndex;
                        if (!TryParse(tokenStrings, ref tokenIndex, keyValidator, customOperandCallback, out errorMessage, operandToPropagate))
                        {
                            return false;
                        }

                        // After completing a nested expression, we expect an AND or OR next
                        expectingAndor = true;
                    }

                    else if (op == AqsOperator.CloseParen)
                    {
                        // On close paren, return from the recursion
                        break;
                    }
                    else
                    {
                        // It's an operator, but not a paren
                        PushOperator(operatorStack, op);
                    }

                }

            }

            // Any remaining operators on the stack pop off and get added to the expression
            // E.g. if we started with `a OR b AND c`, the expression right now will be
            // "a, b, c" and the operator stack will be "AND, OR" (the AND pops first)
            while (operatorStack.Count != 0)
            {
                var op = operatorStack.Pop();
                _expression.Add(new AqsToken(op));
            }

            // Provide the caller with the list of custom operands that were found during parse
            if (customOperands != null)
            {
                _customOperands = customOperands.ToArray();
            }

            return true;
        }

        private void PushOperator(Stack<AqsOperator> operatorStack, AqsOperator op)
        {
            // Compare the current operator with those on the operator stack.
            // If what's on the stack is higher precedent than the current operator, pop off the stack
            // and add to the final expression.
            while (operatorStack.Count != 0)
            {
                var topOp = operatorStack.Peek();
                if ((int)topOp > (int)op)
                {
                    _expression.Add(new AqsToken(operatorStack.Pop()));
                }
                else
                {
                    break;
                }
            }

            // Push this current operator on the stack.
            // (All operators depend on operands that are later in the string expression)
            operatorStack.Push(op);
        }

        /// <summary>
        /// Helper to create a Regex, catching argument exceptions and converted from wildcard syntax
        /// </summary>
        Regex CreateRegex(string value)
        {
            return CreateRegex(value, _caseSensitive, _isWildcardSyntax);
        }

        /// <summary>
        /// Helper to create a Regex, catching argument exceptions and converted from wildcard syntax
        /// </summary>
        public static Regex CreateRegex(string value, bool caseSensitive, bool isWildcardSyntax)
        {
            if (isWildcardSyntax)
            {
                // Convert wildcard syntax to regex syntax

                value = value.Replace(".", @"\.");
                value = value.Replace("*", ".*");
                value = value.Replace("?", ".");
                value = $"^{value}$";
            }

            try
            {
                return new Regex(value, caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
            }
            catch (System.ArgumentException)
            {
                // Ignore invalid regex expression (user might still be composing the expression)
                return null;
            }
        }

        /// <summary>
        /// Evaluate the expression using values from a property evaluator.
        /// If a property can't be evaluated, the condition it's in is assumed to be true.
        /// For example, if you have HasParameters:Foo in the expression, which doesn't have meaning
        /// for an event, it will evaluate to true.
        /// If a key isn't recognized, the customEvaluator will be called
        /// </summary>
        /// <param name="propertyEvaluator">Look up a property value given its name</param>
        /// <param name="customEvaluator">Look up custom operands</param>
        internal bool Evaluate(Func<string, string> propertyEvaluator, Func<object, bool> customEvaluator)
        {
            if (_expression == null || _expression.Count == 0)
            {
                return true;
            }

            // `calculation` is the state of the calculation. Operands get pushed onto it, then
            // when an operator shows up pop the operands off the calculation, execute the operator, then
            // put the result back.
            // E.g. of _expression has "a,b,OR", then calculation will be
            // "a", then "a, b", and then when the OR shows up calculation will become "true" or "false"
            var calculation = new Stack<AqsToken>();

            foreach (var token in _expression)
            {
                if (token.IsOperand)
                {
                    calculation.Push(token);
                    continue;
                }

                // First check the two unary operators: CUSTOM and NOT
                // Custom is higher precedence, so that 
                // !b and a:1
                // becomes
                // (NOT (CUSTOM b)) && a:1
                if (token.Operator == AqsOperator.Custom)
                {
                    // An expression of "a:Foo && a" becomes
                    // "a MATCHES foo && CUSTOM b"
                    // So we're on "CUSTOM" now and the top of 'calculation' is "b"
                    if (!calculation.MyTryPop(out var custom))
                    {
                        throw new AqsException("no value for custom operator");
                    }

                    // Call back to evaluate the custom operaror on this operand
                    calculation.Push(new AqsToken(customEvaluator(custom.CustomOperand)));
                }

                else if (token.Operator == AqsOperator.Not)
                {
                    // An expression of
                    // "a:foo AND NOT b:bar"
                    // would be in _expression as
                    // "a, foo, :, b, bar, :, NOT, AND"
                    // So by the time we reach the NOT operator, calculation will have evaluated a and b, and be
                    // "boolean (from b:bar), boolean (from a:foo)"
                    // So pop the boolean result of "b:bar" off the stack, invert it, and push it back on

                    if (!calculation.MyTryPop(out var operand)
                        || !operand.IsBoolean)
                    {
                        throw new AqsException("misapplied not");
                    }

                    operand.Boolean = !operand.Boolean;
                    calculation.Push(operand);
                }

                else
                {
                    // This token is an AND, OR, or MATCHES operator.
                    // Get the two most recent operands and compare, putting the result back into `calculation`

                    if (!calculation.MyTryPop(out var operand1)
                        || !calculation.MyTryPop(out var operand2)
                        || !operand1.IsOperand && !operand1.IsBoolean
                        || !operand2.IsOperand && !operand2.IsBoolean)
                    {
                        throw new AqsException("operator applied but not to operands");
                    }


                    if (token.Operator == AqsOperator.And
                        || token.Operator == AqsOperator.Or)
                    {
                        // For an AND/OR we have special handling for null values

                        bool? result = null;

                        var bool1 = operand1.Boolean;
                        var bool2 = operand2.Boolean;

                        if (bool1 == null || bool2 == null)
                        {
                            // One of the values is unknown. We handle that by ignoring it, but how to ignore depends on the context.
                            // For example if U is unknown, then
                            // A || U and A && U should be A
                            // A && U || B should be A || B
                            // A || U && B should be A && B

                            // If bool1 is null, then the result is bool2, which could be null
                            // If bool2 is null, then the result is bool1 (which, if null, see previous line)
                            result = bool1 == null ? bool2 : bool1;
                        }

                        else if (token.Operator == AqsOperator.And)
                        {
                            result = (bool1 == true) && (bool2 == true);
                        }

                        else
                        {
                            result = (bool1 == true) || (bool2 == true);
                        }

                        calculation.Push(new AqsToken(result));
                    }

                    else if (token.Operator == AqsOperator.Matches)
                    {
                        // Use the callback to get the value
                        if (operand2.Lhs == null)
                        {
                            throw new AqsException("missing lhs");
                        }
                        var actualValue = propertyEvaluator(operand2.Lhs);

                        if (actualValue != null)
                        {
                            var match = operand1.Rhs.Match(actualValue);
                            var isMatch = match != Match.Empty;
                            calculation.Push(new AqsToken(isMatch));
                        }
                        else
                        {
                            // Unrecognized key. That gets represented as null
                            calculation.Push(new AqsToken((bool?)null));
                        }
                    }
                }
            }

            // By the time we get here there should be one entry in `calculation`, a boolean
            if (calculation.Count != 1 || !calculation.Peek().IsBoolean)
            {
                throw new AqsException("ended with bad calculation");
            }
            else
            {
                // Verbose code to enable easier breakpoints
                if (calculation.Pop().Boolean == true)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static bool IsOperator(string s)
        {
            return ParseAqsOperator(s) != AqsOperator.None;
        }

        static AqsOperator ParseAqsOperator(string s)
        {
            if (s == "(")
            {
                return AqsOperator.OpenParen;
            }
            if (s == ")")
            {
                return AqsOperator.CloseParen;
            }

            switch (s)
            {
                case "OR":
                case "||":
                    return AqsOperator.Or;

                case "AND":
                case "&&":
                    return AqsOperator.And;

                case "NOT":
                case "!":
                    return AqsOperator.Not;

                case ":":
                    return AqsOperator.Matches;
            }

            return AqsOperator.None;
        }


        internal enum AqsOperator
        {
            None = 0,
            Or,
            And,
            Not,
            Custom, // Case where no operator was given for an operand. (Unary operator)
            OpenParen,
            CloseParen,
            Matches
        }

        /// <summary>
        /// A custom operand is wrapped in this Holder, which can then
        /// be put into an AqsToken
        /// </summary>
        internal class CustomOperandHolder
        {
            internal CustomOperandHolder(object customOperand)
            {
                CustomOperand = customOperand;
            }

            internal object CustomOperand { get; }
        }



        /// <summary>
        /// Operators, operands, or boolean values that are entries in the expression or evaluation
        /// </summary>
        class AqsToken
        {
            /// <summary>
            /// Regex for the right hand side
            /// </summary>
            public AqsToken(Regex rhs)
            {
                Rhs = rhs;
            }

            /// <summary>
            /// String for the left hand side
            /// </summary>
            public AqsToken(string lhs)
            {
                Lhs = lhs;
            }

            public AqsToken(AqsOperator op)
            {
                Operator = op;
            }

            public AqsToken(bool? value)
            {
                Boolean = value;
            }

            public AqsToken(CustomOperandHolder customOperandHolder)
            {
                _customOperandHolder = customOperandHolder;
            }

            public object CustomOperand => _customOperandHolder?.CustomOperand;


            public bool IsOperand { get { return Lhs != null || Rhs != null || IsCustomOperand; } }
            public bool IsOperator => Operator != AqsOperator.None;
            public bool IsBoolean => !IsOperand && !IsOperator;
            public bool IsCustomOperand => _customOperandHolder != null;


            public override string ToString()
            {
                if (Lhs != null)
                {
                    return Lhs.ToString();
                }
                else if (Rhs != null)
                {
                    return Rhs.ToString();
                }
                else if (IsOperator)
                {
                    return Operator.ToString();
                }
                else if (Boolean != null)
                {
                    return Boolean.ToString();
                }
                else
                {
                    return base.ToString();
                }
            }

            // Only one of these is ever set
            public string Lhs = null;
            public Regex Rhs = null;
            public bool? Boolean = null;
            public AqsOperator Operator = AqsOperator.None;
            private CustomOperandHolder _customOperandHolder;
        }

    }

    public class AqsException : Exception
    {
        internal AqsException(string message)
            : base($"AQS evaluation error: {message}")
        {
        }
        //internal AqsException(string message, string tokenString)
        //    : base($"AQS evaluation error: {message}\nAt: '{tokenString}'")
        //{
        //}
    }
}
