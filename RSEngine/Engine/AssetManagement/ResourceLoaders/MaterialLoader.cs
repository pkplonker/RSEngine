using Engine.Logging;

namespace Engine;

public class MaterialLoader : IResourceLoader
{
    public IResource? Load(IMetadata metadata)
    {
        try
        {
            return (Material)ObjectSerializer.Deserialize(metadata.Path.MakeProjectAbsolute());
        }
        catch (Exception e)
        {
            Logger.Warning($"Failed to load material {e}");
            return null;
        }
    }
}