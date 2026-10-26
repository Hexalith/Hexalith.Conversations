using Synthetic;

[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
namespace Synthetic;
public class Fixture {}
 public class TestClass : Xunit.IClassFixture<Fixture> { public TestClass(Fixture value) { System.ArgumentNullException.ThrowIfNull(value); } [Xunit.Fact] public void Scenario01() {} }