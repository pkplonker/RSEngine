using System.Numerics;
using Engine;

namespace Editor;

public class GizmoAxisRenderable : IGizmoRenderable
{
    private readonly GizmoAxis axis;

    public GizmoAxisRenderable(GizmoAxis axis)
    {
        this.axis = axis;
    }

    public GizmoAxis Axis => axis;

    public void Render(IRenderer renderer, RenderPassData data, CustomShaderArgs customShaderArgs = null)
    {
        // This is just a marker for selection, actual rendering is done by GizmoScene
    }

    public RenderID24 RenderID
    {
        get => axis switch
        {
            GizmoAxis.X => new RenderID24(1),
            GizmoAxis.Y => new RenderID24(2),
            GizmoAxis.Z => new RenderID24(3),
            _ => new RenderID24(0)
        };
        set { }
    }

    public IShader? Shader => null;
    public Matrix4x4 ModelMatrix => Matrix4x4.Identity;

    public override string ToString()
    {
        return $"Gizmo {axis}-Axis";
    }
}