using System.Numerics;
using Editor.IMGUIControls;
using Editor.Properties;
using Engine;
using ImGuiNET;
using Texture = Engine.Texture;

namespace Editor.Controls;

[CustomEditor(typeof(Engine.Texture))]
public class TextureCustomEditor : BaseCustomEditor
{
	private Vector2 imageSize = new(175, 175);

	public override void Draw(object component, object owningObject, IMemberAdapter memberInfoToSetObjectOnOwner,
		IRenderer renderer, int depth = 0)
	{
		Draw<Texture>(component, owningObject, memberInfoToSetObjectOnOwner,
			renderer, depth);
	}

	protected override void DropProps(object component, IMemberAdapter memberInfoToSetObjectOnOwner,
		object owningObject, int depth = 0)
	{
		if (component is not Texture texture) return;

		if (ImGui.Button($"Reload##{texture.GUID}"))
		{
			ResourceManager.Instance.ReleaseResource(texture.GUID);
		}

		ImGui.SameLine();
		if (owningObject != null && ImGui.Button($"Remove##{texture.GUID}"))
		{
			memberInfoToSetObjectOnOwner.SetValue(owningObject, Guid.Empty);
		}

		var textureId = texture.TextureId;
		if (textureId == 0)
		{
			return;
		}

		IntPtr texturePtr = new IntPtr(textureId);

		DrawTexture(memberInfoToSetObjectOnOwner, texturePtr, imageSize,
			() => DropTarget<Texture>(owningObject, memberInfoToSetObjectOnOwner));

		ImGui.Text($"{texture.Width} x {texture.Height}");
	}
}