using Synthetic;

[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
namespace Synthetic;
public class Fixture(string missing) {}
 public class TestClass : Xunit.IClassFixture<Fixture> { public TestClass(Fixture value) {  } [Xunit.Fact] public void Scenario01() {} }