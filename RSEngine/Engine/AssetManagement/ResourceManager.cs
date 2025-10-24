using Editor;
using Engine.Logging;
using Silk.NET.OpenGL;

namespace Engine;

public class ResourceManager : IAssetManager
{
	public const string DEFAULT_SHADER = "unlit.glsl";
	public const string DEFAULT_SPHERE = "sphere.fbx";
	public const string DEFAULT_CUBE = "cube.fbx";
	public const string DEFAULT_PLANE = "plane.fbx";
	public const string DEFAULT_MATERIAL = "defaultmat.mat";

	private static readonly Lazy<ResourceManager> instance = new(() => new ResourceManager());
	private IAssetManager assetManager;

	public void Init(GL gl, string? directory)
	{
		ProjectManager.ProjectCreated += OnProjectCreated;
		ProjectManager.ProjectChanged += OnProjectChanged;

		assetManager = new UserResourceManager(gl, directory);
	}

	private void OnProjectCreated(IProject? obj)
	{
		if (obj == null)
		{
			Logger.Error("Project creation failed or project is null.");
			return;
		}

		string? sourceDirectory = "resources/coreassets/".MakeAbsolute();
		string targetDirectory = obj.CoreAssetsDirectory;

		try
		{
			CopyDirectory(sourceDirectory, targetDirectory);
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed to copy resources to project: {ex}");
		}

		FileImporter.ImportAllFromDirectory(targetDirectory);
		FileImporter.ImportAllFromDirectory(obj.AssetsDirectory);
	}

	private static void CopyDirectory(string? sourceDir, string targetDir)
	{
		if (!Directory.Exists(targetDir))
		{
			Directory.CreateDirectory(targetDir);
		}

		foreach (var file in Directory.GetFiles(sourceDir))
		{
			string targetFilePath = Path.Combine(targetDir, Path.GetFileName(file));
			File.Copy(file, targetFilePath, true);
		}

		foreach (var directory in Directory.GetDirectories(sourceDir))
		{
			string targetSubDir = Path.Combine(targetDir, Path.GetFileName(directory));
			CopyDirectory(directory, targetSubDir);
		}
	}

	private void OnProjectChanged(IProject? project)
	{
		ClearMetadatas();
		if (project != null)
		{
			LoadMetadata(project.AssetsDirectory);
			LoadMetadata(project.CoreAssetsDirectory);
		}
	}

	public static ResourceManager Instance => instance.Value;

	public void LoadMetadata(string? directory) => assetManager?.LoadMetadata(directory);

	public IEnumerable<string> GetFilesFromFolder(string? path, IEnumerable<string> ext = null) =>
		assetManager.GetFilesFromFolder(path, ext);

	public bool TryGetResourceByGuid<T>(Guid guid, out T result) where T : class?, IResource?
	{
		result = default;

		if (assetManager.TryGetResourceByGuid<T>(guid, out var resource))
		{
			if (resource is T)
			{
				result = resource;
				return true;
			}
		}

		return false;
	}

	public bool TryGetResourceByGuid(Guid guid, out IResource? result)
	{
		result = default;

		if (assetManager.TryGetResourceByGuid(guid, out IResource? resource))
		{
			result = resource;
			return true;
		}

		return false;
	}

	public bool AddMetaData(IMetadata metadata) => assetManager?.AddMetaData(metadata) ?? false;

	public IEnumerable<IMetadata> GetMetadata(MetadataType? filterType = null) =>
		assetManager?.GetMetadata(filterType) ?? Enumerable.Empty<IMetadata>();

	public bool MetadataExistsWithPath(string path) => assetManager.MetadataExistsWithPath(path);

	public void ClearMetadatas() => assetManager?.ClearMetadatas();
	public IMetadata? GetResourceByName(string name) => assetManager?.GetResourceByName(name);

	public void Save() => assetManager.Save();

	public IMetadata? GetMetadata(string path) => assetManager?.GetMetadata(path);

	public bool GuidIsType<T>(Guid guid) where T : class, IResource => TryGetResourceByGuid<T>(guid, out var _);

	public bool GetMetadata(Guid guid, out IMetadata? metadata)
	{
		metadata = null;
		if (assetManager?.GetMetadata(guid, out var md) ?? false)
		{
			metadata = md;
			return true;
		}

		return false;
	}

	public void ReleaseResource(Guid guid) => assetManager?.ReleaseResource(guid);
	public void ReleaseAll<T>() where T : class, IResource => assetManager?.ReleaseAll<T>();
	public void ReloadAll() => assetManager?.ReloadAll();

	public IMetadata? GetMetadata(Guid guid) => assetManager?.GetMetadata(guid);

	public Type? GetTypeFromGuid(Guid guid) => assetManager?.GetTypeFromGuid(guid);

	public IResource RegisterRuntimeResource(IMetadata meshMetadata, object resource)
	{
		return assetManager.RegisterRuntimeResource(meshMetadata, resource);
	}
}