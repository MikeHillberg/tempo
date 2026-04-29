using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tempo;

namespace Tempo.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class DotNetWindowsTypesTests
    {
        private static readonly string CachePath = Path.Combine(Path.GetTempPath(), "TempoTests.Cache");
        private static TypeSet _dotNetWindowsTypes;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            DesktopManager2.Initialize(wpfApp: false, packagesCachePath: CachePath);

            // Find the .NET Windows Desktop shared framework
            var dotNetPath = Path.Combine(Environment.ExpandEnvironmentVariables("%ProgramFiles%"), "dotnet");
            var dotNetWindowsPath = Path.Combine(dotNetPath, @"shared\Microsoft.WindowsDesktop.App");
            Assert.IsTrue(Directory.Exists(dotNetWindowsPath), $".NET Windows Desktop not found at {dotNetWindowsPath}");

            // Find the highest version
            var versions = Directory.GetDirectories(dotNetWindowsPath)
                .Select(d => Path.GetFileName(d))
                .Where(v => Version.TryParse(v, out _))
                .OrderByDescending(v => new Version(v))
                .ToArray();
            Assert.IsTrue(versions.Length > 0, "No .NET Windows Desktop versions found");

            var versionPath = Path.Combine(dotNetWindowsPath, versions[0]);
            DotNetTypeSet.DotNetCoreVersion = versions[0];

            var files = Directory.GetFiles(versionPath, "*.dll");
            Assert.IsTrue(files.Length > 0, $"No DLLs found in {versionPath}");

            // Load using the TypeSetLoader
            var loader = new DotNetWindowsTypeSetLoader(files);
            _dotNetWindowsTypes = loader.Load();

            Assert.IsNotNull(_dotNetWindowsTypes, "Failed to load DotNet Windows types");
            Assert.IsNotNull(_dotNetWindowsTypes.Types, "Types should not be null");
            Assert.IsTrue(_dotNetWindowsTypes.Types.Count > 0, "Should have loaded types");

            Manager.CurrentTypeSet = _dotNetWindowsTypes;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            ModelTests.ResetManagerState();
            Manager.CurrentTypeSet = _dotNetWindowsTypes;
        }

        [TestMethod]
        public void LoadDotNetWindowsTypes_ReturnsSomething()
        {
            Assert.IsNotNull(_dotNetWindowsTypes);
            Assert.IsTrue(_dotNetWindowsTypes.Types.Count > 1000,
                $"Expected > 1000 types from DotNet Windows, got {_dotNetWindowsTypes.Types.Count}");
        }

        [TestMethod]
        public void ValidateFrameworkElementTypeVM()
        {
            Manager.Settings.MemberKind = MemberKind.Type;
            var searchExpression = new SearchExpression { RawValue = "^FrameworkElement$" };
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);

            // Find the WPF FrameworkElement (System.Windows namespace)
            var typeVM = members.OfType<TypeViewModel>()
                .FirstOrDefault(t => t.FullName == "System.Windows.FrameworkElement");
            Assert.IsNotNull(typeVM, $"Expected to find System.Windows.FrameworkElement. Found: {string.Join(", ", members.OfType<TypeViewModel>().Select(t => t.FullName))}");

            Assert.AreEqual("FrameworkElement", typeVM.Name);
            Assert.AreEqual("System.Windows", typeVM.Namespace);
            Assert.AreEqual("System.Windows.FrameworkElement", typeVM.FullName);

            // Type kind properties
            Assert.IsTrue(typeVM.IsClass);
            Assert.IsFalse(typeVM.IsInterface);
            Assert.IsFalse(typeVM.IsEnum);
            Assert.IsFalse(typeVM.IsValueType);
            Assert.IsFalse(typeVM.IsStruct);
            Assert.IsFalse(typeVM.IsDelegate);

            // Type modifier properties
            Assert.IsFalse(typeVM.IsSealed);
            Assert.IsFalse(typeVM.IsAbstract);
            Assert.IsFalse(typeVM.IsStatic);
            Assert.IsTrue(typeVM.IsPublic);
            Assert.IsFalse(typeVM.IsGenericTypeDefinition);

            // Base type and interfaces
            Assert.IsNotNull(typeVM.BaseType);
            Assert.AreEqual("UIElement", typeVM.BaseType.Name);
            Assert.AreEqual(4, typeVM.PublicInterfaces.Count, $"Expected 4 public interfaces, got {typeVM.PublicInterfaces.Count}");

            // Members - verify exact counts
            Assert.AreEqual(37, typeVM.Properties.Count, $"Expected 37 properties, got {typeVM.Properties.Count}");
            Assert.AreEqual(47, typeVM.Methods.Count, $"Expected 47 methods, got {typeVM.Methods.Count}");
            Assert.AreEqual(12, typeVM.Events.Count, $"Expected 12 events, got {typeVM.Events.Count}");
            Assert.AreEqual(1, typeVM.Constructors.Count, $"Expected 1 constructor, got {typeVM.Constructors.Count}");
            Assert.AreEqual(588, typeVM.TotalMembers, $"Expected 588 total members, got {typeVM.TotalMembers}");

            // Other type properties
            Assert.IsFalse(typeVM.IsVirtual);
            Assert.IsFalse(typeVM.IsFamily);
            Assert.IsFalse(typeVM.IsAssembly);
            Assert.IsFalse(typeVM.IsInternal);
            Assert.AreEqual(MemberKind.Type, typeVM.MemberKind);
            Assert.AreEqual(TypeKind.Class, typeVM.TypeKind);
            Assert.AreEqual("public", typeVM.ModifierCodeString);
        }

        [TestMethod]
        public void ValidateFrameworkElementTypeVM_ProtectedInternalMembers()
        {
            Manager.Settings.MemberKind = MemberKind.Type;
            var searchExpression = new SearchExpression { RawValue = "^FrameworkElement$" };
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            var typeVM = members.OfType<TypeViewModel>()
                .FirstOrDefault(t => t.FullName == "System.Windows.FrameworkElement");
            Assert.IsNotNull(typeVM);

            // FrameworkElement is unsealed, so protected internal members should be visible
            // Find members whose ModifierCodeString contains both "protected" and "internal"
            var protectedInternalProps = typeVM.Properties
                .Where(p => p.ModifierCodeString.Contains("protected") && p.ModifierCodeString.Contains("internal"))
                .ToList();

            var protectedInternalMethods = typeVM.Methods
                .Where(m => m.ModifierCodeString.Contains("protected") && m.ModifierCodeString.Contains("internal"))
                .ToList();

            var totalProtectedInternal = protectedInternalProps.Count + protectedInternalMethods.Count;

            Assert.IsTrue(totalProtectedInternal > 0,
                $"Expected protected internal members on FrameworkElement. " +
                $"Properties: {typeVM.Properties.Count}, Methods: {typeVM.Methods.Count}");

            // Verify the modifier string is exactly "protected internal"
            if (protectedInternalProps.Count > 0)
            {
                var prop = protectedInternalProps[0];
                Assert.AreEqual("protected internal", prop.ModifierCodeString,
                    $"Property '{prop.Name}' should have modifier 'protected internal'");
            }
            else
            {
                var method = protectedInternalMethods[0];
                Assert.AreEqual("protected internal", method.ModifierCodeString,
                    $"Method '{method.Name}' should have modifier 'protected internal'");
            }
        }

        [TestMethod]
        public void SearchDefaultStyleKey_ReturnsResults()
        {
            Manager.Settings.MemberKind = MemberKind.Property;
            var searchExpression = new SearchExpression { RawValue = "^DefaultStyleKey$" };
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.IsTrue(members.Count > 0,
                $"Expected to find DefaultStyleKey. Found: {members.Count}");

            // Filter to only PropertyViewModels (search may also return types)
            var props = members.OfType<PropertyViewModel>().ToList();
            Assert.IsTrue(props.Count > 0,
                $"Expected to find DefaultStyleKey properties. Found {members.Count} results but {props.Count} properties");

            foreach (var prop in props)
            {
                Assert.AreEqual("DefaultStyleKey", prop.Name);
                Assert.IsFalse(prop.IsPrivate,
                    $"DefaultStyleKey on {prop.DeclaringType.FullName} should not be private. " +
                    $"IsPublic={prop.IsPublic}, IsProtected={prop.IsProtected}, IsInternal={prop.IsInternal}, " +
                    $"ModifierCodeString='{prop.ModifierCodeString}'");
            }
        }

        [TestMethod]
        public void ValidateDefaultParameterValues()
        {
            // Find methods with default parameter values across types
            var typesWithMethods = _dotNetWindowsTypes.Types
                .Where(t => t.Methods != null && t.Methods.Count > 0)
                .Take(200);

            ParameterViewModel paramWithDefault = null;
            string ownerMethod = null;

            foreach (var type in typesWithMethods)
            {
                foreach (var method in type.Methods)
                {
                    foreach (var param in method.Parameters)
                    {
                        if (param.HasDefaultValue)
                        {
                            paramWithDefault = param;
                            ownerMethod = $"{type.FullName}.{method.Name}";
                            break;
                        }
                    }
                    if (paramWithDefault != null) break;
                }
                if (paramWithDefault != null) break;
            }

            Assert.IsNotNull(paramWithDefault,
                "Expected to find at least one parameter with a default value in DotNetWindows types");

            Assert.IsTrue(paramWithDefault.HasDefaultValue);
            Assert.IsNotNull(paramWithDefault.DefaultValueString,
                $"DefaultValueString should not be null for {ownerMethod} param '{paramWithDefault.Name}'");
        }

        [TestMethod]
        public void ValidateGenericConstraints_DotNetWindows()
        {
            // DotNet Windows has plenty of generic types with constraints
            var genericTypes = _dotNetWindowsTypes.Types
                .Where(t => t.IsGenericType)
                .ToList();

            Assert.IsTrue(genericTypes.Count > 0, "Expected generic types in DotNetWindows");

            // Find a type with constraint clauses
            var typeWithConstraints = genericTypes.FirstOrDefault(t => t.GenericParameterConstraintClauses.Count > 0);
            Assert.IsNotNull(typeWithConstraints,
                $"Expected at least one generic type with constraints. Checked {genericTypes.Count} generic types");

            // Verify constraint clause format
            foreach (var clause in typeWithConstraints.GenericParameterConstraintClauses)
            {
                Assert.IsTrue(clause.StartsWith("where "),
                    $"Constraint clause should start with 'where ': '{clause}'");
                Assert.IsTrue(clause.Contains(" : "),
                    $"Constraint clause should contain ' : ': '{clause}'");
            }

            // Verify GenericParameterAttributes works
            var argWithConstraint = genericTypes
                .SelectMany(t => t.GetGenericArguments())
                .FirstOrDefault(a => a.IsGenericParameter
                    && (a.GenericParameterAttributes != GenericParameterAttributes.None
                        || a.GenericConstraints.Count > 0));

            Assert.IsNotNull(argWithConstraint,
                "Expected at least one generic parameter with attributes or constraints");
        }

        [TestMethod]
        public void SearchHasGenericConstraintClause_ReturnsResults()
        {
            ModelTests.ResetManagerState();
            Manager.CurrentTypeSet = _dotNetWindowsTypes;
            Manager.Settings.MemberKind = MemberKind.Type;
            var searchExpression = new SearchExpression { RawValue = "HasGenericParameterConstraintClauses:True" };
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.IsTrue(members.Count > 0,
                $"Expected results for 'HasGenericParameterConstraintClauses:True'. Got {members.Count}");

            // Every result should be a type with constraints
            foreach (var member in members)
            {
                var type = member as TypeViewModel;
                Assert.IsNotNull(type);
                Assert.IsTrue(type.HasGenericParameterConstraintClauses,
                    $"{type.FullName} should have generic parameter constraint clauses");
            }
        }

        [TestMethod]
        public void SearchHasGenericParameterConstraintClauses_False_FindsButton()
        {
            ModelTests.ResetManagerState();
            Manager.CurrentTypeSet = _dotNetWindowsTypes;
            Manager.Settings.MemberKind = MemberKind.Type;
            var searchExpression = new SearchExpression { RawValue = "HasGenericParameterConstraintClauses:False" };
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            var button = members.OfType<TypeViewModel>().FirstOrDefault(t => t.FullName == "System.Windows.Controls.Button");
            Assert.IsNotNull(button, "Expected to find System.Windows.Controls.Button in results");
            Assert.IsFalse(button.HasGenericParameterConstraintClauses);
        }

        [TestMethod]
        public void SearchGenericWithoutConstraints_FindsPageFunction()
        {
            ModelTests.ResetManagerState();
            Manager.CurrentTypeSet = _dotNetWindowsTypes;
            Manager.Settings.MemberKind = MemberKind.Type;
            var searchExpression = new SearchExpression { RawValue = "HasGenericParameterConstraintClauses:False IsGenericType:True" };
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.IsTrue(members.Count > 0, "Expected results");
            var pageFunction = members.OfType<TypeViewModel>().FirstOrDefault(t => t.Name.StartsWith("PageFunction"));
            Assert.IsNotNull(pageFunction,
                $"Expected to find PageFunction<T> in results. Found: {string.Join(", ", members.OfType<TypeViewModel>().Take(10).Select(t => t.FullName))}");
        }

        [TestMethod]
        public void SearchGenericWithConstraints_FindsFreezableCollection()
        {
            ModelTests.ResetManagerState();
            Manager.CurrentTypeSet = _dotNetWindowsTypes;
            Manager.Settings.MemberKind = MemberKind.Type;
            var searchExpression = new SearchExpression { RawValue = "HasGenericParameterConstraintClauses:True IsGenericType:True" };
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.IsTrue(members.Count > 0, "Expected results for generic types with constraints");
            var freezableCollection = members.OfType<TypeViewModel>().FirstOrDefault(t => t.FullName == "System.Windows.FreezableCollection`1");
            Assert.IsNotNull(freezableCollection,
                $"Expected to find System.Windows.FreezableCollection<T> in results. Found: {string.Join(", ", members.OfType<TypeViewModel>().Take(10).Select(t => t.FullName))}");
            Assert.IsTrue(freezableCollection.HasGenericParameterConstraintClauses);

            // Verify the constraint clause content
            var clauses = freezableCollection.GenericParameterConstraintClauses;
            Assert.IsTrue(clauses.Count > 0, "FreezableCollection should have constraint clauses");
            Assert.IsTrue(clauses[0].StartsWith("where "), $"Clause should start with 'where ': '{clauses[0]}'");
        }

        [TestMethod]
        public void ValidateDefaultParameterValues_Specific()
        {
            // Search for methods and verify some have parameters with default values
            var typesWithMethods = _dotNetWindowsTypes.Types
                .Where(t => t.Methods != null && t.Methods.Count > 0)
                .Take(100);

            var methodsWithDefaults = new List<(string TypeName, string MethodName, string ParamName, string DefaultValue)>();

            foreach (var type in typesWithMethods)
            {
                foreach (var method in type.Methods)
                {
                    foreach (var param in method.Parameters)
                    {
                        if (param.HasDefaultValue)
                        {
                            methodsWithDefaults.Add((
                                type.Name, method.Name, param.Name, param.DefaultValueString));
                        }
                    }
                }
            }

            Assert.IsTrue(methodsWithDefaults.Count > 0,
                "Expected to find methods with default parameter values in DotNetWindows");

            // Verify DefaultValueString is populated for all
            foreach (var (typeName, methodName, paramName, defaultValue) in methodsWithDefaults)
            {
                Assert.IsNotNull(defaultValue,
                    $"{typeName}.{methodName} param '{paramName}' has HasDefaultValue=true but DefaultValueString is null");
            }
        }

        [TestMethod]
        public void ValidateAssemblyMvidUniqueness()
        {
            // Different assemblies in the same type set should have different MVIDs
            var assemblies = _dotNetWindowsTypes.Assemblies;
            if (assemblies.Count < 2) return; // Can't test uniqueness with fewer than 2

            var mvids = assemblies
                .Where(a => a.ModelVersionId != System.Guid.Empty)
                .Select(a => a.ModelVersionId)
                .ToList();

            var uniqueMvids = mvids.Distinct().Count();
            Assert.AreEqual(mvids.Count, uniqueMvids,
                $"All assemblies should have unique MVIDs. Found {mvids.Count} total, {uniqueMvids} unique");
        }
    }
}
