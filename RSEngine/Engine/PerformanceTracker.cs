using Engine.Logging;

namespace Engine;

using System;
using System.Collections.Generic;
using System.Diagnostics;
public class PerformanceTracker : IDisposable
{
	private static Dictionary<string, List<long>> recentElapsedTimes = new Dictionary<string, List<long>>();
	private static int bufferSize = 60;

	private Stopwatch stopwatch = new Stopwatch();
	private string functionName;
	private static bool write = false;

	public PerformanceTracker(string functionName)
	{
		this.functionName = functionName;
		stopwatch.Start();
	}

	public void Dispose()
	{
		stopwatch.Stop();
		long elapsedTicks = stopwatch.ElapsedTicks;

		if (!recentElapsedTimes.ContainsKey(functionName))
		{
			recentElapsedTimes[functionName] = new List<long>(bufferSize);
		}

		recentElapsedTimes[functionName].Add(elapsedTicks);

		// Keep only the last 'bufferSize' measurements
		if (recentElapsedTimes[functionName].Count > bufferSize)
		{
			recentElapsedTimes[functionName].RemoveAt(0);
		}
	}

	public static void ReportAverages()
	{
		if (!write) return;
		foreach (var functionName in recentElapsedTimes.Keys)
		{
			int iterationCount = recentElapsedTimes[functionName].Count;
			if (iterationCount > 0)
			{
				double averageMilliseconds = recentElapsedTimes[functionName].Average() / Stopwatch.Frequency * 1000.0;
				Logging.Logger.Log($"Average time for {functionName} over {iterationCount} iterations: {averageMilliseconds} ms");
			}
		}
	}
}
