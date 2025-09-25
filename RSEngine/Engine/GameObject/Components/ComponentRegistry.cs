using System.Reflection;
using Engine;

public static class ComponentRegistry
{
	public static readonly Dictionary<string, Type> Components = new();

	static ComponentRegistry()
	{
		RegisterComponents();
	}

	private static void RegisterComponents()
	{
		foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
			         .Where(t => typeof(IComponent).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract))
		{
			var componentNameAttribute = type.GetCustomAttribute<ComponentNameAttribute>();
			var friendlyName = componentNameAttribute?.Name ?? type.Name;

			Components[friendlyName] = type;
		}
	}
}