namespace Engine;

public class CustomShaderArgs
{
    public IShader Shader { get; private set; }
    public Action? SetupCustomUniforms { get; private set; }

    public CustomShaderArgs(IShader shader, Action? setupCustomUniforms = null)
    {
        Shader = shader;
        SetupCustomUniforms = setupCustomUniforms;
    }
}