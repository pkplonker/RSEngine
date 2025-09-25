using System.Numerics;
using Engine;
using ImGuiNET;

namespace Editor.Controls;

public class HierarchyPanel : IPanel
{
	private readonly EditorImGuiController controller;
	private readonly IInputController inputController;
	private readonly RenamingHelper renamingHelper;
	private const string CONTEXT_MENU_NAME = "HierachyRightClickContextMenu";

	public HierarchyPanel(EditorImGuiController controller, IInputController inputController)
	{
		this.controller = controller;
		this.inputController = inputController;
		renamingHelper = new RenamingHelper();
	}

	public string PanelName { get; set; } = "Hierarchy";

	public void Draw(IRenderer renderer)
	{
		if (inputController.IsKeyHeld(IInputController.Key.F2) && controller.SelectedObject is GameObject go)
		{
			renamingHelper.RequestRename(go);
		}

		ImGui.Begin(PanelName);
		if (ImGui.IsMouseReleased(ImGuiMouseButton.Right) && ImGui.IsWindowHovered(ImGuiHoveredFlags.None))
		{
			ImGui.OpenPopup(CONTEXT_MENU_NAME);
		}

		if (ImGui.BeginPopup(CONTEXT_MENU_NAME))
		{
			if (SceneController.ActiveScene != null)
			{
				if (ImGui.BeginMenu("Add"))
				{
					if (ImGui.BeginMenu("Default Shapes"))
					{
						if (ImGui.MenuItem("Cube"))
						{
							GameObjectFactory.CreatePrimitive(SceneController.ActiveScene, PrimitiveType.Cube);
						}

						if (ImGui.MenuItem("Sphere"))
						{
							GameObjectFactory.CreatePrimitive(SceneController.ActiveScene, PrimitiveType.Sphere);
						}

						if (ImGui.MenuItem("Plane"))
						{
							GameObjectFactory.CreatePrimitive(SceneController.ActiveScene, PrimitiveType.Plane);
						}

						ImGui.EndMenu();
					}

					if (ImGui.MenuItem("New Empty"))
					{
						GameObjectFactory.CreatePrimitive(SceneController.ActiveScene);
					}

					if (ImGui.MenuItem("New Mesh"))
					{
						GameObjectFactory.CreateMesh(SceneController.ActiveScene);
					}

					if (ImGui.MenuItem("New Camera"))
					{
						GameObjectFactory.CreateCamera(SceneController.ActiveScene);
					}

					ImGui.EndMenu();
				}
			}
			else
			{
				ImGui.Text("No active scene");
			}

			ImGui.EndPopup();
		}

		var scene = SceneController.ActiveScene;
		if (scene != null)
		{
			DrawChildren(scene);

			renamingHelper.DrawRenamePopup();
		}

		ImGui.End();
	}

	private void DrawChildren(ITransform transform)
	{
		foreach (var gameobject in transform.ChildrenAsGameObjects)
		{
			DrawGameObjectNode(gameobject);
		}
	}

	private void DrawGameObjectNode(GameObject go)
	{
		ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow |
		                           ImGuiTreeNodeFlags.SpanAvailWidth;
		var selected = go == controller.SelectedObject;
		if (selected)
		{
			flags |= ImGuiTreeNodeFlags.Selected;
			ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.1764705926179886f, 0.3490196168422699f,
				0.5764706134796143f,
				0.8619999885559082f));
		}

		if (!go.Transform.HasChildren)
		{
			flags |= ImGuiTreeNodeFlags.Leaf;
		}

		bool nodeOpen = ImGui.TreeNodeEx($"{go.Name}##{go.Transform.GUID}", flags);

		if (ImGui.BeginPopupContextItem())
		{
			ImGui.Text($"{go.Name} - {go.Transform.GUID}");
			if (ImGui.MenuItem($"Rename##{go.Transform.GUID}"))
			{
				renamingHelper.RequestRename(go);
			}

			if (ImGui.MenuItem($"Delete##{go.Transform.GUID}"))
			{
				go.Transform.SetParent(null);
			}

			ImGui.EndPopup();
		}

		if (selected)
		{
			ImGui.PopStyleColor();
		}

		if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen())
		{
			HandleGameObjectSelection(go);
		}

		if (nodeOpen)
		{
			DrawChildren(go.Transform);
			ImGui.TreePop();
		}
	}

	private void HandleGameObjectSelection(GameObject go)
	{
		controller.SelectedObject = go;
	}
}