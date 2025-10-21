using System.Numerics;
using Editor;
using Silk.NET.OpenGL;

public class ScaleGizmo : GizmoBase
{
    private float LineLength  = 1.0f;
    private float CubeSize = 0.1f;
    private float LineThickness  = 0.02f;
    
    public override void Initialize(GL gl)
    {
        if (initialized) return;

        List<float> vertices = new List<float>();
        List<uint> indices = new List<uint>();

        uint currentIndex = 0;
        uint startIndex = 0;

        // X-Axis (Red)
        startIndex = (uint)indices.Count;
        AddScaleLine(vertices, indices, ref currentIndex,
            Vector3.Zero, new Vector3(LineLength, 0, 0),
            new Vector3(1, 0, 0));
        xAxisIndices = (startIndex, (uint)indices.Count - startIndex);

        // Y-Axis (Green)
        startIndex = (uint)indices.Count;
        AddScaleLine(vertices, indices, ref currentIndex,
            Vector3.Zero, new Vector3(0, LineLength, 0),
            new Vector3(0, 1, 0));
        yAxisIndices = (startIndex, (uint)indices.Count - startIndex);

        // Z-Axis (Blue)
        startIndex = (uint)indices.Count;
        AddScaleLine(vertices, indices, ref currentIndex,
            Vector3.Zero, new Vector3(0, 0, LineLength),
            new Vector3(0, 0, 1));
        zAxisIndices = (startIndex, (uint)indices.Count - startIndex);

        SetupBuffers(gl, vertices.ToArray(), indices.ToArray());
    }

    private void AddScaleLine(List<float> vertices, List<uint> indices, ref uint currentIndex,
        Vector3 start, Vector3 end, Vector3 color)
    {
        Vector3 direction = Vector3.Normalize(end - start);
        
        int segments = 8;
        Vector3 perpendicular1 = Vector3.Normalize(Vector3.Cross(direction,
            Math.Abs(direction.Y) > 0.9f ? new Vector3(1, 0, 0) : new Vector3(0, 1, 0)));
        Vector3 perpendicular2 = Vector3.Normalize(Vector3.Cross(direction, perpendicular1));

        uint lineStartIndex = currentIndex;
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * MathF.PI * 2.0f;
            Vector3 offset = (perpendicular1 * MathF.Cos(angle) + perpendicular2 * MathF.Sin(angle)) * LineThickness;
            Vector3 point = start + offset;
            vertices.AddRange(new[] { point.X, point.Y, point.Z, color.X, color.Y, color.Z });
            currentIndex++;
        }

        uint lineEndIndex = currentIndex;
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * MathF.PI * 2.0f;
            Vector3 offset = (perpendicular1 * MathF.Cos(angle) + perpendicular2 * MathF.Sin(angle)) * LineThickness;
            Vector3 point = end + offset;
            vertices.AddRange(new[] { point.X, point.Y, point.Z, color.X, color.Y, color.Z });
            currentIndex++;
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;

            indices.Add(lineStartIndex + (uint)i);
            indices.Add(lineEndIndex + (uint)i);
            indices.Add(lineStartIndex + (uint)next);

            indices.Add(lineStartIndex + (uint)next);
            indices.Add(lineEndIndex + (uint)i);
            indices.Add(lineEndIndex + (uint)next);
        }

        AddCube(vertices, indices, ref currentIndex, end, color);
    }

    private void AddCube(List<float> vertices, List<uint> indices, ref uint currentIndex,
        Vector3 center, Vector3 color)
    {
        float halfSize = CubeSize * 0.5f;
        
        Vector3[] cubeVertices = new Vector3[]
        {
            center + new Vector3(-halfSize, -halfSize, -halfSize),
            center + new Vector3(halfSize, -halfSize, -halfSize),
            center + new Vector3(halfSize, halfSize, -halfSize),
            center + new Vector3(-halfSize, halfSize, -halfSize),
            center + new Vector3(-halfSize, -halfSize, halfSize),
            center + new Vector3(halfSize, -halfSize, halfSize),
            center + new Vector3(halfSize, halfSize, halfSize),
            center + new Vector3(-halfSize, halfSize, halfSize)
        };

        uint baseIndex = currentIndex;
        foreach (var vertex in cubeVertices)
        {
            vertices.AddRange(new[] { vertex.X, vertex.Y, vertex.Z, color.X, color.Y, color.Z });
            currentIndex++;
        }

        uint[] cubeIndices = new uint[]
        {
            // Front face
            0, 1, 2, 0, 2, 3,
            // Back face
            5, 4, 7, 5, 7, 6,
            // Left face
            4, 0, 3, 4, 3, 7,
            // Right face
            1, 5, 6, 1, 6, 2,
            // Top face
            3, 2, 6, 3, 6, 7,
            // Bottom face
            4, 5, 1, 4, 1, 0
        };

        foreach (var index in cubeIndices)
        {
            indices.Add(baseIndex + index);
        }
    }
}