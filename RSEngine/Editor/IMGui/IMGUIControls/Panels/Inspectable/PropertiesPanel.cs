using Engine;
using ImGuiNET;

namespace Editor.Controls;

public class PropertiesPanel : IPanel
{
	private IPropertyDrawer? propertyDrawer;
	private IInspectable? currentSelected;
	private bool newChange = false;
	private bool isLocked;

	private bool IsLocked
	{
		get => isLocked;
		set
		{
			if (value != isLocked)
			{
				isLocked = value;
				if (!isLocked)
				{
					currentSelected = requestedSelected;
				}
			}
		}
	}

	private IInspectable? requestedSelected;
	public event Action<IInspectable?>? SelectionChanged;

	public PropertiesPanel(ISelectableObjectController controller)
	{
		controller.GameObjectSelectionChanged += ControllerOnGameObjectSelectionChanged;
		SceneController.OnActiveSceneChanged += (_, _) => { currentSelected = null; };
	}

	private void ControllerOnGameObjectSelectionChanged(IInspectable? obj)
	{
		requestedSelected = obj;
		if (!IsLocked)
		{
			currentSelected = requestedSelected;
		}

		SelectionChanged?.Invoke(obj);
		newChange = true;
	}

	public string PanelName { get; set; } = "Inspector";

	public void Draw(IRenderer renderer)
	{
		ImGui.Begin(PanelName);
		ImGui.Text("Lock");
		ImGui.SameLine();
		var lockedLocal = IsLocked;
		if (ImGui.Checkbox("##lock slider", ref lockedLocal))
		{
			IsLocked = lockedLocal;
		}

		if (newChange)
		{
			newChange = false;
			ImGui.SetScrollY(0);
		}

		propertyDrawer ??= new PropertyDrawer(renderer);

		if (currentSelected == null) return;
		if (CustomEditorLoader.TryGetEditor(currentSelected.GetType(), out var editor))
		{
			editor.Draw(currentSelected, null, null, renderer);
		}
		else
		{
			propertyDrawer.DrawObject(currentSelected);
		}

		ImGui.End();
	}
}