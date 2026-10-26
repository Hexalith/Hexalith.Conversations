using Synthetic;

[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
namespace Synthetic;
public class Fixture {} [Xunit.CollectionDefinition("named")] public class Group : Xunit.ICollectionFixture<Fixture> {}
[Xunit.Collection("named")] public class TestClass { public TestClass(Fixture value) { System.ArgumentNullException.ThrowIfNull(value); } [Xunit.Fact] public void Scenario01() {} }