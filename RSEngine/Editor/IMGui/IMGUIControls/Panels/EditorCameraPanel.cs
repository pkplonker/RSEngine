using Engine;
using ImGuiNET;

namespace Editor.Controls;

public class EditorCameraPanel : IPanel
{
	private readonly IEditorCamera editorCamera;

	public EditorCameraPanel(IEditorCamera editorCamera)
	{
		this.editorCamera = editorCamera;
	}

	public string PanelName { get; set; } = "Editor Camera";

	public void Draw(IRenderer renderer)
	{
		ImGui.Begin(PanelName);
		ImGui.Text($"Aspect Ratio: {editorCamera.AspectRatio}");
		var originalPos = editorCamera.Transform.Position;
		var originalRotation = editorCamera.Transform.Rotation;

		var originalAspect = editorCamera.AspectRatio;

		UndoableImGui.UndoableButton("Reset",
			new Memento(() => { editorCamera.Reset(); },
				() =>
				{
					editorCamera.Transform.Rotation = originalRotation;
					editorCamera.Transform.Position = originalPos;
					editorCamera.AspectRatio = originalAspect;
				}, "Reset Editor Camera"));
		ImGuiHelpers.DrawTransform(editorCamera.Transform);
		ImGui.End();
	}
}