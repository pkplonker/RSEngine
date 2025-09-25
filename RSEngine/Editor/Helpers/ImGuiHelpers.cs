using System.Numerics;
using Editor.Controls;
using Editor.Properties;
using Engine;
using ImGuiNET;
using System.Windows.Forms;

namespace Editor;

public static class ImGuiHelpers
{
	public static void DrawTransform(ITransform trans)
	{
		ImGui.Separator();
		float labelWidth = ImGui.CalcTextSize("Rotation").X + 20.0f;
		float totalWidth = ImGui.GetContentRegionAvail().X - labelWidth;
		float fieldWidth = totalWidth / 3.0f - ImGui.GetStyle().ItemInnerSpacing.X;

		ImGui.Text("Position");
		ImGui.SameLine(labelWidth);
		ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(1, 0, 0, 1));
		ImGui.Text("X");
		ImGui.SameLine();
		ImGui.SetNextItemWidth(fieldWidth - ImGui.CalcTextSize("X").X);
		UndoableImGui.UndoableDragFloat(
			$"##PosX{trans.GetHashCode()}",
			"Change Position X",
			() => trans.Position.X,
			value => trans.Position = new Vector3(value, trans.Position.Y, trans.Position.Z),
			stretch: false);
		ImGui.PopStyleColor();
		ImGui.SameLine();
		ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 1, 0, 1));
		ImGui.Text("Y");
		ImGui.SameLine();
		ImGui.SetNextItemWidth(fieldWidth - ImGui.CalcTextSize("Y").X);
		UndoableImGui.UndoableDragFloat(
			$"##PosY{trans.GetHashCode()}",
			"Change Position Y",
			() => trans.Position.Y,
			value => trans.Position = new Vector3(trans.Position.X, value, trans.Position.Z),
			stretch: false);
		ImGui.PopStyleColor();
		ImGui.SameLine();
		ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 0, 1, 1));
		ImGui.Text("Z");
		ImGui.SameLine();
		ImGui.SetNextItemWidth(fieldWidth - ImGui.CalcTextSize("Z").X);
		UndoableImGui.UndoableDragFloat(
			$"##PosZ{trans.GetHashCode()}",
			"Change Position Z",
			() => trans.Position.Z,
			value => trans.Position = new Vector3(trans.Position.X, value, trans.Position.Z),
			stretch: false);
		ImGui.PopStyleColor();

		ImGui.Text("Rotation");
		ImGui.SameLine(labelWidth);
		ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(1, 0, 0, 1));
		ImGui.Text("X");
		ImGui.SameLine();
		ImGui.SetNextItemWidth(fieldWidth - ImGui.CalcTextSize("X").X);
		UndoableImGui.UndoableDragFloat(
			$"##RotX{trans.GetHashCode()}",
			"Change Rotation X",
			() => trans.Rotation.ToEulerDegrees().X,
			value => trans.Rotation = new Vector3(value, trans.Rotation.Y, trans.Rotation.Z).ToQuaternionFromDegrees(),
			stretch: false);
		ImGui.PopStyleColor();
		ImGui.SameLine();
		ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 1, 0, 1));
		ImGui.Text("Y");
		ImGui.SameLine();
		ImGui.SetNextItemWidth(fieldWidth - ImGui.CalcTextSize("Y").X);
		UndoableImGui.UndoableDragFloat(
			$"##RotY{trans.GetHashCode()}",
			"Change Rotation Y",
			() => trans.Rotation.ToEulerDegrees().Y,
			value => trans.Rotation = new Vector3(trans.Rotation.X, value, trans.Rotation.Z).ToQuaternionFromDegrees(),
			stretch: false);
		ImGui.PopStyleColor();
		ImGui.SameLine();
		ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 0, 1, 1));
		ImGui.Text("Z");
		ImGui.SameLine();
		ImGui.SetNextItemWidth(fieldWidth - ImGui.CalcTextSize("Z").X);
		UndoableImGui.UndoableDragFloat(
			$"##RotZ{trans.GetHashCode()}",
			"Change Rotation Z",
			() => trans.Rotation.ToEulerDegrees().Z,
			value => trans.Rotation = new Vector3(trans.Rotation.X, trans.Rotation.Y, value).ToQuaternionFromDegrees(),
			stretch: false);
		ImGui.PopStyleColor();

		ImGui.Text("Scale");
		ImGui.SameLine(labelWidth);
		ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(1, 0, 0, 1));
		ImGui.Text("X");
		ImGui.SameLine();
		ImGui.SetNextItemWidth(fieldWidth - ImGui.CalcTextSize("X").X);
		UndoableImGui.UndoableDragFloat(
			$"##ScaleX{trans.GetHashCode()}",
			"Change Scale X",
			() => trans.Scale.X,
			value => trans.Scale = new Vector3(value, trans.Scale.Y, trans.Scale.Z), stretch: false
		);
		ImGui.PopStyleColor();
		ImGui.SameLine();
		ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 1, 0, 1));
		ImGui.Text("Y");
		ImGui.SameLine();
		ImGui.SetNextItemWidth(fieldWidth - ImGui.CalcTextSize("Y").X);
		UndoableImGui.UndoableDragFloat(
			$"##ScaleY{trans.GetHashCode()}",
			"Change Scale Y",
			() => trans.Scale.Y,
			value => trans.Scale = new Vector3(trans.Scale.X, value, trans.Scale.Z),
			stretch: false);
		ImGui.PopStyleColor();
		ImGui.SameLine();
		ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0, 0, 1, 1));
		ImGui.Text("Z");
		ImGui.SameLine();
		ImGui.SetNextItemWidth(fieldWidth - ImGui.CalcTextSize("Z").X);
		UndoableImGui.UndoableDragFloat(
			$"##ScaleZ{trans.GetHashCode()}",
			"Change Scale Z",
			() => trans.Scale.Z,
			value => trans.Scale = new Vector3(trans.Scale.X, trans.Scale.Y, value),
			stretch: false);
		ImGui.PopStyleColor();
	}

	public static void AddProperty(IMemberAdapter member)
	{
		if (ImGui.Button($"Add {member.Name}"))
		{
			var result = FileDialog.OpenFileDialog(FileDialog.FilterByType(member.MemberType));
		}
	}

	public static bool CenteredButton(string buttonText)
	{
		var windowSize = ImGui.GetWindowSize();
		var buttonSize = ImGui.CalcTextSize(buttonText);

		var buttonPosX = (windowSize.X - buttonSize.X) * 0.5f;
		ImGui.SetCursorPosX(buttonPosX);

		return ImGui.Button(buttonText);
	}
}