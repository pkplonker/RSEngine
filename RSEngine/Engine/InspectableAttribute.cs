namespace Engine;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Struct)]
public class InspectableAttribute : Attribute
{
	public bool Show { get; set; } = true;
	public string? CustomName { get; set; } = null;

	public InspectableAttribute() { }

	public InspectableAttribute(bool show)
	{
		Show = show;
	}

	public InspectableAttribute(string customName)
	{
		CustomName = customName;
	}

	public InspectableAttribute(bool show, string customName)
	{
		Show = show;
		CustomName = customName;
	}
}