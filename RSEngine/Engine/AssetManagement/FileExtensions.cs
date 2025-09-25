using Editor;
using Engine.Logging;

namespace Engine;

public static class FileExtensions
{
	public static string MakeProjectAbsolute(this string relativePath)
	{
		var basePath = ProjectManager.ActiveProject?.Directory;

		if (string.IsNullOrWhiteSpace(relativePath) || string.IsNullOrEmpty(basePath))
		{
			Logger.Warning("Failed to generate relativePath");
			return string.Empty;
		}

		relativePath = relativePath.Trim('/');

		string? absolutePath = Path.IsPathFullyQualified(relativePath) ? relativePath : string.Empty;
		if (!Path.IsPathFullyQualified(relativePath) && relativePath != null && basePath != null)
		{
			absolutePath = Path.Combine(basePath, relativePath);
		}

		return absolutePath;
	}

	public static string? MakeAbsolute(this string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return string.Empty;
		}

		path = path.Trim('/');

		if (!Path.IsPathFullyQualified(path))
		{
			path = Path.GetFullPath(path);
		}

		return path;
	}

	public static string MakeProjectRelative(this string path)
	{
		return ProjectManager.ActiveProject?.Directory != null ? Path.GetRelativePath(ProjectManager.ActiveProject?.Directory, path) : string.Empty;
	}
}