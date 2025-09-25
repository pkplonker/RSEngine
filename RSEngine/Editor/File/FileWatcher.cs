using Engine.Logging;

namespace Editor;

public class FileWatcher
{
	private readonly FileSystemWatcher fileSystemWatcher;

	public FileWatcher(string? path)
	{
		if (string.IsNullOrEmpty(path))
		{
			throw new ArgumentNullException(path);
		}
		fileSystemWatcher = new FileSystemWatcher(path)
		{
			IncludeSubdirectories = true,
			EnableRaisingEvents = true,
			NotifyFilter = NotifyFilters.Attributes
			               | NotifyFilters.CreationTime
			               | NotifyFilters.DirectoryName
			               | NotifyFilters.FileName
			               | NotifyFilters.LastAccess
			               | NotifyFilters.LastWrite
			               | NotifyFilters.Security
			               | NotifyFilters.Size,
		};
		fileSystemWatcher.Created += OnCreated;
		fileSystemWatcher.Deleted += OnDeleted;
		fileSystemWatcher.Renamed += OnRenamed;
		fileSystemWatcher.Error += OnError;
	}
	

	private static void OnCreated(object sender, FileSystemEventArgs e)
	{
	}

	private static void OnDeleted(object sender, FileSystemEventArgs e)
	{
		
	}

	private static void OnRenamed(object sender, RenamedEventArgs e)
	{
		
	}

	private static void OnError(object sender, ErrorEventArgs e) => Logger.Warning(e.GetException());
}