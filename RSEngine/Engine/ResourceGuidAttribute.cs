namespace Engine;

public class ResourceGuidAttribute : Attribute
{
	public readonly Type ResourceGuidType;

	public ResourceGuidAttribute(Type resourceGuidType)
	{
		this.ResourceGuidType = resourceGuidType;
	}
}