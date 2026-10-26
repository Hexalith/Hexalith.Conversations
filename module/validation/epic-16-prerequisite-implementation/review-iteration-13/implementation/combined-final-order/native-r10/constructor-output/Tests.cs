using Synthetic;

[assembly:System.Reflection.AssemblyInformationalVersion("1.0.0+aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
namespace Synthetic;

 public class TestClass { public TestClass(Xunit.ITestOutputHelper value) { value.WriteLine("synthetic output"); } [Xunit.Fact] public void Scenario01() {} }