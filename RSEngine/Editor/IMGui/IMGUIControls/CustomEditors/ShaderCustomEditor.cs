using Editor.IMGUIControls;
using Editor.Properties;
using Engine;
using Engine.Logging;
using ImGuiNET;
using Shader = Engine.Shader;

namespace Editor.Controls;

[CustomEditor(typeof(Engine.Shader))]
public class ShaderCustomEditor : BaseCustomEditor
{
	public override void Draw(object component, object owningObject, IMemberAdapter memberInfoToSetObjectOnOwner,
		IRenderer renderer, int depth = 0)
	{
		Draw<Shader>(component, owningObject, memberInfoToSetObjectOnOwner,
			renderer, depth);
	}

	protected override void DropProps(object component, IMemberAdapter memberInfoToSetObjectOnOwner,
		object owningObject,
		int depth = 0)
	{
		if (component is not Shader shader) return;
		if (!string.IsNullOrEmpty(shader.ShaderPath))
		{
			ImGui.Text(
				$"{nameof(shader.ShaderPath).GetFormattedName()} : {shader.ShaderPath.MakeProjectRelative()}");
			if (ImGui.Button("Edit##shader"))
			{
				FileDialog.OpenFileWithDefaultApp(shader.ShaderPath.MakeAbsolute());
			}

			ImGui.SameLine();

			if (owningObject != null && ImGui.Button("Remove##shader"))
			{
				memberInfoToSetObjectOnOwner.SetValue(owningObject, Guid.Empty);
			}

			ImGui.SameLine();
		}

		if (ImGui.Button("Reload"))
		{
			ResourceManager.Instance.ReleaseResource(shader.GUID);
		}
	}
}