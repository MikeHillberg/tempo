using System.Diagnostics;
using System.IO;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
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

        [TestMethod]
        public void TypeSetAssemblies_ArePopulated()
        {
            Assert.IsNotNull(_winAppSdkTypes.Assemblies);
            Assert.AreEqual(34, _winAppSdkTypes.Assemblies.Count, $"TypeSet should have 34 assemblies loaded, got {_winAppSdkTypes.Assemblies.Count}: {string.Join(", ", _winAppSdkTypes.Assemblies.Select(a => a.Name))}");

            // All assemblies should have names
            foreach (var asm in _winAppSdkTypes.Assemblies)
            {
                Assert.IsFalse(string.IsNullOrEmpty(asm.Name), "Assembly should have a name");
            }

            // Should be sorted by name
            var names = _winAppSdkTypes.Assemblies.Select(a => a.Name).ToList();
            CollectionAssert.AreEqual(names.OrderBy(n => n).ToList(), names, "Assemblies should be sorted by name");
        }

        [TestMethod]
        public void TypeSetAssemblies_ContainWinAppSdkAssembly()
        {
            // The WinAppSDK nupkg contains Microsoft.UI.Xaml among others
            var uiXaml = _winAppSdkTypes.Assemblies.FirstOrDefault(a => a.Name == "Microsoft.UI.Xaml");
            Assert.IsNotNull(uiXaml, $"Expected Microsoft.UI.Xaml assembly. Found: {string.Join(", ", _winAppSdkTypes.Assemblies.Select(a => a.Name))}");
            Assert.IsNotNull(uiXaml.Version);
        }

        [TestMethod]
        public void TypeViewModel_AssemblyProperty_IsPopulated()
        {
            // Get a known type and verify its Assembly property
            var type = _winAppSdkTypes.Types.First(t => t.FullName == "Microsoft.UI.Xaml.Controls.ItemsPresenter");
            var asm = type.Assembly;

            Assert.IsNotNull(asm, "TypeViewModel.Assembly should not be null");
            Assert.IsFalse(string.IsNullOrEmpty(asm.Name), "Assembly name should not be empty");
            Assert.IsNotNull(asm.Version, "Assembly version should not be null");
        }

        [TestMethod]
        public void AssemblyViewModel_ReferencedAssemblies_ArePopulated()
        {
            // Find an assembly that has referenced assemblies
            var asmWithRefs = _winAppSdkTypes.Assemblies.FirstOrDefault(a => a.ReferencedAssemblies.Count > 0);
            Assert.IsNotNull(asmWithRefs, $"At least one assembly should have references. Assembly count: {_winAppSdkTypes.Assemblies.Count}");

            foreach (var r in asmWithRefs.ReferencedAssemblies)
            {
                Assert.IsFalse(string.IsNullOrEmpty(r.Name), "Referenced assembly should have a name");
            }
        }

        [TestMethod]
        public void AssemblyViewModel_CustomAttributes_ArePopulated()
        {
            // Not all assemblies have custom attributes (WinRT WinMDs often don't).
            // Just verify the property is accessible without throwing.
            foreach (var asm in _winAppSdkTypes.Assemblies)
            {
                var attrs = asm.CustomAttributes;
                Assert.IsNotNull(attrs, $"CustomAttributes should not be null for {asm.Name}");
            }
        }



        // ===== Member ViewModel Tests =====

        [TestMethod]
        public void ValidatePropertyViewModel()
        {
            var type = _winAppSdkTypes.Types.First(t => t.FullName == "Microsoft.UI.Xaml.Controls.Button");
            Assert.AreEqual(2, type.Properties.Count, $"Button should have exactly 2 direct properties, got {type.Properties.Count}");

            var flyoutProp = type.Properties.First(p => p.Name == "Flyout");
            Assert.AreEqual("Flyout", flyoutProp.Name);
            Assert.AreEqual(MemberKind.Property, flyoutProp.MemberKind);
            Assert.IsTrue(flyoutProp.IsPublic);
            Assert.IsFalse(flyoutProp.IsStatic);
            Assert.IsTrue(flyoutProp.CanRead);
            Assert.IsTrue(flyoutProp.CanWrite);
            Assert.AreEqual("FlyoutBase", flyoutProp.PropertyType.Name);
            Assert.AreEqual("Button", flyoutProp.DeclaringType.Name);

            var dpProp = type.Properties.First(p => p.Name == "FlyoutProperty");
            Assert.AreEqual("DependencyProperty", dpProp.PropertyType.Name);
            Assert.IsTrue(dpProp.CanRead);
            Assert.IsFalse(dpProp.CanWrite);
        }

        [TestMethod]
        public void ValidateMethodViewModel()
        {
            var type = _winAppSdkTypes.Types.First(t => t.FullName == "Microsoft.UI.Xaml.Controls.StackPanel");
            var methods = type.Methods;
            Assert.AreEqual(3, methods.Count, $"StackPanel should have exactly 3 direct methods, got {methods.Count}");

            var method = methods.First(m => m.Name == "GetInsertionIndexes");
            Assert.AreEqual("GetInsertionIndexes", method.Name);
            Assert.AreEqual(MemberKind.Method, method.MemberKind);
            Assert.AreEqual("Void", method.ReturnType.Name);
            Assert.AreEqual(3, method.Parameters.Count);
            Assert.IsTrue(method.IsPublic);
            Assert.IsFalse(method.IsStatic);
            Assert.AreEqual("StackPanel", method.DeclaringType.Name);
        }

        [TestMethod]
        public void ValidateMethodParameters()
        {
            var type = _winAppSdkTypes.Types.First(t => t.FullName == "Microsoft.UI.Xaml.Controls.StackPanel");
            var method = type.Methods.First(m => m.Name == "GetInsertionIndexes");

            Assert.AreEqual(3, method.Parameters.Count);
            Assert.AreEqual("position", method.Parameters[0].Name);
            Assert.AreEqual("Point", method.Parameters[0].ParameterType.Name);
            Assert.AreEqual("first", method.Parameters[1].Name);
            Assert.AreEqual("second", method.Parameters[2].Name);
        }

        [TestMethod]
        public void ValidateEventViewModel()
        {
            var type = _winAppSdkTypes.Types.First(t => t.FullName == "Microsoft.UI.Xaml.Input.AccessKeyManager");
            Assert.AreEqual(1, type.Events.Count, $"AccessKeyManager should have exactly 1 event, got {type.Events.Count}");

            var ev = type.Events[0];
            Assert.AreEqual("IsDisplayModeEnabledChanged", ev.Name);
            Assert.AreEqual(MemberKind.Event, ev.MemberKind);
            Assert.AreEqual("TypedEventHandler`2", ev.EventHandlerType.Name);
        }

        [TestMethod]
        public void ValidateConstructorViewModel()
        {
            var type = _winAppSdkTypes.Types.First(t => t.FullName == "Microsoft.UI.Xaml.Controls.Button");
            Assert.AreEqual(1, type.Constructors.Count, $"Button should have exactly 1 constructor, got {type.Constructors.Count}");

            var ctor = type.Constructors[0];
            Assert.AreEqual(MemberKind.Constructor, ctor.MemberKind);
            Assert.AreEqual("Button", ctor.DeclaringType.Name);
            Assert.AreEqual(0, ctor.Parameters.Count);
        }

        [TestMethod]
        public void ValidateButtonHasNoDirectMethodsOrEvents()
        {
            // Button inherits everything — it has 0 direct methods and 0 direct events
            var type = _winAppSdkTypes.Types.First(t => t.FullName == "Microsoft.UI.Xaml.Controls.Button");
            Assert.AreEqual(0, type.Methods.Count, $"Button should have 0 direct methods, got {type.Methods.Count}");
            Assert.AreEqual(0, type.Events.Count, $"Button should have 0 direct events, got {type.Events.Count}");
        }

        // ===== ModifierCodeString / TypeKind Tests =====

        [TestMethod]
        public void ValidateModifierCodeString_SealedClass()
        {
            var type = _winAppSdkTypes.Types.First(t => t.FullName == "Microsoft.UI.Xaml.Controls.ItemsPresenter");
            Assert.AreEqual("public", type.ModifierCodeString);
            Assert.AreEqual(TypeKind.Class, type.TypeKind);
            Assert.IsTrue(type.IsSealed);
        }

        [TestMethod]
        public void ValidateModifierCodeString_Interface()
        {
            var type = _winAppSdkTypes.Types.First(t => t.FullName == "Microsoft.UI.Xaml.Controls.IAnimatedVisual");
            Assert.AreEqual(TypeKind.Interface, type.TypeKind);
            Assert.AreEqual("public abstract", type.ModifierCodeString);
        }

        [TestMethod]
        public void ValidateModifierCodeString_Enum()
        {
            var type = _winAppSdkTypes.Types.First(t => t.FullName == "Microsoft.UI.Xaml.Visibility");
            Assert.AreEqual(TypeKind.Enum, type.TypeKind);
            Assert.AreEqual("public", type.ModifierCodeString);
        }

        [TestMethod]
        public void ValidateModifierCodeString_Struct()
        {
            var type = _winAppSdkTypes.Types.First(t => t.FullName == "Microsoft.Windows.Security.AccessControl.AccessControlContract");
            Assert.AreEqual(TypeKind.Struct, type.TypeKind);
        }

        [TestMethod]
        public void ValidateModifierCodeString_Delegate()
        {
            var type = _winAppSdkTypes.Types.First(t => t.FullName == "Microsoft.UI.Xaml.Printing.AddPagesEventHandler");
            Assert.IsTrue(type.IsDelegate);
        }

        // ===== Export Tests =====

        [TestMethod]
        public void ExportCsv_SingleType()
        {
            var type = _winAppSdkTypes.Types.First(t => t.FullName == "Microsoft.UI.Xaml.Controls.Button");
            var csv = CopyExport.GetItemsAsCsv(new[] { type }, forceAllTypes: true);

            var expected =
                "Declaring type,Name,Member Kind,Namespace,Base type,Return type,Contract,Contract version,Version,\r\n"
                + "\"Button\",,\"Class\",\"Microsoft.UI.Xaml.Controls\",\"ButtonBase\",,\"WinUIContract\",\" 1\",\"Microsoft.WindowsAppSDK.WinUI.nupkg\",\r\n";
            Assert.AreEqual(expected, csv);
        }

        [TestMethod]
        public void ExportCsv_MultipleTypes()
        {
            var types = _winAppSdkTypes.Types
                .Where(t => t.Namespace == "Microsoft.UI.Xaml.Controls" && t.IsClass)
                .Take(5)
                .Cast<BaseViewModel>();

            var csv = CopyExport.GetItemsAsCsv(types, forceAllTypes: true);
            var lines = csv.Split("\r\n", System.StringSplitOptions.RemoveEmptyEntries);
            Assert.AreEqual(6, lines.Length, $"Expected header + 5 data rows, got {lines.Length}: {string.Join(" | ", lines)}");

            // Verify header
            Assert.AreEqual("Declaring type,Name,Member Kind,Namespace,Base type,Return type,Contract,Contract version,Version,", lines[0]);

            // Verify first data row
            Assert.AreEqual(
                "\"AnchorRequestedEventArgs\",,\"Class\",\"Microsoft.UI.Xaml.Controls\",,,\"WinUIContract\",\" 1\",\"Microsoft.WindowsAppSDK.WinUI.nupkg\",",
                lines[1]);
        }

        [TestMethod]
        public void ExportCsv_ExportHelperBasics()
        {
            var helper = new ExportHelper();
            helper.AddKey("Name");
            helper.AddKey("Value");

            helper.CreateNewRow();
            helper.AppendCell("Name", "Test1");
            helper.AppendCell("Value", "123");

            helper.CreateNewRow();
            helper.AppendCell("Name", "Test2");
            helper.AppendCell("Value", "456");

            var csv = helper.ToString();
            var expected = "Name,Value,\r\n\"Test1\",\"123\",\r\n\"Test2\",\"456\",\r\n";
            Assert.AreEqual(expected, csv);
        }

        [TestMethod]
        public void GetMembers_WithAqsMethodFilter_ExcludesTypesWithNoMatchingMembers()
        {
            ModelTests.ResetManagerState();
            Manager.CurrentTypeSet = _winAppSdkTypes;
            Manager.Settings.MemberKind = MemberKind.Any;
            var searchExpression = new SearchExpression { RawValue = "IsMethod:True Parameters.Count:>0" };
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.IsTrue(members.Count > 0, "Expected results for 'IsMethod:True Parameters.Count:>0'");

            // The key invariant: every type in results should have at least one method with parameters after it
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i] is TypeViewModel type)
                {
                    var hasChildMethod = false;
                    for (int j = i + 1; j < members.Count && !(members[j] is TypeViewModel); j++)
                    {
                        if (members[j] is MethodViewModel method && method.Parameters.Count > 0)
                        {
                            hasChildMethod = true;
                            break;
                        }
                    }
                    Assert.IsTrue(hasChildMethod,
                        $"Type '{type.FullName}' in results has no matching methods with parameters after it");
                }
            }
        }

        [TestMethod]
        public void ValidateGenericConstraints()
        {
            // Check that GenericParameterAttributes and GenericConstraints are accessible
            // WinRT types may not have many generic types, so iterate through what we have
            var genericTypes = _winAppSdkTypes.Types
                .Where(t => t.IsGenericType)
                .ToList();

            // Even if there are no generic types, verify the properties don't throw
            foreach (var gt in genericTypes)
            {
                var args = gt.GetGenericArguments();
                Assert.IsNotNull(args);
                foreach (var arg in args)
                {
                    var attrs = arg.GenericParameterAttributes;
                    var constraints = arg.GenericConstraints;
                    Assert.IsNotNull(constraints);
                }
            }

            // Verify GenericParameterConstraintClauses property is accessible on all types
            foreach (var type in _winAppSdkTypes.Types.Take(100))
            {
                var clauses = type.GenericParameterConstraintClauses;
                Assert.IsNotNull(clauses);
            }
        }

        [TestMethod]
        public void GetMembers_WithIsMethodOnly_ExcludesOrphanTypes()
        {
            ModelTests.ResetManagerState();
            Manager.CurrentTypeSet = _winAppSdkTypes;
            Manager.Settings.MemberKind = MemberKind.Any;
            var searchExpression = new SearchExpression { RawValue = "IsMethod:True" };
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.IsTrue(members.Count > 0);

            // Every type should have at least one method after it
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i] is TypeViewModel type)
                {
                    var hasMethod = false;
                    for (int j = i + 1; j < members.Count && !(members[j] is TypeViewModel); j++)
                    {
                        if (members[j] is MethodViewModel) { hasMethod = true; break; }
                    }
                    Assert.IsTrue(hasMethod,
                        $"Type '{type.FullName}' has no matching methods after it");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithIsPropertyAndCanWrite_ReturnsWritableProperties()
        {
            ModelTests.ResetManagerState();
            Manager.CurrentTypeSet = _winAppSdkTypes;
            Manager.Settings.MemberKind = MemberKind.Any;
            var searchExpression = new SearchExpression { RawValue = "IsProperty:True CanWrite:True" };
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.IsTrue(members.Count > 0, "Expected results for writable properties");

            // All non-type results should be writable properties
            foreach (var member in members)
            {
                if (member is PropertyViewModel prop)
                {
                    Assert.IsTrue(prop.CanWrite,
                        $"Property '{prop.DeclaringType.Name}.{prop.Name}' should be writable");
                }
            }
        }

#if !DEBUG // Release only because it's expensive
        [TestMethod]
#endif
        public void ExerciseAllViewModelProperties()
        {
            // Enumerate all types, members, and parameters, calling ToString on every
            // non-null property value. This flushes out crashes, asserts, and null refs
            // in lazy property getters across the entire type set.

            var errors = new System.Collections.Generic.List<string>();

            foreach (var type in _winAppSdkTypes.Types)
            {
                ExerciseProperties(type, $"Type:{type.FullName}", errors);

                foreach (var prop in type.Properties)
                {
                    ExerciseProperties(prop, $"Property:{type.Name}.{prop.Name}", errors);
                }

                foreach (var method in type.Methods)
                {
                    ExerciseProperties(method, $"Method:{type.Name}.{method.Name}", errors);

                    foreach (var param in method.Parameters)
                    {
                        ExerciseProperties(param, $"Parameter:{type.Name}.{method.Name}.{param.Name}", errors);
                    }
                }

                foreach (var ev in type.Events)
                {
                    ExerciseProperties(ev, $"Event:{type.Name}.{ev.Name}", errors);
                }

                foreach (var ctor in type.Constructors)
                {
                    ExerciseProperties(ctor, $"Constructor:{type.Name}", errors);

                    foreach (var param in ctor.Parameters)
                    {
                        ExerciseProperties(param, $"CtorParameter:{type.Name}.{param.Name}", errors);
                    }
                }
            }

            // Report unique error types for debugging
            var uniqueErrors = errors.GroupBy(e => e.Split(':')[1]?.Trim()).Select(g => $"{g.Key}: {g.Count()} occurrences").ToList();
            Assert.AreEqual(0, errors.Count,
                $"Found {errors.Count} errors ({uniqueErrors.Count} unique types):\n{string.Join("\n", uniqueErrors.Take(20))}\n\nFirst errors:\n{string.Join("\n", errors.Take(5))}");
        }

        static readonly HashSet<string> _skipProperties = new HashSet<string>
        {
            // ReturnedByAsync/ReferencedByAsync trigger async background work that crashes the test host
            "ReturnedByAsync",
            "ReferencedByAsync",

            // FieldsAsync can trigger background work
            "FieldsAsync",

            // ApiDescriptionAsync hits GitHub and gets rate-limited
            "ApiDescriptionAsync",
            "ApiDesignNotes"
        };

        static void ExerciseProperties(object vm, string context, System.Collections.Generic.List<string> errors)
        {
            foreach (var prop in vm.GetType().GetProperties(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                // Skip indexers
                if (prop.GetIndexParameters().Length > 0) continue;

                // Skip properties that trigger background work and crash the test host
                if (_skipProperties.Contains(prop.Name)) continue;

                var value = prop.GetValue(vm);
                value?.ToString();
            }
        }

//#if !DEBUG // Release only because it's expensive
        [TestMethod]
        [TestCategory("Expensive")]
        public void ValidateReferencedByAsync()
        {
            // First wait for ReturnedByCalculated to be done (triggered by OnTypesUpdated)
            var timeout = TimeSpan.FromSeconds(30);
            var sw = Stopwatch.StartNew();
            while (!_winAppSdkTypes.ReturnedByCalculated && sw.Elapsed < timeout)
            {
                Thread.Sleep(100);
            }
            Assert.IsTrue(_winAppSdkTypes.ReturnedByCalculated, "ReturnedBy should be calculated by now");

            // Set up a semaphore to wait for all ReferencedBy calculations to complete
            var semaphore = new ManualResetEventSlim(false);
            _winAppSdkTypes.AllReferencedByCalculationsCompleted += (s, e) =>
            {
                semaphore.Set();
            };

            // Trigger ReferencedByAsync on all types (this starts async calculations)
            foreach (var type in _winAppSdkTypes.Types)
            {
                _ = type.ReferencedByAsync;
            }

            // If nothing was pending (already calculated or no types), we're done
            if (_winAppSdkTypes.PendingReferencedByCount > 0)
            {
                var completed = semaphore.Wait(timeout);
                Assert.IsTrue(completed,
                    $"Timed out waiting for ReferencedBy calculations. Pending: {_winAppSdkTypes.PendingReferencedByCount}");
            }

            // Now validate: check ReturnedByAsync counts for known types
            var totalWithReturnedBy = _winAppSdkTypes.Types
                .Count(t => t.ReturnedByAsync != null && t.ReturnedByAsync.Count > 0);
            Assert.AreEqual(1022, totalWithReturnedBy,
                $"Expected 1022 types with ReturnedBy > 0, got {totalWithReturnedBy}");

            var depProp = _winAppSdkTypes.Types.First(t => t.FullName == "Microsoft.UI.Xaml.DependencyProperty");
            Assert.AreEqual(1850, depProp.ReturnedByAsync.Count,
                $"DependencyProperty ReturnedBy count should be 1850, got {depProp.ReturnedByAsync.Count}");

            var uiElement = _winAppSdkTypes.Types.First(t => t.FullName == "Microsoft.UI.Xaml.UIElement");
            Assert.AreEqual(158, uiElement.ReturnedByAsync.Count,
                $"UIElement ReturnedBy count should be 158, got {uiElement.ReturnedByAsync.Count}");

            var frameworkElement = _winAppSdkTypes.Types.First(t => t.FullName == "Microsoft.UI.Xaml.FrameworkElement");
            Assert.AreEqual(70, frameworkElement.ReturnedByAsync.Count,
                $"FrameworkElement ReturnedBy count should be 70, got {frameworkElement.ReturnedByAsync.Count}");

            // Validate ReferencedByAsync
            var totalWithReferencedBy = _winAppSdkTypes.Types
                .Count(t => t.ReferencedByAsync != null && t.ReferencedByAsync.Count > 0);
            Assert.AreEqual(1, totalWithReferencedBy,
                $"Expected 1 type with ReferencedBy > 0, got {totalWithReferencedBy}");
        }
    }
}
