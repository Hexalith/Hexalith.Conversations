using Synthetic;

[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
namespace Synthetic;
public class Fixture {}public class Base : Xunit.IClassFixture<Fixture> {}
 public class TestClass : Base { public TestClass(Fixture value) { System.ArgumentNullException.ThrowIfNull(value); } [Xunit.Fact] public void Scenario01() {} }