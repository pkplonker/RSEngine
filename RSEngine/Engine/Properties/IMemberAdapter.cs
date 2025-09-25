using System.Reflection;

namespace Editor.Properties;

public interface IMemberAdapter
{
	object? GetValue(object component);
	void SetValue(object component, object value);
	Type? MemberType { get; }
	T? GetCustomAttribute<T>() where T : Attribute;
	string? Name { get; }
	MemberInfo? GetMemberInfo();

}