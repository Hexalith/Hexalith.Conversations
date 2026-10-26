using Synthetic;
[assembly:Xunit.AssemblyFixture(typeof(AssemblyData))]
[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
namespace Synthetic;
public class AssemblyData {} public class CollectionData(AssemblyData value) {} public class ClassData(CollectionData value) {} [Xunit.CollectionDefinition("nested")] public class Group : Xunit.ICollectionFixture<CollectionData>, Xunit.IClassFixture<ClassData> {}
[Xunit.Collection("nested")] public class TestClass { public TestClass(ClassData value) { System.ArgumentNullException.ThrowIfNull(value); } [Xunit.Fact] public void Scenario01() {} }