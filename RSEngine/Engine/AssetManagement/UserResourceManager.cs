using System.Collections.Concurrent;
using Editor;
using Engine.Logging;
using Silk.NET.OpenGL;

namespace Engine;

public class UserResourceManager : IAssetManager
{
	protected ConcurrentDictionary<Guid, IResource?> resources = new();
	protected ConcurrentDictionary<Guid, IMetadata> metadatas = new();
	protected Dictionary<MetadataType, IResourceLoader> loaders = new();
	protected Dictionary<MetadataType, Type> resourceTypes = new();

	protected GL gl;

	public UserResourceManager(GL gl, string? directory)
	{
		this.gl = gl;
		RegisterLoaders();
		LoadMetadata(directory);
	}

	private void RegisterLoaders()
	{
		RegisterLoader(MetadataType.Texture, new TextureLoader(gl), typeof(Texture));
		RegisterLoader(MetadataType.Material, new MaterialLoader(), typeof(Material));
		RegisterLoader(MetadataType.Shader, new ShaderLoader(gl), typeof(Shader));
		RegisterLoader(MetadataType.Mesh, new MeshLoader(gl), typeof(Mesh));
	}

	private void RegisterLoader(MetadataType type, IResourceLoader loader, Type resourceType)
	{
		loaders[type] = loader;
		resourceTypes[type] = resourceType;
	}

	public void LoadMetadata(string? root)
	{
		if (string.IsNullOrEmpty(root))
		{
			Logger.Warning("No valid directory to import metadata from");
			return;
		}

		var files = GetFilesFromFolder(root, new List<string>() {Metadata.MetadataFileExtension});
		int result = 0;
		foreach (var file in files)
		{
			var metaData = Metadata.CreateMetadataFromMetadataFile(file);
			if (metaData != null)
			{
				if (AddMetaData(metaData))
				{
					result++;
				}
			}
			else
			{
				Logger.Warning($"Failed to create metadata from {file}");
			}
		}

		Logger.Info($"Imported {result}/{files.Count()} metadata files");
	}

	public IEnumerable<string> GetFilesFromFolder(string? path, IEnumerable<string> ext = null)
	{
		IEnumerable<string> paths = Directory.GetFiles(path);

		if (ext != null && ext.Any())
		{
			paths = paths.Where(p => ext.Contains(Path.GetExtension(p).TrimStart('.').ToLowerInvariant()));
		}

		foreach (var dir in Directory.GetDirectories(path))
		{
			paths = paths.Concat(GetFilesFromFolder(dir, ext));
		}

		return paths;
	}

	public bool TryGetResourceByGuid<T>(Guid guid, out T result) where T : class, IResource
	{
		result = default(T);

		if (!metadatas.TryGetValue(guid, out var metadata))
		{
			return false;
		}

		var res = TryLoadResource(guid, metadata, out var resource);
		res &= resource?.GetType() == typeof(T);
		result = resource as T;
		return res;
	}

	public bool TryGetResourceByGuid(Guid guid, out IResource? result)
	{
		result = null;

		if (!metadatas.TryGetValue(guid, out var metadata))
		{
			return false;
		}

		return TryLoadResource(guid, metadata, out result);
	}

	private bool TryLoadResource(Guid guid, IMetadata metadata, out IResource? result)
	{
		result = null;

		if (!loaders.TryGetValue(metadata.MetadataType, out var loader))
		{
			Logger.Warning($"No loader registered for resource type {metadata.MetadataType}");
			return false;
		}

		if (!resources.TryGetValue(guid, out var resource))
		{
			resource = loader.Load(metadata);
			if (resource != null)
			{
				resources[guid] = resource;
			}
		}

		result = resource;
		return result != null;
	}

	public bool AddMetaData(IMetadata metadata)
	{
		if (metadatas.TryAdd(metadata.GUID, metadata))
		{
			return true;
		}

		Logger.Warning($"Duplicate metadata detected {metadata.GUID}");
		return false;
	}

	public IEnumerable<IMetadata> GetMetadata(MetadataType? filterType = null) =>
		metadatas.Values.Where(metadata => filterType == null || metadata.MetadataType == filterType);

	public bool MetadataExistsWithPath(string path) =>
		metadatas.Values.Any(m => m.Path.Equals(path, StringComparison.OrdinalIgnoreCase));

	public void ClearMetadatas()
	{
		metadatas.Clear();
	}

	public IMetadata? GetResourceByName(string name) =>
		metadatas.FirstOrDefault(x => x.Value.Path.Contains(name, StringComparison.OrdinalIgnoreCase)).Value;

	public void Save()
	{
		resources.WhereNotNull().Foreach(x => Save(x.Value));
	}

	public IMetadata? GetMetadata(string path)
	{
		var relative = Path.GetRelativePath(ProjectManager.ActiveProject.Directory, path);

		foreach (var md in metadatas.Values)
		{
			if (md.Path == relative)
			{
				return md;
			}
		}

		return null;
	}

	public bool GetMetadata(Guid guid, out IMetadata? metadata)
	{
		metadata = null;
		if (metadatas.TryGetValue(guid, out var md))
		{
			metadata = md;
			return true;
		}

		return false;
	}

	public void ReleaseResource(Guid guid)
	{
		resources.Remove(guid, out _);
	}

	public void ReleaseAll<T>() where T : class, IResource
	{
		var itemsToRemove = resources.Where(x => x is T).ToList();
		foreach (var item in itemsToRemove)
		{
			resources.Remove(item.Key, out _);
		}
	}

	public void ReloadAll()
	{
		resources.Clear();
	}

	public IMetadata? GetMetadata(Guid guid)
	{
		metadatas.TryGetValue(guid, out var md);
		return md;
	}

	public Type? GetTypeFromGuid(Guid guid)
	{
		metadatas.TryGetValue(guid, out var md);
		return md?.MetadataTypeAsType;
	}

	private void Save(IResource? resource)
	{
		// if (resource == null) return;
		// var guid = resource.GUID;
		// if (metadatas.TryGetValue(guid, out var metadata))
		// {
		// 	ObjectSerializer.Serialize(resource, metadata.Path.MakeProjectAbsolute());
		// }
	}

	public IResource RegisterRuntimeResource(IMetadata metadata, object resource)
	{
		if (resource is not IResource resourceInstance)
		{
			throw new ArgumentException("Resource parameter must implement IResource", nameof(resource));
		}

		if (!resourceTypes.TryGetValue(metadata.MetadataType, out var expectedType))
		{
			throw new ArgumentException($"Unknown metadata type {metadata.MetadataType}", nameof(metadata));
		}

		if (!expectedType.IsInstanceOfType(resource))
		{
			throw new ArgumentException(
				$"Resource type {resource.GetType().Name} does not match expected type {expectedType.Name} for metadata type {metadata.MetadataType}", 
				nameof(resource));
		}

		if (!metadatas.ContainsKey(metadata.GUID))
		{
			AddMetaData(metadata);
		}

		resources[metadata.GUID] = resourceInstance;

		return resourceInstance;
	}
}