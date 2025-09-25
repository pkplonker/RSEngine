using System.Linq.Expressions;
using System.Numerics;
using Editor.IMGUIControls;
using Editor.Properties;
using Engine;
using Engine.Logging;
using ImGuiNET;
using Silk.NET.OpenGL;
using Shader = Engine.Shader;

namespace Editor.Controls;

[CustomEditor(typeof(Engine.GameObject))]
public class GameobjectCustomEditor : BaseCustomEditor
{
	public override void Draw(object component, object owningObject, IMemberAdapter memberInfoToSetObjectOnOwner,
		IRenderer renderer, int depth = 0)
	{
		propertyDrawer ??= new PropertyDrawer(renderer);
		Draw<GameObject>(component, owningObject, memberInfoToSetObjectOnOwner,
			renderer, depth);
	}

	protected override void Draw<T>(object component, object owningObject, IMemberAdapter memberInfoToSetObjectOnOwner,
		IRenderer renderer, int depth = 0) where T : class
	{
		propertyDrawer ??= new PropertyDrawer(renderer);
		var dropTarget = () => DropTarget<T>(owningObject, memberInfoToSetObjectOnOwner);
		if (component != null)
		{
			DropProps(component as T, memberInfoToSetObjectOnOwner, owningObject, depth);
		}
		else
		{
			DrawEmptyContent(memberInfoToSetObjectOnOwner, dropTarget);
		}
	}

	const string ADD_COMPONENT_POPUP_NAME = "AddComponentPopup";

	protected override void DropProps(object component, IMemberAdapter memberInfoToSetObjectOnOwner,
		object owningObject,
		int depth = 0)
	{
		if (component is not GameObject go) return;

		UndoableImGui.UndoableCheckbox("##Enabled", "GameObject Enabled Toggled", () => go.Enabled,
			val => go.Enabled = val
		);
		ImGui.SameLine();
		var guid = go.Transform.GUID.ToString();
		ImGui.Text($"{go.Name}: {guid}");
		if (ImGuiHelpers.CenteredButton("Add Component"))
		{
			ImGui.OpenPopup(ADD_COMPONENT_POPUP_NAME);
		}

		ImGuiHelpers.DrawTransform(go.Transform);

		if (ImGui.BeginPopup(ADD_COMPONENT_POPUP_NAME))
		{
			foreach (var kvp in ComponentRegistry.Components.OrderBy(x => x.Key))

			{
				if (!ImGui.Selectable(kvp.Key)) continue;
				var cachedGameObject = go;

				UndoManager.RecordAndPerform(
					new Memento(() => go.AddComponent(kvp.Value),
						() =>
						{
							if (cachedGameObject != null)
							{
								cachedGameObject.RemoveComponent(kvp.Value);
							}
						}, $"Added component - {kvp.Key}"));
				ImGui.CloseCurrentPopup();
			}

			ImGui.EndPopup();
		}

		foreach (var comp in go.GetComponents())
		{
			propertyDrawer.DrawObject(comp);
		}
	}
}