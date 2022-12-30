using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private AqsExpression(bool caseSensitive)
        {
            _caseSensitive = caseSensitive;
        }

        /// <summary>
        /// Parse an AQS string. Returns null if invalid
        /// </summary>
        internal static AqsExpression TryParse(string s, bool caseSensitive, Func<string,bool> keyValidator)
        {
            var expression = new AqsExpression(caseSensitive);
            int i = 0;
            if (expression.TryParse(s.Split(' '), ref i, keyValidator))
            {
                return expression;
            }
            else
            {
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
            Func<string,bool> keyValidator,
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

            // When we're expecting an operator, if we don't get one, implicitly add an AND
            var expectingOperator = false;

            for (; tokenIndex < tokenStrings.Length; tokenIndex++)
            {
                var tokenString = tokenStrings[tokenIndex];

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
                            expectingOperator = true;
                        }
                        else
                        {
                            // This is a left-hand side

                            if(!keyValidator(tokenString))
                            {
                                SearchExpression.RaiseSearchExpressionError();
                                return false;
                            }

                            if (expectingOperator)
                            {
                                // The last thing we saw was an operand, so there should have seen an operator
                                // before this current operand. So add an And as if we saw one (to the operator stack)
                                // This causes e.g. `a:Foo b:Bar` to become `a:Foo AND b:Bar`

                                operatorStack.Push(AqsOperator.And);
                                expectingOperator = false;
                            }

                            // Add the LHS to the expression
                            _expression.Add(new AqsToken(tokenString));
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
                    expectingOperator = false;

                    // If it's an "(", then parse inside the parens recursively
                    if (op == AqsOperator.OpenParen)
                    {
                        // No support yet for nested parens (but should be easy?)
                        if (propagatingKey != null)
                        {
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
                        if (!TryParse(tokenStrings, ref tokenIndex, keyValidator, operandToPropagate))
                        {
                            return false;
                        }
                    }

                    else if (op == AqsOperator.CloseParen)
                    {
                        // On close paren, return from the recursion
                        break;
                    }
                    else
                    {
                        // It's an operator, but not a paren

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

            return true;
        }

        /// <summary>
        /// Helper to create a Regex, catching argument exceptions
        /// </summary>
        Regex CreateRegex(string value)
        {
            try
            {
                return new Regex(value, _caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
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
        /// </summary>
        /// <param name="propertyEvaluator">Look up a property value given its name</param>
        internal bool Evaluate(Func<string, string> propertyEvaluator)
        {
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

                if (token.Operator == AqsOperator.Not)
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
                        throw new Exception("AQS evaluation error (misapplied not)");
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
                        throw new Exception("AQS evaluation error (operator applied but not to operands)");
                    }


                    if (token.Operator == AqsOperator.And)
                    {
                        var result = (operand1.Boolean == true) && (operand2.Boolean == true);
                        calculation.Push(new AqsToken(result));
                    }
                    else if (token.Operator == AqsOperator.Or)
                    {
                        var result = (operand1.Boolean == true) || (operand2.Boolean == true);
                        calculation.Push(new AqsToken(result));
                    }
                    else if (token.Operator == AqsOperator.Matches)
                    {
                        // Use the callback to get the value
                        var actualValue = propertyEvaluator(operand2.Lhs);
                        if (actualValue != null)
                        {
                            var match = operand1.Rhs.Match(actualValue);
                            calculation.Push(new AqsToken(match != Match.Empty));
                        }
                        else
                        {
                            // Unrecognized key. Assume that whatever it's matching against, the answer is false
                            calculation.Push(new AqsToken(false));
                        }
                    }
                }
            }

            // By the time we get here there should be one entry in `calculation`, a boolean
            if (calculation.Count != 1 || !calculation.Peek().IsBoolean)
            {
                throw new Exception("AQS evaluation error (ended with bad calculation)");
            }
            else
            {
                return calculation.Pop().Boolean == true;
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
            OpenParen,
            CloseParen,
            Matches
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

            public AqsToken(bool value)
            {
                Boolean = value;
            }


            public bool IsOperand { get { return Lhs != null || Rhs != null; } }
            public bool IsOperator => !IsOperand && Boolean == null;
            public bool IsBoolean => Boolean != null;

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
        }

    }
}
