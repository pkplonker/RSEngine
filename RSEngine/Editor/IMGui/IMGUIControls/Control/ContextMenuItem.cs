namespace Editor.Controls;

public class ContextMenuItem
{
	public string Name { get; set; }

	public Action Action { get; set; }

	public ContextMenuItem(string name, Action action)
	{
		this.Name = name;
		this.Action = action;
	}
}