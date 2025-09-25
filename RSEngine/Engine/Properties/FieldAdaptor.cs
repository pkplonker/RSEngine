using System.Reflection;

namespace Editor.Properties;

public struct FieldAdapter : IMemberAdapter
{
	private readonly FieldInfo field;

	public FieldAdapter(FieldInfo field)
	{
		this.field = field;
	}

	public object? GetValue(object component)
	{
		return field?.GetValue(component);
	}

	public void SetValue(object component, object value)
	{
		field?.SetValue(component, value);
	}

	public Type? MemberType => field.FieldType;
	public T? GetCustomAttribute<T>() where T : Attribute => field?.GetCustomAttribute<T>();

	public string? Name => field.Name;
	public MemberInfo? GetMemberInfo() => field;

}