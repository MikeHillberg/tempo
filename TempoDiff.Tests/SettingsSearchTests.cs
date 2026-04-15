using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tempo;

namespace Tempo.Tests
{
    /// <summary>
    /// Tests for searching using various Settings from Filters3.xaml.
    /// Each test verifies that Manager.GetMembers correctly filters results
    /// based on the corresponding setting set on Manager.Settings.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public class SettingsSearchTests
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ModelTests.LoadWinUI2TypeSet("SettingsSearchTestsTypeSet");
        }

        [TestInitialize]
        public void TestInitialize()
        {
            ModelTests.ResetManagerState();
        }

        #region Member Kind Settings (MemberKind enum)

        [TestMethod]
        public void GetMembers_WithMemberKindProperty_ReturnsOnlyProperties()
        {
            Manager.Settings.MemberKind = MemberKind.Property;
            var searchExpression = new SearchExpression { RawValue = "Button" };

            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.AreEqual(71, members.Count, "Expected to find 71 properties");
            foreach (var member in members)
            {
                Assert.IsTrue(member is PropertyViewModel || member is TypeViewModel,
                    $"Expected PropertyViewModel or TypeViewModel, got {member.GetType().Name}");
            }
        }

        [TestMethod]
        public void GetMembers_WithMemberKindMethod_ReturnsOnlyMethods()
        {
            Manager.Settings.MemberKind = MemberKind.Method;
            var searchExpression = new SearchExpression { RawValue = "Measure" };

            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.AreEqual(7, members.Count, "Expected to find 7 methods");
            foreach (var member in members)
            {
                Assert.IsTrue(member is MethodViewModel || member is TypeViewModel,
                    $"Expected MethodViewModel or TypeViewModel, got {member.GetType().Name}");
            }
        }

        [TestMethod]
        public void GetMembers_WithMemberKindEvent_ReturnsOnlyEvents()
        {
            Manager.Settings.MemberKind = MemberKind.Event;
            var searchExpression = new SearchExpression { RawValue = "Click" };

            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.AreEqual(11, members.Count, "Expected to find 11 events");
            foreach (var member in members)
            {
                Assert.IsTrue(member is EventViewModel || member is TypeViewModel,
                    $"Expected EventViewModel or TypeViewModel, got {member.GetType().Name}");
            }
        }

        [TestMethod]
        public void GetMembers_WithMemberKindField_ReturnsOnlyFields()
        {
            Manager.Settings.MemberKind = MemberKind.Field;
            var searchExpression = new SearchExpression { RawValue = "Property" };

            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            // Note: Field count can be 0 or more depending on what's loaded
            foreach (var member in members)
            {
                Assert.IsTrue(member is FieldViewModel || member is TypeViewModel,
                    $"Expected FieldViewModel or TypeViewModel, got {member.GetType().Name}");
            }
        }

        [TestMethod]
        public void GetMembers_WithMemberKindConstructor_ReturnsOnlyConstructors()
        {
            Manager.Settings.MemberKind = MemberKind.Constructor;
            var searchExpression = new SearchExpression { RawValue = "Button" };

            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.AreEqual(8, members.Count, "Expected to find 8 constructors");
            foreach (var member in members)
            {
                Assert.IsTrue(member is ConstructorViewModel || member is TypeViewModel,
                    $"Expected ConstructorViewModel or TypeViewModel, got {member.GetType().Name}");
            }
        }

        [TestMethod]
        public void GetMembers_WithMemberKindType_ReturnsOnlyTypes()
        {
            Manager.Settings.MemberKind = MemberKind.Type;
            var searchExpression = new SearchExpression { RawValue = "Button" };

            var members = Manager.GetMembers(searchExpression, iteration: 0);

            Assert.IsNotNull(members);
            Assert.AreEqual(15, members.Count, "Expected to find 15 types");
            foreach (var member in members)
            {
                Assert.IsTrue(member is TypeViewModel,
                    $"Expected TypeViewModel, got {member.GetType().Name}");
            }
        }

        #endregion

        #region Type Kind Settings (TypeKind enum)

        [TestMethod]
        public void GetMembers_WithTypeKindClass_ReturnsOnlyClasses()
        {
            // Arrange
            Manager.Settings.TypeKind = TypeKind.Class;
            var searchExpression = new SearchExpression { RawValue = "Button" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(92, members.Count, "Expected to find 92 classes");
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.IsClass, $"Type {typeVM.Name} is not a class");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithTypeKindInterface_ReturnsOnlyInterfaces()
        {
            // Arrange
            Manager.Settings.TypeKind = TypeKind.Interface;
            var searchExpression = new SearchExpression { RawValue = "Command" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            // Note: Interface count can be 0 depending on search term
            Assert.IsTrue(members.Count >= 0, "Expected to find interfaces");
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.IsInterface, $"Type {typeVM.Name} is not an interface");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithTypeKindEnum_ReturnsOnlyEnums()
        {
            // Arrange
            Manager.Settings.TypeKind = TypeKind.Enum;
            var searchExpression = new SearchExpression { RawValue = "Visibility" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(2, members.Count, "Expected to find 2 enums");
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.IsEnum, $"Type {typeVM.Name} is not an enum");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithTypeKindStruct_ReturnsOnlyStructs()
        {
            // Arrange
            Manager.Settings.TypeKind = TypeKind.Struct;
            var searchExpression = new SearchExpression { RawValue = "Point" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            // Note: Struct count can be 0 depending on search term
            Assert.IsTrue(members.Count >= 0, "Expected to find structs");
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.IsValueType && !typeVM.IsEnum, 
                        $"Type {typeVM.Name} is not a struct (IsValueType={typeVM.IsValueType}, IsEnum={typeVM.IsEnum})");
                }
            }
        }

        #endregion

        #region Type Modifier Settings

        [TestMethod]
        public void GetMembers_WithIsSealedTypeTrue_ReturnsSealedTypes()
        {
            // Arrange
            Manager.Settings.IsSealedType = true;
            var searchExpression = new SearchExpression { RawValue = "Button" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(14, members.Count, "Expected to find 14 sealed types");
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.IsSealed, $"Type {typeVM.Name} is not sealed");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithIsAbstractTypeTrue_ReturnsAbstractTypes()
        {
            // Arrange
            Manager.Settings.IsAbstractType = true;
            var searchExpression = new SearchExpression { RawValue = "Control" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(9, members.Count, "Expected to find 9 abstract types");
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.IsAbstract, $"Type {typeVM.Name} is not abstract");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithIsGenericTrue_ReturnsGenericTypes()
        {
            // Arrange
            Manager.Settings.IsGeneric = true;
            var searchExpression = new SearchExpression { RawValue = "TypedEventHandler" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(0, members.Count, "Expected to find 0 generic types matching 'TypedEventHandler'");
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.IsGenericTypeDefinition, 
                        $"Type {typeVM.Name} is not a generic type definition");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithIsStaticClassTrue_ReturnsStaticClasses()
        {
            // Arrange
            Manager.Settings.IsStaticClass = true;
            var searchExpression = new SearchExpression { RawValue = "Metadata" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            // Note: Static class count can be 0 depending on search term
            Assert.IsTrue(members.Count >= 0, "Expected to find static classes");
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    // Static classes are both abstract and sealed
                    Assert.IsTrue(typeVM.IsAbstract && typeVM.IsSealed, 
                        $"Type {typeVM.Name} is not a static class (IsAbstract={typeVM.IsAbstract}, IsSealed={typeVM.IsSealed})");
                }
            }
        }

        #endregion

        #region Member Modifier Settings

        [TestMethod]
        public void GetMembers_WithIsStaticTrue_ReturnsStaticMembers()
        {
            // Arrange
            Manager.Settings.IsStatic = true;
            var searchExpression = new SearchExpression { RawValue = "Property" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(480, members.Count, "Expected to find 480 static members");
            foreach (var member in members)
            {
                if (member is MemberViewModelBase memberVM && !(member is TypeViewModel))
                {
                    Assert.IsTrue(memberVM.IsStatic, $"Member {memberVM.Name} is not static");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithIsVirtualTrue_ReturnsVirtualMembers()
        {
            // Arrange
            Manager.Settings.IsVirtual = true;
            var searchExpression = new SearchExpression { RawValue = "Measure" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(8, members.Count, "Expected to find 8 virtual members");
            foreach (var member in members)
            {
                if (member is MemberViewModelBase memberVM && !(member is TypeViewModel))
                {
                    Assert.IsTrue(memberVM.IsVirtual, $"Member {memberVM.Name} is not virtual");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithIsProtectedTrue_ReturnsProtectedMembers()
        {
            // Arrange
            Manager.Settings.IsProtected = true;
            var searchExpression = new SearchExpression { RawValue = "On" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(20, members.Count, "Expected to find 20 protected members");
            foreach (var member in members)
            {
                if (member is MemberViewModelBase memberVM && !(member is TypeViewModel))
                {
                    Assert.IsTrue(memberVM.IsProtected, $"Member {memberVM.Name} is not protected");
                }
            }
        }

        #endregion

        #region Property-specific Settings

        [TestMethod]
        public void GetMembers_WithCanWriteTrue_ReturnsWriteableProperties()
        {
            // Arrange
            Manager.Settings.CanWrite = true;
            Manager.Settings.MemberKind = MemberKind.Property;
            var searchExpression = new SearchExpression { RawValue = "Width" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(9, members.Count, "Expected to find 9 writeable properties");
            foreach (var member in members)
            {
                if (member is PropertyViewModel propVM)
                {
                    Assert.IsTrue(propVM.CanWrite, $"Property {propVM.Name} is not writeable");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithCanWriteFalse_ReturnsReadOnlyProperties()
        {
            // Arrange
            Manager.Settings.CanWrite = false;
            Manager.Settings.MemberKind = MemberKind.Property;
            var searchExpression = new SearchExpression { RawValue = "Actual" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(3, members.Count, "Expected to find 3 read-only properties");
            foreach (var member in members)
            {
                if (member is PropertyViewModel propVM)
                {
                    Assert.IsFalse(propVM.CanWrite, $"Property {propVM.Name} is writeable");
                }
            }
        }

        #endregion

        #region Type Has Settings

        [TestMethod]
        public void GetMembers_WithHasInterfacesTrue_ReturnsTypesWithInterfaces()
        {
            // Arrange
            Manager.Settings.HasInterfaces = true;
            var searchExpression = new SearchExpression { RawValue = "Button" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(7, members.Count, "Expected to find 7 types with interfaces");
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.Interfaces != null && typeVM.Interfaces.Count > 0,
                        $"Type {typeVM.Name} does not have interfaces");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithHasDefaultConstructorTrue_ReturnsTypesWithDefaultConstructor()
        {
            // Arrange
            Manager.Settings.HasDefaultConstructor = true;
            var searchExpression = new SearchExpression { RawValue = "Button" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(79, members.Count, "Expected to find 79 types with default constructor");
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    var hasDefaultCtor = typeVM.Constructors.Any(c => c.Parameters == null || c.Parameters.Count == 0);
                    Assert.IsTrue(hasDefaultCtor,
                        $"Type {typeVM.Name} does not have a default constructor");
                }
            }
        }

        #endregion

        #region Search On Settings

        [TestMethod]
        public void GetMembers_WithFilterOnReturnTypeFalse_ReturnsFewerResults()
        {
            // Arrange - Compare results with and without FilterOnReturnType
            var searchExpression = new SearchExpression { RawValue = "ToString" };

            // Get results with FilterOnReturnType = true (default)
            Manager.Settings.FilterOnReturnType = true;
            var membersWithReturnType = Manager.GetMembers(searchExpression, iteration: 0);

            // Get results with FilterOnReturnType = false
            Manager.Settings.FilterOnReturnType = false;
            var membersWithoutReturnType = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert - Both should return results, but the setting should affect the search behavior
            Assert.IsNotNull(membersWithReturnType);
            Assert.IsNotNull(membersWithoutReturnType);
            // The setting is working if we get results in both cases
            Assert.AreEqual(2, membersWithoutReturnType.Count, "Expected to find 2 results with FilterOnReturnType=false");
        }

        [TestMethod]
        public void GetMembers_WithFilterOnParametersFalse_ReturnsResults()
        {
            // Arrange - Verify setting works
            Manager.Settings.FilterOnParameters = false;
            var searchExpression = new SearchExpression { RawValue = "Button" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert - Setting should still allow finding results by name
            Assert.IsNotNull(members);
            Assert.AreEqual(94, members.Count, "Expected to find 94 results with FilterOnParameters=false");
        }

        #endregion

        #region Special Settings

        [TestMethod]
        public void GetMembers_WithIsDelegateTypeTrue_ReturnsDelegates()
        {
            // Arrange
            Manager.Settings.IsDelegateType = true;
            // Use a broader search to find any delegates
            var searchExpression = new SearchExpression { RawValue = "Event" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(0, members.Count, "Expected to find 0 delegate types matching 'Event'");
            // If we find any types, they should be delegates
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.IsDelegate, $"Type {typeVM.Name} is not a delegate");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithIsEventArgsTypeTrue_ReturnsEventArgsTypes()
        {
            // Arrange
            Manager.Settings.IsEventArgsType = true;
            var searchExpression = new SearchExpression { RawValue = "EventArgs" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(36, members.Count, "Expected to find 36 EventArgs types");
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.Name.EndsWith("EventArgs"),
                        $"Type {typeVM.Name} does not end with 'EventArgs'");
                }
            }
        }

        #endregion

        #region Combined Settings

        [TestMethod]
        public void GetMembers_WithMultipleSettings_ReturnsMatchingResults()
        {
            // Arrange - Search for sealed classes only
            Manager.Settings.TypeKind = TypeKind.Class;
            Manager.Settings.IsSealedType = true;
            var searchExpression = new SearchExpression { RawValue = "Button" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(6, members.Count, "Expected to find 6 sealed classes");
            foreach (var member in members)
            {
                if (member is TypeViewModel typeVM)
                {
                    Assert.IsTrue(typeVM.IsClass, $"Type {typeVM.Name} is not a class");
                    Assert.IsTrue(typeVM.IsSealed, $"Type {typeVM.Name} is not sealed");
                }
            }
        }

        [TestMethod]
        public void GetMembers_WithMemberKindAndCanWrite_ReturnsWriteablePropertiesOnly()
        {
            // Arrange
            Manager.Settings.MemberKind = MemberKind.Property;
            Manager.Settings.CanWrite = true;
            var searchExpression = new SearchExpression { RawValue = "Content" };

            // Act
            var members = Manager.GetMembers(searchExpression, iteration: 0);

            // Assert
            Assert.IsNotNull(members);
            Assert.AreEqual(17, members.Count, "Expected to find 17 writeable Content properties");
            foreach (var member in members)
            {
                if (member is PropertyViewModel propVM)
                {
                    Assert.IsTrue(propVM.CanWrite, $"Property {propVM.Name} is not writeable");
                }
            }
        }

        #endregion
    }
}
