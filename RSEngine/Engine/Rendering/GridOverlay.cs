using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine;

public class GridOverlay : ISceneOverlay
{
    public string Name => "Grid";
    public bool Enabled { get; set; } = true;

    private IShader? gridShader;
    private uint quadVAO, quadVBO, quadEBO;
    private bool initialized = false;

    public float GridScale { get; set; } = 1.0f;
    public Vector3 GridColor { get; set; } = new Vector3(255,255,255);
    public float FadeDistance { get; set; } = 0.01f;

    private IShader? GridShader
    {
        get
        {
            if (gridShader == null)
            {
                var res = ResourceManager.Instance.GetResourceByName("Grid");
                if (res != null && ResourceManager.Instance.TryGetResourceByGuid<Shader>(res.GUID, out var gs))
                {
                    gridShader = gs;
                }
            }

            return gridShader;
        }
    }

    public unsafe void Initialize(GL gl)
    {
        if (initialized) return;

        // Create a large quad for the infinite grid
        float[] quadVertices =
        {
            -1000.0f, 0.0f, -1000.0f, // Bottom left
            1000.0f, 0.0f, -1000.0f, // Bottom right
            1000.0f, 0.0f, 1000.0f, // Top right
            -1000.0f, 0.0f, 1000.0f // Top left
        };

        uint[] quadIndices =
        {
            0, 1, 2, // First triangle
            2, 3, 0 // Second triangle
        };

        gl.GenVertexArrays(1, out quadVAO);
        gl.GenBuffers(1, out quadVBO);
        gl.GenBuffers(1, out quadEBO);

        gl.BindVertexArray(quadVAO);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, quadVBO);
        fixed (float* vertPtr = quadVertices)
        {
            gl.BufferData(BufferTargetARB.ArrayBuffer,
                (nuint)(quadVertices.Length * sizeof(float)),
                vertPtr, BufferUsageARB.StaticDraw);
        }

        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, quadEBO);
        fixed (uint* indPtr = quadIndices)
        {
            gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                (nuint)(quadIndices.Length * sizeof(uint)),
                indPtr, BufferUsageARB.StaticDraw);
        }

        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false,
            3 * sizeof(float), (void*)0);
        gl.EnableVertexAttribArray(0);

        gl.BindVertexArray(0);
        initialized = true;
    }

    public void Render(IRenderer renderer, RenderPassData data)
    {
        if (!Enabled || GridShader == null) return;

        if (!initialized)
            Initialize(renderer.Gl);

        // Enable blending for grid transparency
        renderer.Gl.Enable(EnableCap.Blend);
        renderer.Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        renderer.Gl.DepthMask(false); // Don't write to depth buffer

        renderer.UseShader(GridShader);
        GridShader.SetUniform("uView", data.View);
        GridShader.SetUniform("uProjection", data.Projection);
        GridShader.SetUniform("uModel", Matrix4x4.Identity);
        GridShader.SetUniform("uGridColor", GridColor);
        GridShader.SetUniform("uGridScale", GridScale);
        GridShader.SetUniform("uFadeDistance", FadeDistance);

        renderer.Gl.BindVertexArray(quadVAO);
        unsafe
        {
            renderer.Gl.DrawElements(Silk.NET.OpenGL.PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        }


        // Restore state
        renderer.Gl.DepthMask(true);
        renderer.Gl.Disable(EnableCap.Blend);
    }

}