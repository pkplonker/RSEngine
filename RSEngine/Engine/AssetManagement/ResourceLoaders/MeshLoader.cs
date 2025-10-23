using Engine.Logging;
using Silk.NET.OpenGL;

namespace Engine;

public class MeshLoader : IResourceLoader
{
    private readonly GL gl;
    public MeshLoader(GL gl) => this.gl = gl;

    public IResource? Load(IMetadata metadata)
    {
        try
        {
            return ModelLoader.LoadModel(gl, metadata.Path.MakeProjectAbsolute(), metadata.GUID);
        }
        catch (Exception e)
        {
            Logger.Warning($"Failed to load mesh {e}");
            return null;
        }
    }
}