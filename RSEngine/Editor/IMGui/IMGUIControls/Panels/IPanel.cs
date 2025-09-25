using Engine;
using ImGuiNET;

namespace Editor.Controls;

public interface IPanel
{
	string PanelName { get; protected set; }

	public void Draw(IRenderer renderer);

	
}