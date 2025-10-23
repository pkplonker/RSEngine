namespace Engine;

public interface IResourceLoader
{
    IResource? Load(IMetadata metadata);
}