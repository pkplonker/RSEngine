using Engine;

namespace Editor.Controls;

public interface IInspectableStrategy
{
	public void Draw(IInspectable obj, IPropertyDrawer? propertyDrawer);
}