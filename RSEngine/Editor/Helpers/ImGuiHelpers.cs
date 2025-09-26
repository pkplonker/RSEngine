using System.Numerics;
using Editor.Properties;
using Engine;
using ImGuiNET;

namespace Editor;

public static class ImGuiHelpers
{
    public static void DrawTransform(ITransform trans)
    {
        ImGui.Separator();

        DrawVec3("Position", trans.Position, newPos => trans.Position = newPos);

        DrawVec3("Rotation", trans.Rotation.ToEulerDegrees(),
            newRot => trans.Rotation = newRot.ToQuaternionFromDegrees());

        DrawVec3("Scale", trans.Scale, newScale => trans.Scale = newScale);
    }

    public static void DrawVec2(string label, Vector2 value, Action<Vector2> setter,
        float? min = null, float? max = null, float step = 0.1f, float speed = 1.0f)
    {
        float labelWidth = ImGui.CalcTextSize(label).X + 20.0f;
        float totalWidth = ImGui.GetContentRegionAvail().X - labelWidth;
        float fieldWidth = totalWidth / 2.0f - ImGui.GetStyle().ItemInnerSpacing.X;
        ImGui.Text(label);
        ImGui.SameLine();
        DrawFloatComponent($"X_{label}", "X", value.X, fieldWidth, new Vector4(1, 0, 0, 1),
            newX => setter(new Vector2(newX, value.Y)), min, max, step, speed);
        ImGui.SameLine();
        DrawFloatComponent($"Y_{label}", "Y", value.Y, fieldWidth, new Vector4(0, 1, 0, 1),
            newY => setter(new Vector2(value.X, newY)), min, max, step, speed);
    }

    public static void DrawVec3(string label, Vector3 value, Action<Vector3> setter,
        float? min = null, float? max = null, float step = 0.1f, float speed = 1.0f)
    {
        float labelWidth = ImGui.CalcTextSize(label).X + 20.0f;
        float totalWidth = ImGui.GetContentRegionAvail().X - labelWidth;
        float fieldWidth = totalWidth / 3.0f - ImGui.GetStyle().ItemInnerSpacing.X;
        ImGui.Text(label);
        ImGui.SameLine();
        DrawFloatComponent($"X_{label}", "X", value.X, fieldWidth, new Vector4(1, 0, 0, 1),
            newX => setter(new Vector3(newX, value.Y, value.Z)), min, max, step, speed);
        ImGui.SameLine();
        DrawFloatComponent($"Y_{label}", "Y", value.Y, fieldWidth, new Vector4(0, 1, 0, 1),
            newY => setter(new Vector3(value.X, newY, value.Z)), min, max, step, speed);
        ImGui.SameLine();
        DrawFloatComponent($"Z_{label}", "Z", value.Z, fieldWidth, new Vector4(0, 0, 1, 1),
            newZ => setter(new Vector3(value.X, value.Y, newZ)), min, max, step, speed);
    }

    public static void DrawVec4(string label, Vector4 value, Action<Vector4> setter,
        float? min = null, float? max = null, float step = 0.1f, float speed = 1.0f)
    {
        float labelWidth = ImGui.CalcTextSize(label).X + 20.0f;
        float totalWidth = ImGui.GetContentRegionAvail().X - labelWidth;
        float fieldWidth = totalWidth / 4.0f - ImGui.GetStyle().ItemInnerSpacing.X;
        ImGui.Text(label);
        ImGui.SameLine();
        DrawFloatComponent($"X_{label}", "X", value.X, fieldWidth, new Vector4(1, 0, 0, 1),
            newX => setter(new Vector4(newX, value.Y, value.Z, value.W)), min, max, step, speed);
        ImGui.SameLine();
        DrawFloatComponent($"Y_{label}", "Y", value.Y, fieldWidth, new Vector4(0, 1, 0, 1),
            newY => setter(new Vector4(value.X, newY, value.Z, value.W)), min, max, step, speed);
        ImGui.SameLine();
        DrawFloatComponent($"Z_{label}", "Z", value.Z, fieldWidth, new Vector4(0, 0, 1, 1),
            newZ => setter(new Vector4(value.X, value.Y, newZ, value.W)), min, max, step, speed);
        ImGui.SameLine();
        DrawFloatComponent($"W_{label}", "W", value.W, fieldWidth,
            new Vector4(0.4f, 0.4f, 0.4f, 1),
            newW => setter(new Vector4(value.X, value.Y, value.Z, newW)), min, max, step, speed);
    }
    
    public static void DrawVec4Color(string? label, Vector4 value, Action<Vector4> setter, float? min = 0, float? max = 1, float step = 0.01f, float speed = 0.01f)
    {
        DrawVec4(label, value, setter, min, max, step, speed);
    }
    
    private static void DrawFloatComponent(string id, string label, float value, float width, Vector4 color,
        Action<float> setter,
        float? min = null, float? max = null, float step = 0.1f, float speed = 1.0f)
    {
        ImGui.PushStyleColor(ImGuiCol.FrameBg, color);
        ImGui.Text(label);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(width - ImGui.CalcTextSize(label).X);

        UndoableImGui.UndoableDragFloat($"##{id}", $"Change {label}",
            () => value,
            setter,
            min: min ?? float.MinValue,
            max: max ?? float.MaxValue,
            speed: speed,
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
    
    public static void DrawFloat(string label, float value, Action<float> setter, 
        float? min = null, float? max = null, float step = 0.1f, float speed = 1.0f)
    {
        float labelWidth = ImGui.CalcTextSize(label).X + 20.0f;
        float totalWidth = ImGui.GetContentRegionAvail().X - labelWidth;
    
        ImGui.Text(label);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(totalWidth);
    
        float constrainedValue = value;
        if (min.HasValue) constrainedValue = Math.Max(constrainedValue, min.Value);
        if (max.HasValue) constrainedValue = Math.Min(constrainedValue, max.Value);
    
        if (ImGui.DragFloat($"##{label}_drag", ref constrainedValue, speed, 
                min ?? float.MinValue, max ?? float.MaxValue))
        {
            setter(constrainedValue);
        }
    }

    public static void DrawInt(string label, int value, Action<int> setter, 
        int min = int.MinValue, int max = int.MaxValue, int step = 1, float speed = 1.0f)
    {
        float labelWidth = ImGui.CalcTextSize(label).X + 20.0f;
        float totalWidth = ImGui.GetContentRegionAvail().X - labelWidth;
    
        ImGui.Text(label);
        ImGui.SameLine();
        ImGui.SetNextItemWidth(totalWidth);
    
        int constrainedValue = Math.Max(min, Math.Min(max, value));
    
        if (ImGui.DragInt($"##{label}_drag", ref constrainedValue, speed, min, max))
        {
            setter(constrainedValue);
        }
    }
}