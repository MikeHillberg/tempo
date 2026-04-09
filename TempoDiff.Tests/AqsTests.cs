using Tempo;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tempo;

namespace Tempo.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class AqsTests
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ModelTests.LoadWinUI2TypeSet("AqsTestsTypeSet");
        }

        [TestInitialize]
        public void TestInitialize()
        {
            ModelTests.ResetManagerState();
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithNoAqsExpression_ReturnsNull()
        {
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = ""; // No AQS expression

            DesktopManager2.Initialize(wpfApp: false, packagesCachePath: @"c:\temp\tests");
            var typeSet = DesktopManager2.LoadWindowsTypes(useWinRTProjections: false);
            var typeVM = typeSet.Types.First();

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) => null,
                (customOperand) => false);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithMatchingCondition_ReturnsTrue()
        {
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "IsClass:true";

            DesktopManager2.Initialize(wpfApp: false, packagesCachePath: @"c:\temp\tests");
            var typeSet = DesktopManager2.LoadWindowsTypes(useWinRTProjections: false);

            var classType = typeSet.Types.FirstOrDefault(t => t.IsClass);
            Assert.IsNotNull(classType);

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithNonMatchingCondition_ReturnsFalse()
        {
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "IsClass:true";

            DesktopManager2.Initialize(wpfApp: false, packagesCachePath: @"c:\temp\tests");
            var typeSet = DesktopManager2.LoadWindowsTypes(useWinRTProjections: false);

            var interfaceType = typeSet.Types.FirstOrDefault(t => t.IsInterface);
            Assert.IsNotNull(interfaceType);

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "False";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsFalse(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithAndCondition_EvaluatesCorrectly()
        {
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "IsClass:true AND IsPublic:true";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    if (key.Equals("IsPublic", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithOrCondition_EvaluatesCorrectly()
        {
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "IsClass:true OR IsInterface:true";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "False";
                    }
                    if (key.Equals("IsInterface", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithCustomOperand_CallsCustomEvaluator()
        {
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "Button";  // Custom operand (no key:value syntax)

            var customEvaluatorCalled = false;

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) => null,
                (customOperand) =>
                {
                    customEvaluatorCalled = true;
                    return true;
                });

            Assert.IsTrue(customEvaluatorCalled);
            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithNotOperator_EvaluatesCorrectly()
        {
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "NOT IsClass:true";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "False";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithParens_ChangesEvaluationOrder()
        {
            // Without parens: "A AND B OR C" would be "(A AND B) OR C"
            // With parens: "A AND (B OR C)" changes the evaluation order
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "IsClass:true AND (IsPublic:true OR IsAbstract:true)";

            // IsClass=true, IsPublic=false, IsAbstract=true
            // Result should be: true AND (false OR true) = true AND true = true
            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    if (key.Equals("IsPublic", StringComparison.OrdinalIgnoreCase))
                    {
                        return "False";
                    }
                    if (key.Equals("IsAbstract", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithParens_GroupsOrBeforeAnd()
        {
            // "A OR B AND C" without parens would be "A OR (B AND C)"
            // With parens: "(A OR B) AND C" changes the evaluation order
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "(IsClass:true OR IsInterface:true) AND IsPublic:true";

            // IsClass=false, IsInterface=true, IsPublic=false
            // Result should be: (false OR true) AND false = true AND false = false
            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "False";
                    }
                    if (key.Equals("IsInterface", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    if (key.Equals("IsPublic", StringComparison.OrdinalIgnoreCase))
                    {
                        return "False";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsFalse(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithNestedParens_EvaluatesCorrectly()
        {
            // Nested parentheses
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "IsClass:true AND ((IsPublic:true OR IsAbstract:true) AND IsSealed:false)";

            // IsClass=true, IsPublic=false, IsAbstract=true, IsSealed=false
            // Result should be: true AND ((false OR true) AND true) = true AND (true AND true) = true AND true = true
            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    if (key.Equals("IsPublic", StringComparison.OrdinalIgnoreCase))
                    {
                        return "False";
                    }
                    if (key.Equals("IsAbstract", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    if (key.Equals("IsSealed", StringComparison.OrdinalIgnoreCase))
                    {
                        return "False";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithNotAndParens_EvaluatesCorrectly()
        {
            // NOT with parentheses
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "NOT (IsClass:true AND IsPublic:true)";

            // IsClass=true, IsPublic=false
            // Result should be: NOT (true AND false) = NOT false = true
            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    if (key.Equals("IsPublic", StringComparison.OrdinalIgnoreCase))
                    {
                        return "False";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithParensOnly_EvaluatesCorrectly()
        {
            // Simple parentheses around a single condition
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "(IsClass:true)";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithDoubleAmpersandSyntax_EvaluatesCorrectly()
        {
            // Using && instead of AND
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "IsClass:true && IsPublic:true";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    if (key.Equals("IsPublic", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithDoublePipeSyntax_EvaluatesCorrectly()
        {
            // Using || instead of OR
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "IsClass:true || IsInterface:true";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "False";
                    }
                    if (key.Equals("IsInterface", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithExclamationSyntax_EvaluatesCorrectly()
        {
            // Using ! instead of NOT
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "!IsClass:true";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "False";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithMixedOperatorSyntax_EvaluatesCorrectly()
        {
            // Mix of && and OR syntax
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "IsClass:true && IsPublic:true || IsInterface:true";

            // IsClass=true, IsPublic=true, IsInterface=false
            // Result should be: (true && true) || false = true || false = true
            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    if (key.Equals("IsPublic", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    if (key.Equals("IsInterface", StringComparison.OrdinalIgnoreCase))
                    {
                        return "False";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithExclamationAndParens_EvaluatesCorrectly()
        {
            // Using ! with parentheses
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "!(IsClass:true && IsPublic:true)";

            // IsClass=true, IsPublic=false
            // Result should be: !(true && false) = !false = true
            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    if (key.Equals("IsPublic", StringComparison.OrdinalIgnoreCase))
                    {
                        return "False";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithAllAlternativeSyntax_EvaluatesCorrectly()
        {
            // Using all alternative syntax: &&, ||, !
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "!IsEnum:true && (IsClass:true || IsInterface:true)";

            // IsEnum=false, IsClass=true, IsInterface=false
            // Result should be: !false && (true || false) = true && true = true
            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsEnum", StringComparison.OrdinalIgnoreCase))
                    {
                        return "False";
                    }
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    if (key.Equals("IsInterface", StringComparison.OrdinalIgnoreCase))
                    {
                        return "False";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithGreaterThanOrEqual_ReturnsTrue()
        {
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "TotalMembers :>= 2";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("TotalMembers", StringComparison.OrdinalIgnoreCase))
                    {
                        return "5";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithGreaterThanOrEqual_ReturnsFalse()
        {
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "TotalMembers :>= 10";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("TotalMembers", StringComparison.OrdinalIgnoreCase))
                    {
                        return "5";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsFalse(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithGreaterThan_ReturnsTrue()
        {
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "TotalMembers :> 2";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("TotalMembers", StringComparison.OrdinalIgnoreCase))
                    {
                        return "5";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithLessThan_ReturnsTrue()
        {
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "TotalMembers :< 10";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("TotalMembers", StringComparison.OrdinalIgnoreCase))
                    {
                        return "5";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithLessThanOrEqual_ReturnsTrue()
        {
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "TotalMembers :<= 5";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("TotalMembers", StringComparison.OrdinalIgnoreCase))
                    {
                        return "5";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithEquals_ReturnsTrue()
        {
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "TotalMembers := 5";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("TotalMembers", StringComparison.OrdinalIgnoreCase))
                    {
                        return "5";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithEquals_ReturnsFalse()
        {
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "TotalMembers := 5";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("TotalMembers", StringComparison.OrdinalIgnoreCase))
                    {
                        return "10";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsFalse(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithNumericComparison_NonNumericRhs_ReturnsNull()
        {
            // RHS is not a number
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "TotalMembers :>= notanumber";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("TotalMembers", StringComparison.OrdinalIgnoreCase))
                    {
                        return "5";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithNumericComparison_UnknownProperty_ReturnsFalse()
        {
            // Property doesn't exist (using a property that doesn't exist in AqsKeyValidator)
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "TotalMembers :>= 2";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) => null,  // Always return null (simulating property doesn't return a value)
                (customOperand) => false);

            Assert.IsNull(result);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithNumericComparisonAndOtherConditions_ReturnsTrue()
        {
            // Combining numeric comparison with other conditions
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "IsClass:true AND TotalMembers :>= 2";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    if (key.Equals("TotalMembers", StringComparison.OrdinalIgnoreCase))
                    {
                        return "5";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithDottedPropertyPath_ReturnsTrue()
        {
            // Using dotted property path like Members.Count
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "Members.Count :> 1";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("Members.Count", StringComparison.OrdinalIgnoreCase))
                    {
                        return "5";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithNoSpacesAroundOperator_ReturnsTrue()
        {
            // Using compact syntax without spaces around the operator
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "TotalMembers:>2";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("TotalMembers", StringComparison.OrdinalIgnoreCase))
                    {
                        return "5";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithRegexPattern_ReturnsTrue()
        {
            // Using regex pattern "Ac.*Br" to match names like "AcrylicBrush"
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "Name:Ac.*Br";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    {
                        return "AcrylicBrush";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithRegexPattern_ReturnsFalse()
        {
            // Using regex pattern "Ac.*Br" which should not match "Button"
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "Name:Ac.*Br";

            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    {
                        return "Button";
                    }
                    return null;
                },
                (customOperand) => false);

            Assert.IsFalse(result == true);
        }

        [TestMethod]
        public void GetMembers_WithRegexPattern_ReturnsMatchingTypes()
        {
            // Search for types with names matching regex "Acr.*Brush" (e.g., AcrylicBrush)
            var searchExpression = new SearchExpression { RawValue = "IsClass:True Name:Acr.*Brush" };

            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(
                        System.Text.RegularExpressions.Regex.IsMatch(typeVM.Name, "Acr.*Brush", System.Text.RegularExpressions.RegexOptions.IgnoreCase),
                        $"Type {typeVM.Name} does not match regex 'Acr.*Brush'");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithNumericGreaterThan_ReturnsMatchingTypes()
        {
            // Search for types with more than 5 total members
            var searchExpression = new SearchExpression { RawValue = "IsClass:True TotalMembers:>5" };

            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.TotalMembers > 5, $"Type {typeVM.Name} has TotalMembers={typeVM.TotalMembers}, expected > 5");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithNumericGreaterThanOrEqual_ReturnsMatchingTypes()
        {
            // Search for classes with at least 10 total members
            var searchExpression = new SearchExpression { RawValue = "IsClass:True TotalMembers:>=10" };

            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.TotalMembers >= 10, $"Type {typeVM.Name} has TotalMembers={typeVM.TotalMembers}, expected >= 10");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithNumericLessThan_ReturnsMatchingTypes()
        {
            // Search for classes with fewer than 3 total members
            var searchExpression = new SearchExpression { RawValue = "IsClass:True TotalMembers:<3" };

            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            // Note: We may find types with very few or no members (like empty interfaces or enums)
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.TotalMembers < 3, $"Type {typeVM.Name} has TotalMembers={typeVM.TotalMembers}, expected < 3");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithNumericLessThanOrEqual_ReturnsMatchingTypes()
        {
            // Arrange - Search for classes with at most 5 total members
            var searchExpression = new SearchExpression { RawValue = "IsClass:True TotalMembers:<=5" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.TotalMembers <= 5, $"Type {typeVM.Name} has TotalMembers={typeVM.TotalMembers}, expected <= 5");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithNumericEquals_ReturnsMatchingTypes()
        {
            // Arrange - Search for classes with exactly 0 total members
            var searchExpression = new SearchExpression { RawValue = "IsClass:True TotalMembers:=0" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.TotalMembers == 0, $"Type {typeVM.Name} has TotalMembers={typeVM.TotalMembers}, expected == 0");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithNumericComparisonAndNameFilter_ReturnsMatchingTypes()
        {
            // Arrange - Search for Button classes with more than 50 total members
            var searchExpression = new SearchExpression { RawValue = "Button IsClass:True TotalMembers:>10" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert - Verify combined filter works (may return 0 if no matching types)
            Assert.IsNotNull(members);
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.Name.Contains("Button"), $"Type {typeVM.Name} does not contain 'Button'");
                    Assert.IsTrue(typeVM.TotalMembers > 10, $"Type {typeVM.Name} has TotalMembers={typeVM.TotalMembers}, expected > 10");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithNumericComparisonAndIsClass_ReturnsMatchingTypes()
        {
            // Arrange - Search for classes with more than 100 total members
            var searchExpression = new SearchExpression { RawValue = "IsClass:true TotalMembers:>40" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert - Verify combined filter works (may return 0 if no matching types)
            Assert.IsNotNull(members);
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.IsClass, $"Type {typeVM.Name} is not a class");
                    Assert.IsTrue(typeVM.TotalMembers > 40, $"Type {typeVM.Name} has TotalMembers={typeVM.TotalMembers}, expected > 40");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithNumericComparisonNoSpaces_ReturnsMatchingTypes()
        {
            // Arrange - Test compact syntax without spaces
            var searchExpression = new SearchExpression { RawValue = "IsClass:True TotalMembers:>20" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert - Verify numeric comparison works (may return 0 if no matching types)
            Assert.IsNotNull(members);
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.TotalMembers > 20, $"Type {typeVM.Name} has TotalMembers={typeVM.TotalMembers}, expected > 20");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithNumericComparisonAndLogicalOr_ReturnsMatchingTypes()
        {
            // Arrange - Search for classes with very few members OR many members
            var searchExpression = new SearchExpression { RawValue = "IsClass:True AND (TotalMembers:<2 OR TotalMembers:>200)" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.TotalMembers < 2 || typeVM.TotalMembers > 200, 
                        $"Type {typeVM.Name} has TotalMembers={typeVM.TotalMembers}, expected < 2 OR > 200");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithNumericComparisonAndLogicalAnd_ReturnsMatchingTypes()
        {
            // Arrange - Search for classes with TotalMembers between 10 and 50 (inclusive)
            var searchExpression = new SearchExpression { RawValue = "TotalMembers:>=10 AND TotalMembers:<=50" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    if (typeVM.IsMatch)
                    {
                        Assert.IsTrue(typeVM.TotalMembers >= 10 && typeVM.TotalMembers <= 50,
                            $"Type {typeVM.Name} has TotalMembers={typeVM.TotalMembers}, expected >= 10 AND <= 50");
                    }
                    else
                    {
                        // Type is in this list because its member(s) matched
                        Assert.IsTrue(typeVM.TotalMembers < 10 || typeVM.TotalMembers > 50);
                    }
                }
            }
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithPropagatedOperand_MatchesFirstValue()
        {
            // Arrange - Using propagated operand syntax: Name:(foo OR bar) becomes (Name:foo OR Name:bar)
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "Name:(Button OR TextBox)";

            // Act - Name=Button should match first value
            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    {
                        return "Button";
                    }
                    return null;
                },
                (customOperand) => false);

            // Assert
            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithPropagatedOperand_MatchesSecondValue()
        {
            // Arrange - Using propagated operand syntax: Name:(foo OR bar) becomes (Name:foo OR Name:bar)
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "Name:(Button OR TextBox)";

            // Act - Name=TextBox should match second value
            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    {
                        return "TextBox";
                    }
                    return null;
                },
                (customOperand) => false);

            // Assert
            Assert.IsTrue(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithPropagatedOperand_NoMatch()
        {
            // Arrange - Using propagated operand syntax: Name:(foo OR bar) becomes (Name:foo OR Name:bar)
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "Name:(Button OR TextBox)";

            // Act - Name=Label should not match either value
            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    {
                        return "Label";
                    }
                    return null;
                },
                (customOperand) => false);

            // Assert
            Assert.IsFalse(result == true);
        }

        [TestMethod]
        public void EvaluateAqsExpression_WithPropagatedOperandAndOtherCondition_EvaluatesCorrectly()
        {
            // Arrange - Combining propagated operand with other conditions
            var searchExpression = new SearchExpression();
            searchExpression.RawValue = "IsClass:true AND Name:(Button OR TextBox)";

            // Act - IsClass=true, Name=Button should match
            searchExpression.EnsureRegex();
            var result = searchExpression.EvaluateAqsExpression(
                (key) =>
                {
                    if (key.Equals("IsClass", StringComparison.OrdinalIgnoreCase))
                    {
                        return "True";
                    }
                    if (key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    {
                        return "Button";
                    }
                    return null;
                },
                (customOperand) => false);

            // Assert
            Assert.IsTrue(result == true);
        }
    }
}








