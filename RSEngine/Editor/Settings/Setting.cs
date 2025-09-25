namespace Editor;

public class Setting
{
	public Type Type { get; set; }

	private object prop { get; set; }
	public string Name { get; set; }
	public string Category { get; set; }
	public bool Exposed { get; set; }

	public object Prop
	{
		get => prop;
		set
		{
			if (Type.IsInstanceOfType(value))
			{
				prop = value;
			}
			else
			{
				throw new ArgumentException($"Invalid type: {value.GetType().Name}. Expected: {Type.Name}.");
			}
		}
	}

	public Setting(string name, string category, Type type, bool exposed = true)
	{
		Name = name;
		Category = category;
		this.Type = type;
		Exposed = exposed;
	}

	public Setting() { }
}