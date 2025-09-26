using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine;

public class WireframeDebugPass : DebugRenderPass
{
    public override RenderTargetType TargetType => RenderTargetType.Debug;
    public override string Name => "Wireframe";
    public override string ShaderName => "Wireframe";
    
    protected override void ConfigureDebugRenderState(GL gl)
    {
        gl.ClearColor(50, 50, 50, 255); 
        gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
    }
    
    protected override void SetupDebugUniforms(GameObject gameObject)
    {
        DebugShader?.SetUniform("uWireframeColor", new Vector3(0, 1, 0));
    }
}