[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
namespace Synthetic;
public class A {} public class B {}
[Xunit.CollectionDefinition("a")] public class GroupA : Xunit.ICollectionFixture<A> {}
[Xunit.CollectionDefinition("b")] public class GroupB : Xunit.ICollectionFixture<A> {}
[Xunit.Collection("a")] public class Base {}
[Xunit.Collection<GroupB>] public class TestClass : Base { public TestClass(A value) { System.ArgumentNullException.ThrowIfNull(value); } [Xunit.Fact] public void Scenario01() {} }
