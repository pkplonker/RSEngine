using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine;

public class GizmoScene : IScene
{
    public bool Enabled { get; set; } = true;

    private IShader? gizmoShader;
    private uint vao, vbo, ebo;
    private bool initialized = false;
    private uint indexCount = 0;
    public float ArrowThickness { get; set; } = 0.1f;
    
    private static readonly GizmoSceneRenderer renderer = new GizmoSceneRenderer();

    public GizmoScene()
    {
        SceneID = IScene.GetNextId();
    }

    public string Name { get; set; }
    public ICamera? ActiveCamera { get; set; }

    public string Path { get; set; }

    public void RenderUsing(IRenderer renderer, IRenderPass renderPass, RenderPassData data)
    {
        GizmoSceneRenderer.Instance.RenderScene(this, renderer, renderPass, data);
    }

    public byte SceneID { get; }
    public IRenderable? ResolveSelection(PickedObject pickingObject)
    {
        throw new NotImplementedException();
    }

    private IShader? GizmoShader
    {
        get
        {
            if (gizmoShader == null)
            {
                var res = ResourceManager.Instance.GetResourceByName("Gizmo");
                if (res != null && ResourceManager.Instance.TryGetResourceByGuid<Shader>(res.GUID, out var gs))
                {
                    gizmoShader = gs;
                }
            }

            return gizmoShader;
        }
    }

    public unsafe void Initialize(GL gl)
    {
        if (initialized) return;

        List<float> vertices = new List<float>();
        List<uint> indices = new List<uint>();

        float arrowLength = 1.0f;
        float arrowHeadLength = 0.2f;
        float arrowHeadRadius = ArrowThickness;
        int coneSegments = 12;

        uint currentIndex = 0;

        // X-Axis (Red)
        AddArrow(vertices, indices, ref currentIndex,
            new Vector3(0, 0, 0), new Vector3(arrowLength, 0, 0),
            new Vector3(1, 0, 0),
            arrowHeadLength, arrowHeadRadius, coneSegments);

        // Y-Axis (Green)
        AddArrow(vertices, indices, ref currentIndex,
            new Vector3(0, 0, 0), new Vector3(0, arrowLength, 0),
            new Vector3(0, 1, 0),
            arrowHeadLength, arrowHeadRadius, coneSegments);

        // Z-Axis (Blue)
        AddArrow(vertices, indices, ref currentIndex,
            new Vector3(0, 0, 0), new Vector3(0, 0, arrowLength),
            new Vector3(0, 0, 1),
            arrowHeadLength, arrowHeadRadius, coneSegments);

        float[] quadVertices = vertices.ToArray();
        uint[] quadIndices = indices.ToArray();
        indexCount = (uint)quadIndices.Length;

        gl.GenVertexArrays(1, out vao);
        gl.GenBuffers(1, out vbo);
        gl.GenBuffers(1, out ebo);

        gl.BindVertexArray(vao);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        fixed (float* vertPtr = quadVertices)
        {
            gl.BufferData(BufferTargetARB.ArrayBuffer,
                (nuint)(quadVertices.Length * sizeof(float)),
                vertPtr, BufferUsageARB.StaticDraw);
        }

        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        fixed (uint* indPtr = quadIndices)
        {
            gl.BufferData(BufferTargetARB.ElementArrayBuffer,
                (nuint)(quadIndices.Length * sizeof(uint)),
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

    private void AddArrow(List<float> vertices, List<uint> indices, ref uint currentIndex,
        Vector3 start, Vector3 end, Vector3 color,
        float headLength, float headRadius, int segments)
    {
        Vector3 direction = Vector3.Normalize(end - start);
        float shaftLength = Vector3.Distance(start, end) - headLength;
        Vector3 shaftEnd = start + direction * shaftLength;

        float shaftRadius = headRadius * 0.3f;

        Vector3 perpendicular1 = Vector3.Normalize(Vector3.Cross(direction,
            Math.Abs(direction.Y) > 0.9f ? new Vector3(1, 0, 0) : new Vector3(0, 1, 0)));
        Vector3 perpendicular2 = Vector3.Normalize(Vector3.Cross(direction, perpendicular1));

        // ===== Arrow Shaft (Cylinder) =====
        uint shaftStartIndex = currentIndex;

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * MathF.PI * 2.0f;
            Vector3 offset = (perpendicular1 * MathF.Cos(angle) + perpendicular2 * MathF.Sin(angle)) * shaftRadius;
            Vector3 point = start + offset;
            vertices.AddRange(new[] { point.X, point.Y, point.Z, color.X, color.Y, color.Z });
            currentIndex++;
        }

        uint shaftEndIndex = currentIndex;
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * MathF.PI * 2.0f;
            Vector3 offset = (perpendicular1 * MathF.Cos(angle) + perpendicular2 * MathF.Sin(angle)) * shaftRadius;
            Vector3 point = shaftEnd + offset;
            vertices.AddRange(new[] { point.X, point.Y, point.Z, color.X, color.Y, color.Z });
            currentIndex++;
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;

            indices.Add(shaftStartIndex + (uint)i);
            indices.Add(shaftEndIndex + (uint)i);
            indices.Add(shaftStartIndex + (uint)next);

            indices.Add(shaftStartIndex + (uint)next);
            indices.Add(shaftEndIndex + (uint)i);
            indices.Add(shaftEndIndex + (uint)next);
        }

        // ===== Arrow Head (Cone) =====
        Vector3 coneTip = end;

        uint tipIndex = currentIndex;
        vertices.AddRange(new[] { coneTip.X, coneTip.Y, coneTip.Z, color.X, color.Y, color.Z });
        currentIndex++;

        uint coneBaseIndex = currentIndex;
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * MathF.PI * 2.0f;
            Vector3 offset = (perpendicular1 * MathF.Cos(angle) + perpendicular2 * MathF.Sin(angle)) * headRadius;
            Vector3 point = shaftEnd + offset;
            vertices.AddRange(new[] { point.X, point.Y, point.Z, color.X, color.Y, color.Z });
            currentIndex++;
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;

            indices.Add(tipIndex);
            indices.Add(coneBaseIndex + (uint)i);
            indices.Add(coneBaseIndex + (uint)next);
        }

        Vector3 baseCenter = shaftEnd;
        uint baseCenterIndex = currentIndex;
        vertices.AddRange(new[] { baseCenter.X, baseCenter.Y, baseCenter.Z, color.X, color.Y, color.Z });
        currentIndex++;

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;

            indices.Add(baseCenterIndex);
            indices.Add(coneBaseIndex + (uint)next);
            indices.Add(coneBaseIndex + (uint)i);
        }
    }
    public void SetParent(ITransformNode? newParent)
    {
    }

    internal unsafe void RenderGeometry(IRenderer renderer, RenderPassData data, IRenderPass renderPass)
    {
        if (!Enabled || GizmoShader == null) return;

        if (!initialized)
            Initialize(renderer.Gl);

        renderer.Gl.Disable(EnableCap.DepthTest);
        renderer.Gl.Enable(EnableCap.Blend);
        renderer.Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        renderer.UseShader(GizmoShader);
        
        GizmoShader.SetUniform("uView", data.View);
        GizmoShader.SetUniform("uProjection", data.Projection);
        GizmoShader.SetUniform("uModel", Matrix4x4.Identity);
        
        if (renderPass?.TargetType == RenderTargetType.Picking)
        {
           // todo
        }

        renderer.Gl.BindVertexArray(vao);
        renderer.Gl.DrawElements(Silk.NET.OpenGL.PrimitiveType.Triangles, indexCount, 
            DrawElementsType.UnsignedInt, null);

        renderer.Gl.Enable(EnableCap.DepthTest);
        renderer.Gl.Disable(EnableCap.Blend);
    }
    
}