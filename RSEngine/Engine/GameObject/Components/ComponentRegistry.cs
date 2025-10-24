using System.Reflection;

namespace Engine;

public static class ComponentRegistry
{
    public static Dictionary<string, Type> Components { get; } = new();

    static ComponentRegistry()
    {
        RegisterComponents();
    }

    private static void RegisterComponents()
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
                     .Where(t => typeof(IComponent).IsAssignableFrom(t) 
                                 && !t.IsInterface 
                                 && !t.IsAbstract
                                 && t.GetCustomAttribute<HideInInspectorAttribute>() == null))
        {
            var componentNameAttribute = type.GetCustomAttribute<ComponentNameAttribute>();
            var friendlyName = componentNameAttribute?.Name ?? type.Name;

            Components[friendlyName] = type;
        }
    }
}