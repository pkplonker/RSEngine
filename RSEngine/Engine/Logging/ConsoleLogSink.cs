using System.Collections.Concurrent;

namespace Engine.Logging;

public class ConsoleLogSink : ILogSink
{
	private readonly Thread loggerThread;
	private bool isRunning;
	private ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
	private const int threadInterval = 11;

	public ConsoleLogSink()
	{
		loggerThread = new Thread(new ThreadStart(ProcessLogs))
		{
			Name = "Console logging thread",
			IsBackground = true,
		};
		loggerThread.Start();
	}

	public void Emit(LogMessage message)
	{
		logQueue.Enqueue(
			$"[{message.Timestamp.ToShortTimeString()}] [{Enum.GetName(typeof(LogLevel), message.Level)}] {message.Message}");
	}

	private void ProcessLogs()
	{
		while (true)
		{
			while (logQueue.TryDequeue(out string message))
			{
				Console.WriteLine(message);
			}

			Thread.Sleep(threadInterval);
		}
	}
}