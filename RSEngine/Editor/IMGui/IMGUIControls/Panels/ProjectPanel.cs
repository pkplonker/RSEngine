using Engine;
using ImGuiNET;
using System;
using Engine.Logging;

namespace Editor.Controls;

public class ProjectPanel : IPanel
{
	public string PanelName { get; set; } = "Project";

	private readonly IInputController inputController;
	private byte[] renameBuffer = new byte[256];

	public ProjectPanel(IInputController inputController)
	{
		this.inputController = inputController;
	}

	public void Draw(IRenderer renderer)
	{
		ImGui.Begin(PanelName);
		var project = ProjectManager.ActiveProject;
		if (project == null)
		{
			ImGui.Text($"No active project");
		}
		else
		{
			ImGui.TextWrapped($"Name: {project.Name}");
			ImGui.TextWrapped($"Name: {project.Directory}");
			ImGui.TextWrapped($"Name: {project.AssetsDirectory}");
#if DEVELOP
			ImGui.TextWrapped($"Core Assets Directory: {project.CoreAssetsDirectory}");
#endif
			ImGui.Text($"Scene Name:");
			if (SceneController.ActiveScene != null)
			{
				ImGui.SameLine();
				Array.Clear(renameBuffer, 0, renameBuffer.Length);
				System.Text.Encoding.UTF8.GetBytes(SceneController.ActiveScene.Name)
					.CopyTo(renameBuffer, 0);

				if (ImGui.InputText("##rename", renameBuffer, (uint) renameBuffer.Length))
				{
					SceneController.ActiveScene.Name = System.Text.Encoding.UTF8.GetString(renameBuffer).TrimEnd('\0');
				}
			}

			ImGui.End();
		}
	}
}