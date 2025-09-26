using Silk.NET.OpenGL;

namespace Engine;

public class NormalsDebugPass : DebugRenderPass
{
    public override RenderTargetType TargetType => RenderTargetType.Debug;
    public override string Name => "Normals";
    public override string ShaderName => "NormalsDebug";
    
    protected override void ConfigureDebugRenderState(GL gl)
    {
        gl.ClearColor(0, 0, 0, 255); // Black background
    }
    
    protected override void SetupDebugUniforms(GameObject gameObject)
    {
        DebugShader?.SetUniform("uNormalScale", 0.1f); // Normal visualization scale
    }
}