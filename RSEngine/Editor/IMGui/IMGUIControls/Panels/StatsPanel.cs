using Engine;
using ImGuiNET;
using System;
using Engine.Logging;

namespace Editor.Controls;

public class StatsPanel : IPanel
{
	public string PanelName { get; set; } = "Stats";

	private double lastUpdateTime = 0;
	private string formattedTotalTime = "00:00:00";
	private readonly float[] fpsBuffer = new float[100];
	private int bufferIndex = 0;
	private readonly IInputController inputController;

	public StatsPanel(IInputController inputController)
	{
		this.inputController = inputController;
	}

	private void UpdateFpsBuffer(int fps)
	{
		fpsBuffer[bufferIndex] = fps;
		bufferIndex = (bufferIndex + 1) % fpsBuffer.Length;
	}

	public void Draw(IRenderer renderer)
	{
		if (Time.TotalTime - lastUpdateTime >= 1.0)
		{
			TimeSpan timeSpan = TimeSpan.FromSeconds(Time.TotalTime);
			formattedTotalTime = timeSpan.ToString(@"hh\:mm\:ss");
			UpdateFpsBuffer(Time.FPS);
			lastUpdateTime = Time.TotalTime;
		}

		ImGui.Begin(PanelName);
		ImGui.Text($"Current Time: {Time.CurrentTime}");
		ImGui.Text($"Uptime: {formattedTotalTime}");
		ImGui.Text($"DeltaTime: {Time.DeltaTime * 1000:F2}ms");
		ImGui.Text($"FPS: {Time.FPS}");
		ImGui.Separator();
		ImGui.Text($"Draw Calls: {renderer.DrawCalls}");
		ImGui.Text($"Triangles: {renderer.Triangles}");
		ImGui.Text($"Vertices: {renderer.Vertices}");
		ImGui.Text($"Materials: {renderer.MaterialsUsed}");
		ImGui.Text($"Shaders: {renderer.ShadersUsed}");
		ImGui.Separator();
		ImGui.Text($"Window Size: {renderer.WindowSize.X} x {renderer.WindowSize.Y}");
		var vps = renderer.GetSceneRenderTarget(SceneController.ActiveScene)?.ViewportSize;
		if (vps.HasValue)
		{
			ImGui.Text($"Viewport Size: {vps.Value.X} x {vps.Value.Y}");
		}

		ImGui.PlotLines(string.Empty, ref fpsBuffer[0], fpsBuffer.Length, 0, null, 0, fpsBuffer.Max(),
			new System.Numerics.Vector2(-1, 80));
		ImGui.Separator();
		ImGui.Text($"Mouse Pos: {inputController.GetMousePosition()}");
	
		ImGui.End();
	}
}