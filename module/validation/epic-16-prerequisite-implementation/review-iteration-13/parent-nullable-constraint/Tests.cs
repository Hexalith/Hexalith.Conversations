namespace Synthetic; public enum Choice { Zero, One }
public sealed class IntDefault(int value = 8675309) { [Xunit.Fact] public void Scenario01() { if (!(value == 8675309)) throw new System.Exception("Unexpected actual xUnit default"); } }
public sealed class LongDefault(long value = 1) { [Xunit.Fact] public void Scenario01() { if (!(value == 1L)) throw new System.Exception("Unexpected actual xUnit default"); } }
public sealed class StringDefault(string value = "") { [Xunit.Fact] public void Scenario01() { if (!(value == "")) throw new System.Exception("Unexpected actual xUnit default"); } }
public sealed class NullReference(object value = null) { [Xunit.Fact] public void Scenario01() { if (!(value is null)) throw new System.Exception("Unexpected actual xUnit default"); } }
public sealed class NullableValue(int? value = 7) { [Xunit.Fact] public void Scenario01() { if (!(value == 7)) throw new System.Exception("Unexpected actual xUnit default"); } }
public sealed class NullableNull(int? value = null) { [Xunit.Fact] public void Scenario01() { if (!(value is null)) throw new System.Exception("Unexpected actual xUnit default"); } }
public sealed class EnumDefault(Choice value = Choice.One) { [Xunit.Fact] public void Scenario01() { if (!(value == Choice.One)) throw new System.Exception("Unexpected actual xUnit default"); } }
public sealed class NullableEnum(Choice? value = Choice.One) { [Xunit.Fact] public void Scenario01() { if (!(value == Choice.One)) throw new System.Exception("Unexpected actual xUnit default"); } }
public sealed class DecimalDefault(decimal value = 1.5m) { [Xunit.Fact] public void Scenario01() { if (!(value == 1.5m)) throw new System.Exception("Unexpected actual xUnit default"); } }
public sealed class DateTimeDefault(System.DateTime value = default) { [Xunit.Fact] public void Scenario01() { if (!(value == default(System.DateTime))) throw new System.Exception("Unexpected actual xUnit default"); } }
public sealed class OptionalString([System.Runtime.InteropServices.Optional] string value) { [Xunit.Fact] public void Scenario01() { if (!(value is null)) throw new System.Exception("Unexpected actual xUnit default"); } }
public sealed class OptionalInt([System.Runtime.InteropServices.Optional] int value) { [Xunit.Fact] public void Scenario01() { if (!(value == 0)) throw new System.Exception("Unexpected actual xUnit default"); } }
public sealed class ParamsArray(params string[] value) { [Xunit.Fact] public void Scenario01() { if (!(value.Length == 0)) throw new System.Exception("Unexpected actual xUnit default"); } }
public sealed class AttributeDefault([System.Runtime.InteropServices.DefaultParameterValue(3)] int value) { [Xunit.Fact] public void Scenario01() { if (!(value == 3)) throw new System.Exception("Unexpected actual xUnit default"); } }
