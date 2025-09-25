using System.Collections.Concurrent;

namespace Engine.Logging;

public static class Logger
{
	private static ConcurrentQueue<LogMessage> logQueue = new ConcurrentQueue<LogMessage>();
	private static ConcurrentBag<ILogSink> sinks = new ConcurrentBag<ILogSink>();
	private static bool isRunning;
	private static readonly Thread loggerThread;
	private const int threadInterval = 10;

	static Logger()
	{
		loggerThread = new Thread(new ThreadStart(ProcessLogs))
		{
			Name = "Logging thread",
			IsBackground = true,
		};
		loggerThread.Start();
	}

	public static void AddSink(ILogSink sink)
	{
		sinks.Add(sink);
	}

	private static void LogInternal(LogLevel level, string message)
	{
		logQueue.Enqueue(new LogMessage {Level = level, Message = message, Timestamp = DateTime.UtcNow});
	}

	public static void Log(object message)
	{
		LogInternal(LogLevel.Debug, message.ToString() ?? string.Empty);
	}

	public static void Info(object message)
	{
		LogInternal(LogLevel.Info, message.ToString() ?? string.Empty);
	}

	public static void Warning(object message)
	{
		LogInternal(LogLevel.Warning, message.ToString() ?? string.Empty);
	}

	public static void Error(object message)
	{
		LogInternal(LogLevel.Error, message.ToString() ?? string.Empty);
	}

	public static void Start()
	{
		isRunning = true;
	}

	public static void Stop()
	{
		isRunning = false;
	}

	private static void ProcessLogs()
	{
		while (isRunning)
		{
			while (logQueue.TryDequeue(out LogMessage message))
			{
				foreach (var sink in sinks)
				{
					sink.Emit(message);
				}
			}

			Thread.Sleep(threadInterval);
		}
	}

	public static void Flush()
	{
		while (logQueue.Any())
		{
			
		}
	}
}