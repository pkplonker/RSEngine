using Engine;
using Engine.Logging;
using Newtonsoft.Json;

namespace Editor;

public static class FileTypeExtensions
{
	private static Dictionary<Type, IEnumerable<string>> extentionsDict = new();

	public static Type? GetTypeFromExtension(string ext) =>
		extentionsDict.FirstOrDefault(pair => pair.Value.Contains(ext.Trim('.'))).Key;

	private static Dictionary<string, Type> preloadedTypes = new Dictionary<string, Type>();

	private static void PreloadTypesFromAssemblies()
	{
		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			foreach (var type in assembly.GetTypes())
			{
				if (!preloadedTypes.ContainsKey(type.FullName))
				{
					preloadedTypes[type.FullName] = type;
				}
			}
		}
	}

	public static IEnumerable<string> GetAllExtensions()
	{
		return extentionsDict.Values.SelectMany(x => x);
	}

	static FileTypeExtensions()
	{
		PreloadTypesFromAssemblies();
		LoadExtensionsFromFile(FileExtensions.MakeAbsolute("resources/extensions.json"));
	}

	public static IEnumerable<string> GetExtensions(Type? type)
	{
		if (type == null)
		{
			return Enumerable.Empty<string>();
		}

		var name = type.Name.Split('.')[^1];
		if (extentionsDict.TryGetValue(type, out var extensions))
		{
			return extensions;
		}

		return Enumerable.Empty<string>();
	}

	private static void LoadExtensionsFromFile(string? filePath)
	{
		if (!File.Exists(filePath))
			throw new FileNotFoundException($"The file {filePath} was not found.");

		string json = File.ReadAllText(filePath);
		var data = JsonConvert.DeserializeObject<Dictionary<string, List<string>>>(json);
		if (data != null)
		{
			foreach (var kvp in data)
			{
				if (preloadedTypes.TryGetValue(kvp.Key, out var type))
				{
					extentionsDict[type] = kvp.Value.AsEnumerable();
				}
				else
				{
					Logger.Warning("Failed to convert name to type: " + kvp.Key);
				}
			}
		}
	}
}