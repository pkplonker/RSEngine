namespace Editor.Controls;

using ImGuiNET;
using System;

public static class InfoBox
{
	private static bool isVisible = false;
	private static string message = string.Empty;
	private static Action callback = () => { };
	private static Action onCancel = () => { };

	public static void Show(string message, Action callback)
	{
		InfoBox.message = message;
		InfoBox.callback = callback;
		isVisible = true;
	}

	public static void Show(string message)
	{
		InfoBox.message = message;
		isVisible = true;
	}

	public static void Render()
	{
		if (!isVisible) return;

		if (ImGui.BeginPopupModal("Info", ref isVisible, ImGuiWindowFlags.AlwaysAutoResize))
		{
			ImGui.Text(message);

			if (ImGui.Button("OK"))
			{
				callback?.Invoke();
				isVisible = false;
				ImGui.CloseCurrentPopup();
			}

			ImGui.EndPopup();
		}

		if (isVisible)
		{
			ImGui.OpenPopup("Info");
		}
	}

	private static void Close()
	{
		ImGui.CloseCurrentPopup();
		callback = () => { };
		onCancel = () => { };
	}
}