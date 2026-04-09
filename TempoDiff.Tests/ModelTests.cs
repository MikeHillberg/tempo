using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NuGet.Versioning;
using Tempo;

namespace Tempo.Tests
{
    [TestClass]
    [DoNotParallelize]
    public class ModelTests
    {
        private static readonly string CachePath = Path.Combine(Path.GetTempPath(), "TempoTests.Cache");
        public static string SampleNupkgPath => Path.Combine(CachePath, "Microsoft.UI.Xaml", "2.8.7", "Microsoft.UI.Xaml.nupkg");


        [AssemblyInitialize]
        public static void AssemblyInitialize(TestContext context)
        {
            DesktopManager2.Initialize(wpfApp: false, packagesCachePath: CachePath);

            // Download Microsoft.UI.Xaml 2.8.7 if not already cached
            if (!File.Exists(SampleNupkgPath))
            {
                var versionRange = new VersionRange(
                    minVersion: new NuGetVersion("2.8.7"),
                    includeMinVersion: true,
                    maxVersion: new NuGetVersion("2.8.7"),
                    includeMaxVersion: true);

                var downloadTask = DesktopManager2.DownloadLatestPackageFromNugetToDirectory(
                    "Microsoft.UI.Xaml",
                    task: null,
                    prereleaseTag: null,
                    versionRange: versionRange);
                downloadTask.Wait();

                Assert.IsTrue(File.Exists(SampleNupkgPath), $"Failed to download Microsoft.UI.Xaml 2.8.7 to {SampleNupkgPath}");
            }
        }

        /// <summary>
        /// Helper method to load the Microsoft.UI.Xaml type set and set it as Manager.CurrentTypeSet.
        /// Call this from ClassInitialize in test classes that need to search against loaded types.
        /// </summary>
        public static void LoadWinUI2TypeSet(string typeSetName)
        {
            var typeSet = new MRTypeSet(typeSetName, usesWinRTProjections: false);
            DesktopManager2.LoadTypeSetMiddleweightReflection(
                typeSet,
                new[] { SampleNupkgPath });

            Assert.IsNotNull(typeSet.Types, "Failed to load types from nupkg");
            Assert.IsTrue(typeSet.Types.Count > 0, "No types loaded from nupkg");

            // Set as current type set for Manager.GetMembers
            Manager.CurrentTypeSet = typeSet;
        }

        /// <summary>
        /// Resets Manager state for test isolation. Call this from TestInitialize in test classes.
        /// Resets Settings to default and RecalculateIteration to 0.
        /// </summary>
        public static void ResetManagerState()
        {
            // Reset settings to default before each test
            Manager.Settings = new Settings();

            // Reset RecalculateIteration to 0 so tests can use iteration: 0
            // (other tests like TempDiffTests may have incremented this)
            Manager.RecalculateIteration = 0;
        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            LoadWinUI2TypeSet("ModelTestsTypeSet");
        }

        [TestInitialize]
        public void TestInitialize()
        {
            ResetManagerState();
        }

        [TestMethod]
        public void GetMembers_ReturnsMembers()
        {
            var searchExpression = new SearchExpression { RawValue = "Button" };
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.IsTrue(members.Count > 0, "Expected to find members matching 'Button'");
        }

        [TestMethod]
        public void ValidateTypeVM()
        {
            Manager.Settings.MemberKind = MemberKind.Type;
            var searchExpression = new SearchExpression { RawValue = "^ItemsRepeater$" };

            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.AreEqual(1, members.Count, $"Expected to find 1 result for ^ItemsRepeater$. Found: {members.Count} - {string.Join(", ", members.OfType<TypeViewModel>().Select(t => t.FullName))}");

            var typeVM = members[0] as TypeViewModel;
            Assert.IsNotNull(typeVM, "Expected result to be a TypeViewModel");

            // Validate TypeViewModel properties - Name and Namespace
            Assert.AreEqual("ItemsRepeater", typeVM.Name);
            Assert.AreEqual("Microsoft.UI.Xaml.Controls", typeVM.Namespace);
            Assert.AreEqual("Microsoft.UI.Xaml.Controls.ItemsRepeater", typeVM.FullName);

            // Type kind properties
            Assert.IsTrue(typeVM.IsClass, "ItemsRepeater should be a class");
            Assert.IsFalse(typeVM.IsInterface, "ItemsRepeater should not be an interface");
            Assert.IsFalse(typeVM.IsEnum, "ItemsRepeater should not be an enum");
            Assert.IsFalse(typeVM.IsValueType, "ItemsRepeater should not be a value type");
            Assert.IsFalse(typeVM.IsStruct, "ItemsRepeater should not be a struct");
            Assert.IsFalse(typeVM.IsDelegate, "ItemsRepeater should not be a delegate");

            // Type modifier properties
            Assert.IsFalse(typeVM.IsSealed, "ItemsRepeater should not be sealed");
            Assert.IsFalse(typeVM.IsAbstract, "ItemsRepeater should not be abstract");
            Assert.IsFalse(typeVM.IsStatic, "ItemsRepeater should not be static");
            Assert.IsTrue(typeVM.IsPublic, "ItemsRepeater should be public");
            Assert.IsFalse(typeVM.IsGenericTypeDefinition, "ItemsRepeater should not be a generic type definition");

            // Base type and interfaces
            Assert.IsNotNull(typeVM.BaseType, "ItemsRepeater should have a base type");
            Assert.AreEqual("FrameworkElement", typeVM.BaseType.Name, "ItemsRepeater's base type should be FrameworkElement");
            Assert.IsNotNull(typeVM.Interfaces, "ItemsRepeater should have interfaces collection");

            // Members - verify exact counts
            Assert.AreEqual(13, typeVM.Properties.Count, "ItemsRepeater should have 13 properties");
            Assert.AreEqual(3, typeVM.Methods.Count, "ItemsRepeater should have 3 methods");
            Assert.AreEqual(3, typeVM.Events.Count, "ItemsRepeater should have 3 events");
            Assert.AreEqual(1, typeVM.Constructors.Count, "ItemsRepeater should have 1 constructor");
            Assert.AreEqual(20, typeVM.TotalMembers, "ItemsRepeater should have 20 total members");

            // Other type properties
            Assert.IsFalse(typeVM.IsVirtual, "ItemsRepeater should not be virtual");
            Assert.IsFalse(typeVM.IsFamily, "ItemsRepeater should not be family/protected");
            Assert.IsFalse(typeVM.IsAssembly, "ItemsRepeater should not be assembly/internal");
            Assert.IsFalse(typeVM.IsInternal, "ItemsRepeater should not be internal");
            Assert.AreEqual(MemberKind.Type, typeVM.MemberKind, "ItemsRepeater's MemberKind should be Type");
        }
    }
}
