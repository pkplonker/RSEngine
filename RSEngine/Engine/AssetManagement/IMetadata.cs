namespace Engine;

public interface IMetadata
{
	string Path { get; set; }
	Guid GUID { get; set; }
	int MetaDataVersion { get; set; }
	MetadataType MetadataType { get; set; }
	string Name { get; }
	string? AbsolutePath { get; }
	Type? MetadataTypeAsType { get; }
	bool Equals(object obj);
	int GetHashCode();
	void Serialize();
}