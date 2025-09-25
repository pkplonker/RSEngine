namespace Engine.Logging;

public interface ILogSink
{
	void Emit(LogMessage message);
}

public enum LogLevel
{
	Debug,
	Info,
	Warning,
	Error,
}

public class LogMessage
{
	public DateTime Timestamp { get; set; }
	public LogLevel Level { get; set; }
	public string Message { get; set; }
}