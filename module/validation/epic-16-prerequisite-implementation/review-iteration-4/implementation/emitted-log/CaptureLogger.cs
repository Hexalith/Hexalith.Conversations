using Microsoft.Extensions.Logging;
public sealed class CaptureLogger<T> : ILogger<T>
{
 public List<object> Rows {get;}=[];
 public IDisposable? BeginScope<TState>(TState state) where TState:notnull => null;
 public bool IsEnabled(LogLevel level)=>true;
 public void Log<TState>(LogLevel level,EventId eventId,TState state,Exception? error,Func<TState,Exception?,string> formatter)
 {
  Rows.Add(new {category=typeof(T).FullName,level=level.ToString(),eventId=eventId.Id,message=formatter(state,error),state=((IEnumerable<KeyValuePair<string,object?>>)(object)state!).ToDictionary(p=>p.Key,p=>p.Value)});
 }
}
