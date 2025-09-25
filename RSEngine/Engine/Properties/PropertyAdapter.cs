using System.Reflection;

namespace Editor.Properties;

public struct PropertyAdapter : IMemberAdapter
{
	private readonly PropertyInfo property;

	public PropertyAdapter(PropertyInfo property)
	{
		this.property = property;
	}

	public object? GetValue(object component)
	{
		return property?.GetValue(component);
	}

	public void SetValue(object component, object value)
	{
		property?.SetValue(component, value);
	}

	public Type? MemberType => property?.PropertyType;
	public T? GetCustomAttribute<T>() where T : Attribute => property?.GetCustomAttribute<T>();

	public string Name => property.Name;
	public MemberInfo? GetMemberInfo() => property;
}
