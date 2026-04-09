using System.IO;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Versioning;
using Tempo;

namespace Tempo.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class WinAppSdkTypesTests
    {
        private static readonly string CachePath = Path.Combine(Path.GetTempPath(), "TempoTests.Cache");
        private static readonly string WinAppSdkVersion = "1.8.260317003";
        private static readonly string WinAppSdkNupkgPath = Path.Combine(CachePath, "Microsoft.WindowsAppSDK", WinAppSdkVersion, "Microsoft.WindowsAppSDK.nupkg");
        private static TypeSet _winAppSdkTypes;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Ensure DesktopManager2 is initialized
            DesktopManager2.Initialize(wpfApp: false, packagesCachePath: CachePath);

            var versionRange = new VersionRange(
                minVersion: new NuGetVersion(WinAppSdkVersion),
                includeMinVersion: true,
                maxVersion: new NuGetVersion(WinAppSdkVersion),
                includeMaxVersion: true);

            // Download and load Microsoft.WindowsAppSDK with dependencies (uses cached copy if available)
            _winAppSdkTypes = DesktopManager2.LoadNugetHelper(
                typeSetName: "WinAppSdkTestsTypeSet",
                cacheFolderName: CachePath,
                useWinRTProjections: true,
                packageName: "Microsoft.WindowsAppSDK",
                task: null,
                prereleasePrefix: null,
                loadDependencies: true,
                versionRange: versionRange);

            Assert.IsTrue(File.Exists(WinAppSdkNupkgPath), $"Failed to download Microsoft.WindowsAppSDK {WinAppSdkVersion} to {WinAppSdkNupkgPath}");
            Assert.IsNotNull(_winAppSdkTypes, "Failed to load WinAppSDK");
            Assert.IsNotNull(_winAppSdkTypes.Types, "Failed to load types from WinAppSDK nupkg");
            Assert.IsTrue(_winAppSdkTypes.Types.Count > 0, "No types loaded from WinAppSDK nupkg");

            Manager.CurrentTypeSet = _winAppSdkTypes;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            ModelTests.ResetManagerState();

            // Set Manager.CurrentTypeSet (another test class may have changed it)
            Manager.CurrentTypeSet = _winAppSdkTypes;
        }

        [TestMethod]
        public void LoadWinAppSdkTypes_ReturnsSomething()
        {
            Assert.IsNotNull(_winAppSdkTypes);
            Assert.AreEqual(4345, _winAppSdkTypes.Types.Count, $"Expected to load 4345 types from WinAppSDK, got {_winAppSdkTypes.Types.Count}");
        }

        [TestMethod]
        public void ValidateItemsPresenterTypeVM()
        {
            Manager.Settings.MemberKind = MemberKind.Type;
            var searchExpression = new SearchExpression { RawValue = "^ItemsPresenter$" };

            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.AreEqual(1, members.Count, $"Expected to find 1 result for ^ItemsPresenter$. Found: {members.Count} - {string.Join(", ", members.OfType<TypeViewModel>().Select(t => t.FullName))}");

            var typeVM = members[0] as TypeViewModel;
            Assert.IsNotNull(typeVM, "Expected result to be a TypeViewModel");

            // Validate TypeViewModel properties - Name and Namespace
            Assert.AreEqual("ItemsPresenter", typeVM.Name);
            Assert.AreEqual("Microsoft.UI.Xaml.Controls", typeVM.Namespace);
            Assert.AreEqual("Microsoft.UI.Xaml.Controls.ItemsPresenter", typeVM.FullName);

            // Type kind properties
            Assert.IsTrue(typeVM.IsClass, "ItemsPresenter should be a class");
            Assert.IsFalse(typeVM.IsInterface, "ItemsPresenter should not be an interface");
            Assert.IsFalse(typeVM.IsEnum, "ItemsPresenter should not be an enum");
            Assert.IsFalse(typeVM.IsValueType, "ItemsPresenter should not be a value type");
            Assert.IsFalse(typeVM.IsStruct, "ItemsPresenter should not be a struct");
            Assert.IsFalse(typeVM.IsDelegate, "ItemsPresenter should not be a delegate");

            // Type modifier properties
            Assert.IsTrue(typeVM.IsSealed, "ItemsPresenter should be sealed");
            Assert.IsFalse(typeVM.IsAbstract, "ItemsPresenter should not be abstract");
            Assert.IsFalse(typeVM.IsStatic, "ItemsPresenter should not be static");
            Assert.IsTrue(typeVM.IsPublic, "ItemsPresenter should be public");
            Assert.IsFalse(typeVM.IsGenericTypeDefinition, "ItemsPresenter should not be a generic type definition");

            // Base type and interfaces
            Assert.IsNotNull(typeVM.Interfaces, "ItemsPresenter should have interfaces collection");

            // Members - verify exact counts
            Assert.AreEqual(16, typeVM.Properties.Count, $"ItemsPresenter should have 16 properties, got {typeVM.Properties.Count}");
            Assert.AreEqual(2, typeVM.Methods.Count, $"ItemsPresenter should have 2 methods, got {typeVM.Methods.Count}");
            Assert.AreEqual(2, typeVM.Events.Count, $"ItemsPresenter should have 2 events, got {typeVM.Events.Count}");
            Assert.AreEqual(324, typeVM.TotalMembers, $"ItemsPresenter should have 324 total members, got {typeVM.TotalMembers}");

            // Other type properties
            Assert.IsFalse(typeVM.IsVirtual, "ItemsPresenter should not be virtual");
            Assert.IsFalse(typeVM.IsFamily, "ItemsPresenter should not be family/protected");
            Assert.IsFalse(typeVM.IsAssembly, "ItemsPresenter should not be assembly/internal");
            Assert.IsFalse(typeVM.IsInternal, "ItemsPresenter should not be internal");
            Assert.AreEqual(MemberKind.Type, typeVM.MemberKind, "ItemsPresenter's MemberKind should be Type");
        }

        [TestMethod]
        public void ValidateAttributeTypeInfo_WrapCustomAttributes()
        {
            var types = _winAppSdkTypes.Types;

            // Get the BindableAttribute type and validate all of its custom attributes
            var bindableAttributeType = types.First(t => t.FullName == "Microsoft.UI.Xaml.Data.BindableAttribute");
            var bindableAttrs = AttributeTypeInfo.WrapCustomAttributes(bindableAttributeType.CustomAttributes).ToList();
            Assert.AreEqual(2, bindableAttrs.Count,
                $"BindableAttribute: expected 2, got {bindableAttrs.Count}: {string.Join(", ", bindableAttrs.Select(a => $"'{a.TypeName}'='{a.Properties}'"))}");
            var bindableAttrUsage = bindableAttrs.FirstOrDefault(a => a.TypeName == "[AttributeUsage]");
            Assert.IsNotNull(bindableAttrUsage,
                $"BindableAttribute: expected [AttributeUsage]. Found: {string.Join(", ", bindableAttrs.Select(a => $"'{a.TypeName}'"))}");
            Assert.AreEqual("(AttributeTargets) Class\r\nAllowMultiple=False", bindableAttrUsage.Properties);

            var bindableWebHostHidden = bindableAttrs.FirstOrDefault(a => a.TypeName == "[WebHostHidden]");
            Assert.IsNotNull(bindableWebHostHidden,
                $"BindableAttribute: expected [WebHostHidden]. Found: {string.Join(", ", bindableAttrs.Select(a => $"'{a.TypeName}'"))}");
            Assert.AreEqual("", bindableWebHostHidden.Properties);

            // Get the ContentPropertyAttribute type and validate all of its custom attributes
            var contentPropertyAttributeType = types.First(t => t.FullName == "Microsoft.UI.Xaml.Markup.ContentPropertyAttribute");
            var contentPropertyAttrs = AttributeTypeInfo.WrapCustomAttributes(contentPropertyAttributeType.CustomAttributes).ToList();
            Assert.AreEqual(2, contentPropertyAttrs.Count,
                $"ContentPropertyAttribute: expected 2, got {contentPropertyAttrs.Count}: {string.Join(", ", contentPropertyAttrs.Select(a => $"'{a.TypeName}'='{a.Properties}'"))}");
            var contentPropertyAttrUsage = contentPropertyAttrs.FirstOrDefault(a => a.TypeName == "[AttributeUsage]");
            Assert.IsNotNull(contentPropertyAttrUsage,
                $"ContentPropertyAttribute: expected [AttributeUsage]. Found: {string.Join(", ", contentPropertyAttrs.Select(a => $"'{a.TypeName}'"))}");
            Assert.AreEqual("(AttributeTargets) Class\r\nAllowMultiple=False", contentPropertyAttrUsage.Properties);
            var contentPropertyWebHostHidden = contentPropertyAttrs.FirstOrDefault(a => a.TypeName == "[WebHostHidden]");
            Assert.IsNotNull(contentPropertyWebHostHidden,
                $"ContentPropertyAttribute: expected [WebHostHidden]. Found: {string.Join(", ", contentPropertyAttrs.Select(a => $"'{a.TypeName}'"))}");
            Assert.AreEqual("", contentPropertyWebHostHidden.Properties);
        }

        [TestMethod]
        public void GetMembers_WithMixedPropertyAvailable()
        {
            // Some classes have a Guid property, some don't. This shouldn't return the ones that don't
            var searchExpression = new SearchExpression { RawValue = "Guid:..* IsClass:True" };

            var members = Manager.GetMembers(searchExpression, iteration: 0);
            Assert.IsNotNull(members);
            Assert.AreEqual(59, members.Count);

            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(!string.IsNullOrEmpty(typeVM.Guid), $"Type {typeVM.FullName} should have a Guid property with a value");
                }
            }
        }



    }
}
