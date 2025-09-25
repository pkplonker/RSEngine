using System.Diagnostics;

namespace Engine;

public static class Time
{
	public static float DeltaTime { get; private set; }
	private static float lastFrameTime;

	public static double TotalTime { get; private set; }
	public static uint TotalFrameCount { get; private set; }
	private static int frameCount = 0;
	private static float timeSinceLastSecond = 0f;
	public static int FPS { get; private set; }

	public static string CurrentTime => DateTime.UtcNow.ToShortTimeString();

	public static void Update(float currentTime)
	{
		if (lastFrameTime == 0)
		{
			lastFrameTime = currentTime;
		}

		DeltaTime = currentTime - lastFrameTime;
		TotalTime += DeltaTime;
		lastFrameTime = currentTime;
		
		TotalFrameCount++;
		frameCount++;
		timeSinceLastSecond += DeltaTime;
		if (timeSinceLastSecond >= 1.0f)
		{
			FPS = frameCount;
			frameCount = 0;
			timeSinceLastSecond -= 1.0f;
		}
	}
}