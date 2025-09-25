namespace Engine;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
public class SerializableAttribute : Attribute
{
	public bool Show { get; set; } = true;
	public string? CustomName { get; set; } = null;

	public SerializableAttribute() { }

	public SerializableAttribute(bool show)
	{
		Show = show;
	}

	public SerializableAttribute(string customName)
	{
		CustomName = customName;
	}

	public SerializableAttribute(bool show, string customName)
	{
		Show = show;
		CustomName = customName;
	}
}