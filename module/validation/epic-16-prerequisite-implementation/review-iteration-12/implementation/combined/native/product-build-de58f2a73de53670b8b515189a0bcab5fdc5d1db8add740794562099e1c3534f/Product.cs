#nullable enable
namespace Probe; public interface IFoo {} public class Surface { public int Added() => 1; public T Existing<T>(T value) where T: IFoo? => value; }