using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Versioning;
using Tempo;

namespace Tempo.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class WebView2TypesTests
    {
        private static readonly string CachePath = Path.Combine(Path.GetTempPath(), "TempoTests.Cache");
        private static readonly string WebView2Version = "1.0.3908-prerelease";
        private static readonly string WebView2NupkgPath = Path.Combine(CachePath, "Microsoft.Web.WebView2", WebView2Version, "Microsoft.Web.WebView2.nupkg");
        private static TypeSet _webView2Types;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Ensure DesktopManager2 is initialized
            DesktopManager2.Initialize(wpfApp: false, packagesCachePath: CachePath);

            var versionRange = new VersionRange(
                minVersion: new NuGetVersion(WebView2Version),
                includeMinVersion: true,
                maxVersion: new NuGetVersion(WebView2Version),
                includeMaxVersion: true);

            // Download and load Microsoft.Web.WebView2 with dependencies (uses cached copy if available)
            _webView2Types = DesktopManager2.LoadNugetHelper(
                typeSetName: "WebView2TestsTypeSet",
                cacheFolderName: CachePath,
                useWinRTProjections: false,
                packageName: "Microsoft.Web.WebView2",
                task: null,
                prereleasePrefix: null,
                loadDependencies: true,
                versionRange: versionRange);

            Assert.IsTrue(File.Exists(WebView2NupkgPath), $"Failed to download Microsoft.Web.WebView2 {WebView2Version} to {WebView2NupkgPath}");
            Assert.IsNotNull(_webView2Types, "Failed to load WebView2");
            Assert.IsNotNull(_webView2Types.Types, "Failed to load types from WebView2 nupkg");
            Assert.AreEqual(457, _webView2Types.Types.Count, $"Expected to load 457 types from WebView2, got {_webView2Types.Types.Count}");

            Manager.CurrentTypeSet = _webView2Types;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            ModelTests.ResetManagerState();

            // Set Manager.CurrentTypeSet (another test class may have changed it)
            Assert.IsNotNull(_webView2Types, "_webView2Types is null! ClassInitialize may not have run.");
            Assert.IsNotNull(_webView2Types.Types, "_webView2Types.Types is null!");
            Manager.CurrentTypeSet = _webView2Types;
        }

        [TestMethod]
        public void LoadWebView2Types_ReturnsSomething()
        {
            Assert.IsNotNull(_webView2Types);
            Assert.AreEqual(457, _webView2Types.Types.Count, $"Expected to load 457 types from WebView2, got {_webView2Types.Types.Count}");
        }

        [TestMethod]
        public void ValidateCoreWebView2EnvironmentOptionsTypeVM()
        {
            Manager.Settings.MemberKind = MemberKind.Type;
            var searchExpression = new SearchExpression { RawValue = "^CoreWebView2EnvironmentOptions$" };

            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.AreEqual(1, members.Count, $"Expected to find 1 result for ^CoreWebView2EnvironmentOptions$. Found: {members.Count} - {string.Join(", ", members.OfType<TypeViewModel>().Select(t => t.FullName))}");

            var typeVM = members[0] as TypeViewModel;
            Assert.IsNotNull(typeVM, "Expected result to be a TypeViewModel");

            // Validate TypeViewModel properties - Name and Namespace
            Assert.AreEqual("CoreWebView2EnvironmentOptions", typeVM.Name);
            Assert.AreEqual("Microsoft.Web.WebView2.Core", typeVM.Namespace);
            Assert.AreEqual("Microsoft.Web.WebView2.Core.CoreWebView2EnvironmentOptions", typeVM.FullName);

            // Type kind properties
            Assert.IsTrue(typeVM.IsClass, "CoreWebView2EnvironmentOptions should be a class");
            Assert.IsFalse(typeVM.IsInterface, "CoreWebView2EnvironmentOptions should not be an interface");
            Assert.IsFalse(typeVM.IsEnum, "CoreWebView2EnvironmentOptions should not be an enum");
            Assert.IsFalse(typeVM.IsValueType, "CoreWebView2EnvironmentOptions should not be a value type");
            Assert.IsFalse(typeVM.IsStruct, "CoreWebView2EnvironmentOptions should not be a struct");
            Assert.IsFalse(typeVM.IsDelegate, "CoreWebView2EnvironmentOptions should not be a delegate");

            // Type modifier properties
            Assert.IsTrue(typeVM.IsSealed, "CoreWebView2EnvironmentOptions should be sealed");
            Assert.IsFalse(typeVM.IsAbstract, "CoreWebView2EnvironmentOptions should not be abstract");
            Assert.IsFalse(typeVM.IsStatic, "CoreWebView2EnvironmentOptions should not be static");
            Assert.IsTrue(typeVM.IsPublic, "CoreWebView2EnvironmentOptions should be public");
            Assert.IsFalse(typeVM.IsGenericTypeDefinition, "CoreWebView2EnvironmentOptions should not be a generic type definition");

            // Members - verify exact counts
            Assert.AreEqual(12, typeVM.Properties.Count, $"CoreWebView2EnvironmentOptions should have 12 properties, got {typeVM.Properties.Count}");
            Assert.AreEqual(0, typeVM.Methods.Count, $"CoreWebView2EnvironmentOptions should have 0 methods, got {typeVM.Methods.Count}");
            Assert.AreEqual(0, typeVM.Events.Count, $"CoreWebView2EnvironmentOptions should have 0 events, got {typeVM.Events.Count}");
            Assert.AreEqual(13, typeVM.TotalMembers, $"CoreWebView2EnvironmentOptions should have 13 total members, got {typeVM.TotalMembers}");

            // Other type properties
            Assert.IsFalse(typeVM.IsVirtual, "CoreWebView2EnvironmentOptions should not be virtual");
            Assert.IsFalse(typeVM.IsFamily, "CoreWebView2EnvironmentOptions should not be family/protected");
            Assert.IsFalse(typeVM.IsAssembly, "CoreWebView2EnvironmentOptions should not be assembly/internal");
            Assert.IsFalse(typeVM.IsInternal, "CoreWebView2EnvironmentOptions should not be internal");
            Assert.AreEqual(MemberKind.Type, typeVM.MemberKind, "CoreWebView2EnvironmentOptions's MemberKind should be Type");
        }
    }
}
