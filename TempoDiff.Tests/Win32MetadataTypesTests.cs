using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Versioning;
using Tempo;

namespace Tempo.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class Win32MetadataTypesTests
    {
        private static readonly string CachePath = Path.Combine(Path.GetTempPath(), "TempoTests.Cache");
        private static readonly string Win32MetadataVersion = "70.0.11-preview";
        private static readonly string Win32MetadataNupkgPath = Path.Combine(CachePath, "Microsoft.Windows.SDK.Win32Metadata", Win32MetadataVersion, "Microsoft.Windows.SDK.Win32Metadata.nupkg");
        private static TypeSet _win32MetadataTypes;

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            // Ensure DesktopManager2 is initialized
            DesktopManager2.Initialize(wpfApp: false, packagesCachePath: CachePath);

            var versionRange = new VersionRange(
                minVersion: new NuGetVersion(Win32MetadataVersion),
                includeMinVersion: true,
                maxVersion: new NuGetVersion(Win32MetadataVersion),
                includeMaxVersion: true);

            // Download and load Microsoft.Windows.SDK.Win32Metadata with dependencies (uses cached copy if available)
            _win32MetadataTypes = DesktopManager2.LoadNugetHelper(
                typeSetName: "Win32MetadataTestsTypeSet",
                cacheFolderName: CachePath,
                useWinRTProjections: false,
                packageName: "Microsoft.Windows.SDK.Win32Metadata",
                task: null,
                prereleasePrefix: null,
                loadDependencies: true,
                versionRange: versionRange);

            Assert.IsTrue(File.Exists(Win32MetadataNupkgPath), $"Failed to download Microsoft.Windows.SDK.Win32Metadata {Win32MetadataVersion} to {Win32MetadataNupkgPath}");
            Assert.IsNotNull(_win32MetadataTypes, "Failed to load Win32Metadata");
            Assert.IsNotNull(_win32MetadataTypes.Types, "Failed to load types from Win32Metadata nupkg");
            Assert.IsTrue(_win32MetadataTypes.Types.Count > 0, "No types loaded from Win32Metadata nupkg");

            Manager.CurrentTypeSet = _win32MetadataTypes;
        }

        [TestInitialize]
        public void TestInitialize()
        {
            ModelTests.ResetManagerState();

            // Set Manager.CurrentTypeSet (another test class may have changed it)
            Manager.CurrentTypeSet = _win32MetadataTypes;
        }

        [TestMethod]
        public void LoadWin32MetadataTypes_ReturnsSomething()
        {
            Assert.IsNotNull(_win32MetadataTypes);
            Assert.AreEqual(37026, _win32MetadataTypes.Types.Count, $"Expected to load 37026 types from Win32Metadata, got {_win32MetadataTypes.Types.Count}");
        }

        [TestMethod]
        public void ValidateHistogramGridTypeVM()
        {
            Manager.Settings.MemberKind = MemberKind.Type;
            var searchExpression = new SearchExpression { RawValue = "^HistogramGrid$" };

            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.AreEqual(1, members.Count, $"Expected to find 1 result for ^HistogramGrid$. Found: {members.Count} - {string.Join(", ", members.OfType<TypeViewModel>().Select(t => t.FullName))}");

            var typeVM = members[0] as TypeViewModel;
            Assert.IsNotNull(typeVM, "Expected result to be a TypeViewModel");

            // Validate TypeViewModel properties - Name and Namespace
            Assert.AreEqual("HistogramGrid", typeVM.Name);
            Assert.AreEqual("Windows.Win32.Media.Streaming", typeVM.Namespace);
            Assert.AreEqual("Windows.Win32.Media.Streaming.HistogramGrid", typeVM.FullName);

            // Type kind properties
            Assert.IsFalse(typeVM.IsClass, "HistogramGrid should not be a class");
            Assert.IsFalse(typeVM.IsInterface, "HistogramGrid should not be an interface");
            Assert.IsFalse(typeVM.IsEnum, "HistogramGrid should not be an enum");
            Assert.IsTrue(typeVM.IsValueType, "HistogramGrid should be a value type");
            Assert.IsTrue(typeVM.IsStruct, "HistogramGrid should be a struct");
            Assert.IsFalse(typeVM.IsDelegate, "HistogramGrid should not be a delegate");

            // Type modifier properties
            Assert.IsTrue(typeVM.IsSealed, "HistogramGrid should be sealed");
            Assert.IsFalse(typeVM.IsAbstract, "HistogramGrid should not be abstract");
            Assert.IsFalse(typeVM.IsStatic, "HistogramGrid should not be static");
            Assert.IsTrue(typeVM.IsPublic, "HistogramGrid should be public");
            Assert.IsFalse(typeVM.IsGenericTypeDefinition, "HistogramGrid should not be a generic type definition");

            // Members - verify exact counts
            Assert.AreEqual(0, typeVM.Properties.Count, $"HistogramGrid should have 0 properties, got {typeVM.Properties.Count}");
            Assert.AreEqual(0, typeVM.Methods.Count, $"HistogramGrid should have 0 methods, got {typeVM.Methods.Count}");
            Assert.AreEqual(0, typeVM.Events.Count, $"HistogramGrid should have 0 events, got {typeVM.Events.Count}");
            Assert.AreEqual(3, typeVM.TotalMembers, $"HistogramGrid should have 3 total members, got {typeVM.TotalMembers}");

            // Other type properties
            Assert.IsFalse(typeVM.IsVirtual, "HistogramGrid should not be virtual");
            Assert.IsFalse(typeVM.IsFamily, "HistogramGrid should not be family/protected");
            Assert.IsFalse(typeVM.IsAssembly, "HistogramGrid should not be assembly/internal");
            Assert.IsFalse(typeVM.IsInternal, "HistogramGrid should not be internal");
            Assert.AreEqual(MemberKind.Type, typeVM.MemberKind, "HistogramGrid's MemberKind should be Type");
        }
    }
}
