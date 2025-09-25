using System.Reflection;
using Engine;

namespace Editor;

public static class CustomEditorLoader
{
	public static Dictionary<Type, Type> customEditors { get; private set; } = new();

	static CustomEditorLoader()
	{
		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			foreach (var type in assembly.GetTypes())
			{
				var customEditorAttribute = type.GetCustomAttribute<CustomEditorAttribute>();
				if (!typeof(ICustomEditor).IsAssignableFrom(type))
				{
					continue;
				}

				if (customEditorAttribute != null)
				{
					customEditors[customEditorAttribute.TargetType] = type;
				}
			}
		}
	}

	public static bool TryGetEditor(Type targetType, out ICustomEditor editor)
	{
		if (customEditors.TryGetValue(targetType, out Type editorType))
		{
			editor = Activator.CreateInstance(editorType) as ICustomEditor;
			return editor != null;
		}

		editor = null;
		return false;
	}
}