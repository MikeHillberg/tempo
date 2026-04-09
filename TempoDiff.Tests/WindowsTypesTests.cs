using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tempo;

namespace Tempo.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class WindowsTypesTests
    {
        private static TypeSet _windowsTypes;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            _windowsTypes = DesktopManager2.LoadWindowsTypes(useWinRTProjections: false);
            Manager.CurrentTypeSet = _windowsTypes;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            ModelTests.ResetManagerState();

            // Set Manager.CurrentTypeSet (another test class may have changed it)
            Manager.CurrentTypeSet = _windowsTypes;
        }

        [TestMethod]
        public void LoadWindowsTypes_ReturnsSomething()
        {
            Assert.IsNotNull(_windowsTypes);

            // 25h2, 26220.7752
            Assert.IsTrue(_windowsTypes.AssemblyLocations.Count >= 0x14);
            Assert.IsTrue(_windowsTypes.Types.Count >= 0x3959);
        }

        [TestMethod]
        public void ValidateUISettingsTypeVM()
        {
            Manager.Settings.MemberKind = MemberKind.Type;
            var searchExpression = new SearchExpression { RawValue = "^UISettings$" };

            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.AreEqual(1, members.Count, $"Expected to find 1 result for ^UISettings$. Found: {members.Count} - {string.Join(", ", members.OfType<TypeViewModel>().Select(t => t.FullName))}");

            var typeVM = members[0] as TypeViewModel;
            Assert.IsNotNull(typeVM, "Expected result to be a TypeViewModel");

            // Validate TypeViewModel properties - Name and Namespace
            Assert.AreEqual("UISettings", typeVM.Name);
            Assert.AreEqual("Windows.UI.ViewManagement", typeVM.Namespace);
            Assert.AreEqual("Windows.UI.ViewManagement.UISettings", typeVM.FullName);

            // Type kind properties
            Assert.IsTrue(typeVM.IsClass, "UISettings should be a class");
            Assert.IsFalse(typeVM.IsInterface, "UISettings should not be an interface");
            Assert.IsFalse(typeVM.IsEnum, "UISettings should not be an enum");
            Assert.IsFalse(typeVM.IsValueType, "UISettings should not be a value type");
            Assert.IsFalse(typeVM.IsStruct, "UISettings should not be a struct");
            Assert.IsFalse(typeVM.IsDelegate, "UISettings should not be a delegate");

            // Type modifier properties
            Assert.IsTrue(typeVM.IsSealed, "UISettings should be sealed");
            Assert.IsFalse(typeVM.IsAbstract, "UISettings should not be abstract");
            Assert.IsFalse(typeVM.IsStatic, "UISettings should not be static");
            Assert.IsTrue(typeVM.IsPublic, "UISettings should be public");
            Assert.IsFalse(typeVM.IsGenericTypeDefinition, "UISettings should not be a generic type definition");

            // Base type and interfaces
            // Note: UISettings may not have a base type exposed in WinRT metadata
            if (typeVM.BaseType != null)
            {
                Assert.AreEqual("Object", typeVM.BaseType.Name, "UISettings's base type should be Object");
            }
            Assert.IsNotNull(typeVM.Interfaces, "UISettings should have interfaces collection");

            // Members - verify exact counts
            Assert.AreEqual(15, typeVM.Properties.Count, "UISettings should have 15 properties");
            Assert.AreEqual(2, typeVM.Methods.Count, "UISettings should have 2 methods");
            Assert.AreEqual(6, typeVM.Events.Count, "UISettings should have 6 events");
            Assert.AreEqual(1, typeVM.Constructors.Count, "UISettings should have 1 constructor");
            Assert.AreEqual(24, typeVM.TotalMembers, "UISettings should have 24 total members");

            // Other type properties
            Assert.IsFalse(typeVM.IsVirtual, "UISettings should not be virtual");
            Assert.IsFalse(typeVM.IsFamily, "UISettings should not be family/protected");
            Assert.IsFalse(typeVM.IsAssembly, "UISettings should not be assembly/internal");
            Assert.IsFalse(typeVM.IsInternal, "UISettings should not be internal");
            Assert.AreEqual(MemberKind.Type, typeVM.MemberKind, "UISettings's MemberKind should be Type");
        }

        [TestMethod]
        public void SearchByGuid_ReturnsMatchingType()
        {
            Manager.Settings.MemberKind = MemberKind.Type;
            var searchExpression = new SearchExpression { RawValue = "Guid:{5b38e929-a086-46a7-a678-439135822bcf}" };

            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.AreEqual(1, members.Count,
                $"Expected to find 1 result for Guid search. Found: {members.Count} - {string.Join(", ", members.OfType<TypeViewModel>().Select(t => t.FullName))}");

            var typeVM = members[0] as TypeViewModel;
            Assert.IsNotNull(typeVM, "Expected result to be a TypeViewModel");
            Assert.AreEqual("BackgroundTaskCompletedEventHandler", typeVM.Name);
        }
    }
}
