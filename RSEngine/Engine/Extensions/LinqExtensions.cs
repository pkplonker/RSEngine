namespace Engine;

public static class LinqExtensions
{
	public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T> source)
	{
		return source.Where(item => item != null);
	}

	public static void Foreach<T>(this IEnumerable<T> source, Action<T> action)
	{
		foreach (var item in source)
		{
			if (item == null) continue;
			action(item);
		}
	}
	public static string RemoveSubstring(this string source, string toRemove)
	{
		if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(toRemove))
		{
			return source;
		}

		string lowerSource = source.ToLower();
		string lowerToRemove = toRemove.ToLower();
		int index = source.IndexOf(toRemove, StringComparison.OrdinalIgnoreCase);

		while (index != -1)
		{
			source = source.Remove(index, toRemove.Length);
			lowerSource = source.ToLower();
			index = lowerSource.IndexOf(lowerToRemove, StringComparison.Ordinal);
		}

		return source.Trim();
	}
}