namespace Engine;

public interface IAssetManager
{
	void LoadMetadata(string? directory);
	IEnumerable<string> GetFilesFromFolder(string? path, IEnumerable<string> ext = null);
	bool TryGetResourceByGuid<T>(Guid guid, out T result) where T : class, IResource;
	bool AddMetaData(IMetadata metadata);
	IEnumerable<IMetadata> GetMetadata(MetadataType? filterType = null);
	bool MetadataExistsWithPath(string path);
	void ClearMetadatas();
	IMetadata? GetResourceByName(string name);
	void Save();
	IMetadata? GetMetadata(string filterType);
	bool GetMetadata(Guid guid, out IMetadata metadata);
	void ReleaseResource(Guid guid);
	void ReleaseAll<T>() where T : class, IResource;
	void ReloadAll();

	IMetadata? GetMetadata(Guid guid);
	bool TryGetResourceByGuid(Guid guid, out IResource? result);
	Type? GetTypeFromGuid(Guid guid);
}