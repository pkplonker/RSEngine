using Engine.Logging;
using Silk.NET.OpenGL;

namespace Engine;

public class ShaderLoader : IResourceLoader
{
    private readonly GL gl;
    public ShaderLoader(GL gl) => this.gl = gl;

    public IResource? Load(IMetadata metadata)
    {
        try
        {
            return new Shader(gl, metadata.Path.MakeProjectAbsolute(), metadata.GUID);
        }
        catch (Exception e)
        {
            Logger.Warning($"Failed to generate shader {e}");
            return null;
        }
    }
}