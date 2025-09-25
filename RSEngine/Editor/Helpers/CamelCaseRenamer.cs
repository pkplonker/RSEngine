using System.Text;
using Engine;

namespace Editor;

public static class CamelCaseRenamer
{
	private static Dictionary<string, string> nameCache = new Dictionary<string, string>();

	public static string GetFormattedName(this string camelCaseName)
	{
		if (string.IsNullOrEmpty(camelCaseName))
		{
			return camelCaseName;
		}

		if (nameCache.TryGetValue(camelCaseName, out string formattedName))
		{
			return formattedName;
		}

		camelCaseName = camelCaseName.RemoveSubstring("guid");
		formattedName = ConvertCamelCaseToProperCase(camelCaseName);

		nameCache[camelCaseName] = formattedName;

		return formattedName;
	}

	private static string ConvertCamelCaseToProperCase(this string name)
	{
		if (string.IsNullOrEmpty(name))
			return name;
		if (name == name.ToUpper()) return name;
		var result = new StringBuilder();
		result.Append(char.ToUpper(name[0]));

		for (var i = 1; i < name.Length; i++)
		{
			if (char.IsUpper(name[i]))
			{
				result.Append(' ');
			}

			result.Append(name[i]);
		}

		return result.ToString();
	}
}