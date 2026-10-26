namespace Synthetic; public enum Choice { Zero, One } public struct Value { public int Number; } public class Fixture {}
public class Default0(int value = 8675309) { [Xunit.Fact] public void Scenario01() { if (!(value == 8675309)) throw new System.Exception("Unexpected default"); } }
public class Default1(long value = 1) { [Xunit.Fact] public void Scenario01() { if (!(value == 1L)) throw new System.Exception("Unexpected default"); } }
public class Default2(string value = "") { [Xunit.Fact] public void Scenario01() { if (!(value == "")) throw new System.Exception("Unexpected default"); } }
public class Default3(object value = null) { [Xunit.Fact] public void Scenario01() { if (!(value is null)) throw new System.Exception("Unexpected default"); } }
public class Default4(int? value = 7) { [Xunit.Fact] public void Scenario01() { if (!(value == 7)) throw new System.Exception("Unexpected default"); } }
public class Default5(int? value = null) { [Xunit.Fact] public void Scenario01() { if (!(value is null)) throw new System.Exception("Unexpected default"); } }
public class Default6(Choice value = Choice.One) { [Xunit.Fact] public void Scenario01() { if (!(value == Choice.One)) throw new System.Exception("Unexpected default"); } }
public class Default7(Choice? value = Choice.One) { [Xunit.Fact] public void Scenario01() { if (!(true)) throw new System.Exception("Unexpected default"); } }
public class Default8(decimal value = 1.5m) { [Xunit.Fact] public void Scenario01() { if (!(value == 1.5m)) throw new System.Exception("Unexpected default"); } }
public class Default9(System.DateTime value = default) { [Xunit.Fact] public void Scenario01() { if (!(value == default(System.DateTime))) throw new System.Exception("Unexpected default"); } }
public class Default10([System.Runtime.InteropServices.Optional] string value) { [Xunit.Fact] public void Scenario01() { if (!(value is null)) throw new System.Exception("Unexpected default"); } }
public class Default11([System.Runtime.InteropServices.Optional] int value) { [Xunit.Fact] public void Scenario01() { if (!(value == 0)) throw new System.Exception("Unexpected default"); } }
public class Default12(params string[] value) { [Xunit.Fact] public void Scenario01() { if (!(value.Length == 0)) throw new System.Exception("Unexpected default"); } }
public class Default13([System.Runtime.InteropServices.DefaultParameterValue(3)] int value) { [Xunit.Fact] public void Scenario01() { if (!(value == 3)) throw new System.Exception("Unexpected default"); } }
public class Default14(Choice? value = null) { [Xunit.Fact] public void Scenario01() { if (!(value is null)) throw new System.Exception("Unexpected default"); } }
public class Default15(Value value = default) { [Xunit.Fact] public void Scenario01() { if (!(value.Number == 0)) throw new System.Exception("Unexpected default"); } }
public class Default16(decimal? value = 1.5m) { [Xunit.Fact] public void Scenario01() { if (!(value == 1.5m)) throw new System.Exception("Unexpected default"); } }
public class Default17([System.Runtime.CompilerServices.DateTimeConstant(123)] System.DateTime value) { [Xunit.Fact] public void Scenario01() { if (!(value.Ticks == 123)) throw new System.Exception("Unexpected default"); } }
public class Default18([System.Runtime.InteropServices.Optional] Choice value) { [Xunit.Fact] public void Scenario01() { if (!(value == Choice.Zero)) throw new System.Exception("Unexpected default"); } }
public class Default19(Fixture value = null) : Xunit.IClassFixture<Fixture> { [Xunit.Fact] public void Scenario01() { if (!(value != null)) throw new System.Exception("Unexpected default"); } }
public class Default20(Xunit.ITestOutputHelper value = null) { [Xunit.Fact] public void Scenario01() { if (!(value != null)) throw new System.Exception("Unexpected default"); } }
public class Default21(bool value = true) { [Xunit.Fact] public void Scenario01() { if (!(value)) throw new System.Exception("Unexpected default"); } }
public class Default22(char value = 'é') { [Xunit.Fact] public void Scenario01() { if (!(value == 'é')) throw new System.Exception("Unexpected default"); } }
public class Default23(byte value = 2) { [Xunit.Fact] public void Scenario01() { if (!(value == 2)) throw new System.Exception("Unexpected default"); } }
public class Default24(double value = 1.25) { [Xunit.Fact] public void Scenario01() { if (!(value == 1.25)) throw new System.Exception("Unexpected default"); } }
