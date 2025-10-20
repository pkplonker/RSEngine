using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine;

public class GizmoScene : IScene
{
    public bool Enabled { get; set; } = true;

    private IShader? gizmoShader;
    private IShader? pickingShader;
    private uint vao, vbo, ebo;
    private bool initialized = false;
    private uint indexCount = 0;
    public float ArrowThickness { get; set; } = 0.1f;

    // Picking IDs for each axis
    private static readonly RenderID24 X_AXIS_ID = new RenderID24(1);
    private static readonly RenderID24 Y_AXIS_ID = new RenderID24(2);
    private static readonly RenderID24 Z_AXIS_ID = new RenderID24(3);

    // Store index ranges for each arrow
    private (uint start, uint count) xAxisIndices;
    private (uint start, uint count) yAxisIndices;
    private (uint start, uint count) zAxisIndices;

    private static readonly GizmoSceneRenderer renderer = new GizmoSceneRenderer();

    public GizmoScene()
    {
        SceneID = IScene.GetNextId();
    }

    public string Name { get; set; } = "Gizmo Scene";
    public ICamera? ActiveCamera { get; set; }
    public string Path { get; set; }

    public void RenderUsing(IRenderer renderer, IRenderPass renderPass, RenderPassData data)
    {
        GizmoSceneRenderer.Instance.RenderScene(this, renderer, renderPass, data);
    }

    public byte SceneID { get; }

    public IRenderable? ResolveSelection(PickedObject pickingObject)
    {
        // Return a wrapped renderable based on which axis was picked
        if (pickingObject.ObjectId == X_AXIS_ID.Value)
        {
            return new GizmoAxisRenderable(this, GizmoAxis.X);
        }
        else if (pickingObject.ObjectId == Y_AXIS_ID.Value)
        {
            return new GizmoAxisRenderable(this, GizmoAxis.Y);
        }
        else if (pickingObject.ObjectId == Z_AXIS_ID.Value)
        {
            return new GizmoAxisRenderable(this, GizmoAxis.Z);
        }

        return null;
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

    private IShader? PickingShader
    {
        get
        {
            if (pickingShader == null)
            {
                var res = ResourceManager.Instance.GetResourceByName("Picking");
                if (res != null && ResourceManager.Instance.TryGetResourceByGuid<Shader>(res.GUID, out var ps))
                {
                    pickingShader = ps;
                }
            }

            return pickingShader;
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
        uint startIndex = 0;

        // X-Axis (Red)
        startIndex = (uint)indices.Count;
        AddArrow(vertices, indices, ref currentIndex,
            new Vector3(0, 0, 0), new Vector3(arrowLength, 0, 0),
            new Vector3(1, 0, 0),
            arrowHeadLength, arrowHeadRadius, coneSegments);
        xAxisIndices = (startIndex, (uint)indices.Count - startIndex);

        // Y-Axis (Green)
        startIndex = (uint)indices.Count;
        AddArrow(vertices, indices, ref currentIndex,
            new Vector3(0, 0, 0), new Vector3(0, arrowLength, 0),
            new Vector3(0, 1, 0),
            arrowHeadLength, arrowHeadRadius, coneSegments);
        yAxisIndices = (startIndex, (uint)indices.Count - startIndex);

        // Z-Axis (Blue)
        startIndex = (uint)indices.Count;
        AddArrow(vertices, indices, ref currentIndex,
            new Vector3(0, 0, 0), new Vector3(0, 0, arrowLength),
            new Vector3(0, 0, 1),
            arrowHeadLength, arrowHeadRadius, coneSegments);
        zAxisIndices = (startIndex, (uint)indices.Count - startIndex);

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

    internal unsafe void RenderGeometry(IRenderer renderer, RenderPassData data, IRenderPass renderPass)
    {
        if (!Enabled) return;

        if (!initialized)
            Initialize(renderer.Gl);

        renderer.Gl.BindVertexArray(vao);

        if (renderPass?.TargetType == RenderTargetType.Picking)
        {
            // Picking pass - render each axis with a unique ID
            if (PickingShader == null) return;

            renderer.UseShader(PickingShader);
            PickingShader.SetUniform("uView", data.View);
            PickingShader.SetUniform("uProjection", data.Projection);
            PickingShader.SetUniform("uModel", Matrix4x4.Identity);

            // X-Axis
            SetPickingColor(PickingShader, X_AXIS_ID, SceneID);
            renderer.Gl.DrawElements(Silk.NET.OpenGL.PrimitiveType.Triangles,
                xAxisIndices.count,
                DrawElementsType.UnsignedInt,
                (void*)(xAxisIndices.start * sizeof(uint)));

            // Y-Axis
            SetPickingColor(PickingShader, Y_AXIS_ID, SceneID);
            renderer.Gl.DrawElements(Silk.NET.OpenGL.PrimitiveType.Triangles,
                yAxisIndices.count,
                DrawElementsType.UnsignedInt,
                (void*)(yAxisIndices.start * sizeof(uint)));

            // Z-Axis
            SetPickingColor(PickingShader, Z_AXIS_ID, SceneID);
            renderer.Gl.DrawElements(Silk.NET.OpenGL.PrimitiveType.Triangles,
                zAxisIndices.count,
                DrawElementsType.UnsignedInt,
                (void*)(zAxisIndices.start * sizeof(uint)));
        }
        else
        {
            // Normal rendering - render all at once with colors
            if (GizmoShader == null) return;

            renderer.Gl.Disable(EnableCap.DepthTest);
            renderer.Gl.Enable(EnableCap.Blend);
            renderer.Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            renderer.UseShader(GizmoShader);
            GizmoShader.SetUniform("uView", data.View);
            GizmoShader.SetUniform("uProjection", data.Projection);
            GizmoShader.SetUniform("uModel", Matrix4x4.Identity);

            renderer.Gl.DrawElements(Silk.NET.OpenGL.PrimitiveType.Triangles, indexCount,
                DrawElementsType.UnsignedInt, null);

            renderer.Gl.Enable(EnableCap.DepthTest);
            renderer.Gl.Disable(EnableCap.Blend);
        }
    }

    private void SetPickingColor(IShader pickingShader, RenderID24 renderID, byte sceneID)
    {
        uint objectId = renderID.Value;

        // Pack scene ID and object ID the same way as GameObjects
        uint packedId = ((uint)sceneID << 24) | objectId;

        float r = (packedId & 0xFF) / 255.0f;
        float g = ((packedId >> 8) & 0xFF) / 255.0f;
        float b = ((packedId >> 16) & 0xFF) / 255.0f;
        float a = ((packedId >> 24) & 0xFF) / 255.0f;

        pickingShader.SetUniform("uObjectColor", new Vector4(r, g, b, a));
    }
}

public enum GizmoAxis
{
    X,
    Y,
    Z
}

public class GizmoAxisRenderable : IRenderable
{
    private readonly GizmoScene gizmoScene;
    private readonly GizmoAxis axis;

    public GizmoAxisRenderable(GizmoScene gizmoScene, GizmoAxis axis)
    {
        this.gizmoScene = gizmoScene;
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