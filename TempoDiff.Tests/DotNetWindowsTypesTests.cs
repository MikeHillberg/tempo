using System;
using System.IO;
using System.Linq;
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
    }
}
