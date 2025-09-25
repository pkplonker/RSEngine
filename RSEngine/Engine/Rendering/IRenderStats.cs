namespace Engine;

public interface IRenderStats
{
	int DrawCalls { get; }
	int MaterialsUsed { get; }
	int ShadersUsed { get; }
	uint Triangles { get; set; }
	uint Vertices { get; set; }
}