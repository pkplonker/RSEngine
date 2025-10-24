using Engine.Logging;
using Silk.NET.OpenGL;

namespace Engine;

public class TextureLoader : IResourceLoader
{
    private readonly GL gl;
    public TextureLoader(GL gl) => this.gl = gl;

    public IResource? Load(IMetadata metadata)
    {
        try
        {
            return new Texture(gl, metadata.Path.MakeProjectAbsolute(), metadata.GUID);
        }
        catch (Exception e)
        {
            Logger.Warning($"Failed to load texture {e}");
            return null;
        }
    }
}