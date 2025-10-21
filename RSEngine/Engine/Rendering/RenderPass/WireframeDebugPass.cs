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
        var color = 50 / 255f;
        gl.ClearColor(color,color,color,1); 
        gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
        gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
    }
    
    protected override void SetupDebugUniforms()
    {
        DebugShader?.SetUniform("uWireframeColor", new Vector3(1, 1, 1));
    }
}