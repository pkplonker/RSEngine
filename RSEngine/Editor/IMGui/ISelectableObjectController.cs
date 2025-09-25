using Engine;

namespace Editor;

public interface ISelectableObjectController
{
	event Action<IInspectable?> GameObjectSelectionChanged;
	IInspectable? SelectedObject { get; set; }
}