namespace Engine;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class ComponentNameAttribute : Attribute
{
	public string Name { get; }

	public ComponentNameAttribute(string name)
	{
		Name = name;
	}
}