using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine;

/// Debug render pass that renders scenes in wireframe mode with custom coloring
public class WireframeDebugPass : DebugRenderPass
{
    public override RenderTargetType TargetType => RenderTargetType.Debug;
    public override string Name => "Wireframe";
    
    public WireframeDebugPass() : base("Wireframe")
    {
    }
    
    protected override void ConfigureDebugRenderState(GL gl)
    {
        var color = 50 / 255f;
        gl.ClearColor(color, color, color, 1); 
        gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
        gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
    }
    
    protected override void SetupDebugUniforms()
    {
        if (debugShader != null)
        {
            debugShader.SetUniform("uWireframeColor", new Vector3(1, 1, 1));
        }
    }
}