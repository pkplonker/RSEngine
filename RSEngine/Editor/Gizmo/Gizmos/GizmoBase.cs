using System.Numerics;
using Engine;
using Silk.NET.OpenGL;
using PrimitiveType = Silk.NET.OpenGL.PrimitiveType;

namespace Editor;

public abstract class GizmoBase
{
    protected uint vao, vbo, ebo;
    protected bool initialized = false;
    protected uint indexCount = 0;
    
    protected (uint start, uint count) xAxisIndices;
    protected (uint start, uint count) yAxisIndices;
    protected (uint start, uint count) zAxisIndices;
    
    protected static readonly RenderID24 X_AXIS_ID = new RenderID24(1);
    protected static readonly RenderID24 Y_AXIS_ID = new RenderID24(2);
    protected static readonly RenderID24 Z_AXIS_ID = new RenderID24(3);

    public abstract void Initialize(GL gl);
    
    public unsafe void Render(GL gl, IShader shader, Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection, bool isPicking, byte sceneID)
    {
        if (!initialized) return;
        
        gl.BindVertexArray(vao);
        
        shader.SetUniform("uView", view);
        shader.SetUniform("uProjection", projection);
        shader.SetUniform("uModel", model);

        if (isPicking)
        {
            // X-Axis
            SetPickingColor(shader, X_AXIS_ID, sceneID);
            gl.DrawElements(PrimitiveType.Triangles,
                xAxisIndices.count,
                DrawElementsType.UnsignedInt,
                (void*)(xAxisIndices.start * sizeof(uint)));

            // Y-Axis
            SetPickingColor(shader, Y_AXIS_ID, sceneID);
            gl.DrawElements(PrimitiveType.Triangles,
                yAxisIndices.count,
                DrawElementsType.UnsignedInt,
                (void*)(yAxisIndices.start * sizeof(uint)));

            // Z-Axis
            SetPickingColor(shader, Z_AXIS_ID, sceneID);
            gl.DrawElements(PrimitiveType.Triangles,
                zAxisIndices.count,
                DrawElementsType.UnsignedInt,
                (void*)(zAxisIndices.start * sizeof(uint)));
        }
        else
        {
            gl.DrawElements(PrimitiveType.Triangles, indexCount,
                DrawElementsType.UnsignedInt, null);
        }
    }
    
    protected void SetPickingColor(IShader pickingShader, RenderID24 renderID, byte sceneID)
    {
        uint objectId = renderID.Value;
        uint packedId = ((uint)sceneID << 24) | objectId;

        float r = (packedId & 0xFF) / 255.0f;
        float g = ((packedId >> 8) & 0xFF) / 255.0f;
        float b = ((packedId >> 16) & 0xFF) / 255.0f;
        float a = ((packedId >> 24) & 0xFF) / 255.0f;

        pickingShader.SetUniform("uObjectColor", new Vector4(r, g, b, a));
    }
    
    protected unsafe void SetupBuffers(GL gl, float[] vertices, uint[] indices)
    {
        indexCount = (uint)indices.Length;

        gl.GenVertexArrays(1, out vao);
        gl.GenBuffers(1, out vbo);
        gl.GenBuffers(1, out ebo);

        gl.BindVertexArray(vao);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        fixed (float* vertPtr = vertices)
        {
            gl.BufferData(BufferTargetARB.ArrayBuffer,
                (nuint)(vertices.Length * sizeof(float)),
                vertPtr, BufferUsageARB.StaticDraw);
        }

        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        fixed (uint* indPtr = indices)
        {
            gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                (nuint)(indices.Length * sizeof(uint)),
                indPtr, BufferUsageARB.StaticDraw);
        }

        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false,
            6 * sizeof(float), (void*)0);
        gl.EnableVertexAttribArray(0);

        gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false,
            6 * sizeof(float), (void*)(3 * sizeof(float)));
        gl.EnableVertexAttribArray(1);

        gl.BindVertexArray(0);
        initialized = true;
    }
    
    public void Dispose(GL gl)
    {
        if (initialized)
        {
            gl.DeleteVertexArray(vao);
            gl.DeleteBuffer(vbo);
            gl.DeleteBuffer(ebo);
            initialized = false;
        }
    }
}