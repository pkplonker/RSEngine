using Engine;
using ImGuiNET;
using System;
using System.Linq;

namespace Editor.Controls;

public class UndoRedoPanel : IPanel
{
	private readonly IInputController inputController;
	public string PanelName { get; set; } = "Undo Redo";

	public UndoRedoPanel(IInputController inputController)
	{
		this.inputController = inputController;
		this.inputController.SubscribeToKeyEvent(OnKeyChange);
	}

	private bool OnKeyChange(IInputController.Key key, IInputController.InputState state)
	{
		if (!inputController.IsKeyPressed(IInputController.Key.ControlLeft) ||
		    state != IInputController.InputState.Pressed) return false;
		switch (key)
		{
			case IInputController.Key.Y:
				UndoManager.Redo();
				return false;
			case IInputController.Key.Z:
				UndoManager.Undo();
				return false;
			default:
				return false;
		}
	}

	public void Draw(IRenderer renderer)
	{
		ImGui.Begin(PanelName);

		if (ImGui.Button("Undo"))
		{
			UndoManager.Undo();
		}

		ImGui.SameLine();

		if (ImGui.Button("Redo"))
		{
			UndoManager.Redo();
		}

		ImGui.SameLine();

		if (ImGui.Button("Clear"))
		{
			UndoManager.Clear();
		}

		ImGui.Separator();

		ImGui.Text("Undo Stack:");
		if (ImGui.BeginChild("UndoStack", new System.Numerics.Vector2(0, 150)))
		{
			foreach (var action in UndoManager.GetUndoActions().Reverse())
			{
				ImGui.Text(action);
			}

			ImGui.EndChild();
		}

		ImGui.Separator();

		// Scrollable box for Redo Stack
		ImGui.Text("Redo Stack:");
		if (ImGui.BeginChild("RedoStack", new System.Numerics.Vector2(0, 150)))
		{
			foreach (var action in UndoManager.GetRedoActions().Reverse())
			{
				ImGui.Text(action);
			}

			ImGui.EndChild();
		}

		ImGui.End();
	}
}