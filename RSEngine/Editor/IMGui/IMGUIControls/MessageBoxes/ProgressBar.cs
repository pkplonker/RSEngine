using ImGuiNET;

namespace Editor.Controls;

public class ProgressBar
{
	private static bool isVisible = false;
	private static string message = string.Empty;
	private static string title;
	private static float progress;
	private static ProgressUpdater? progressUpdate;

	public static void Show(string title, ProgressUpdater progressUpdate = null, string message = "")
	{
		ProgressBar.message = message;
		isVisible = true;
		ProgressBar.title = title ?? string.Empty;
		progress = 0;
		ProgressBar.progressUpdate = progressUpdate ?? null;
	}

	public static void UpdateProgress(float progress, string? updatedMessage = null)
	{
		if (!string.IsNullOrEmpty(updatedMessage))
		{
			message = updatedMessage;
		}

		ProgressBar.progress = progress;
	}

	public static void Render()
	{
		if (!isVisible) return;

		if (ImGui.BeginPopupModal(title ?? string.Empty, ref isVisible,
			    ImGuiWindowFlags.Modal | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar |
			    ImGuiWindowFlags.NoScrollWithMouse))
		{
			if (!string.IsNullOrEmpty(message))
			{
				ImGui.Text(message);
			}

			ImGui.ProgressBar(progressUpdate?.Value ?? progress, new System.Numerics.Vector2(300, 0));

			ImGui.EndPopup();
		}

		if (isVisible)
		{
			ImGui.OpenPopup(title ?? string.Empty);
		}
	}

	public static void Close()
	{
		isVisible = false;
	}
}