using Editor.IMGUIControls;
using Editor.Properties;
using Engine;
using Engine.Logging;
using ImGuiNET;

namespace Editor.Controls;

[CustomEditor(typeof(Engine.Material))]
public class MaterialCustomEditor : BaseCustomEditor
{
	public override void Draw(object component, object owningObject, IMemberAdapter memberInfoToSetObjectOnOwner,
		IRenderer renderer, int depth = 0)
	{
		Draw<Material>(component, owningObject, memberInfoToSetObjectOnOwner,
			renderer, depth);
	}

	protected override void DropProps(object component, IMemberAdapter memberInfoToSetObjectOnOwner,
		object owningObject, int depth)
	{
		if (component is not Material mat) return;
		try
		{
			if (ResourceManager.Instance.GetMetadata(mat.GUID, out var metadata))
			{
				ImGui.Text($"Material Path : {metadata.Path}");
			}
		}
		catch (Exception e)
		{
			Logger.Warning($"Failed to convert guid for UI {e}");
		}

		if (owningObject != null && ImGui.Button("Remove##material"))
		{
			memberInfoToSetObjectOnOwner.SetValue(owningObject, Guid.Empty);
		}

		propertyDrawer.ProcessProps(mat, ++depth);
	}
}