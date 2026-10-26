[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
namespace Synthetic;
public class A {} public class B {}
[Xunit.CollectionDefinition("a")] public class GroupA : Xunit.ICollectionFixture<A> {}
[Xunit.CollectionDefinition("b")] public class GroupB : Xunit.ICollectionFixture<B> {}
[Xunit.Collection("a")] public class Base {}
[Xunit.Collection(typeof(GroupB))] public class TestClass : Base { public TestClass(B value) { System.ArgumentNullException.ThrowIfNull(value); } [Xunit.Fact] public void Scenario01() {} }
