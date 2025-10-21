using System.Numerics;
using Editor;
using Silk.NET.OpenGL;

public class RotationGizmo : GizmoBase
{
    public float CircleRadius { get; set; } = 1.0f;
    public float CircleThickness { get; set; } = 0.02f;
    
    public override void Initialize(GL gl)
    {
        if (initialized) return;

        List<float> vertices = new List<float>();
        List<uint> indices = new List<uint>();

        int segments = 64;
        uint currentIndex = 0;
        uint startIndex = 0;

        // X-Axis Circle (Red) - in YZ plane
        startIndex = (uint)indices.Count;
        AddCircle(vertices, indices, ref currentIndex,
            Vector3.UnitX, new Vector3(1, 0, 0), segments);
        xAxisIndices = (startIndex, (uint)indices.Count - startIndex);

        // Y-Axis Circle (Green) - in XZ plane
        startIndex = (uint)indices.Count;
        AddCircle(vertices, indices, ref currentIndex,
            Vector3.UnitY, new Vector3(0, 1, 0), segments);
        yAxisIndices = (startIndex, (uint)indices.Count - startIndex);

        // Z-Axis Circle (Blue) - in XY plane
        startIndex = (uint)indices.Count;
        AddCircle(vertices, indices, ref currentIndex,
            Vector3.UnitZ, new Vector3(0, 0, 1), segments);
        zAxisIndices = (startIndex, (uint)indices.Count - startIndex);

        SetupBuffers(gl, vertices.ToArray(), indices.ToArray());
    }

    private void AddCircle(List<float> vertices, List<uint> indices, ref uint currentIndex,
        Vector3 normal, Vector3 color, int segments)
    {
        Vector3 tangent1 = Vector3.Normalize(Vector3.Cross(normal,
            Math.Abs(normal.Y) > 0.9f ? new Vector3(1, 0, 0) : new Vector3(0, 1, 0)));
        Vector3 tangent2 = Vector3.Normalize(Vector3.Cross(normal, tangent1));

        uint innerRingStart = currentIndex;
        uint outerRingStart = (uint)(currentIndex + segments);

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * MathF.PI * 2.0f;
            Vector3 circlePoint = (tangent1 * MathF.Cos(angle) + tangent2 * MathF.Sin(angle)) * CircleRadius;
            
            Vector3 radialDir = Vector3.Normalize(circlePoint);
            Vector3 innerPoint = circlePoint - radialDir * CircleThickness;
            
            vertices.AddRange(new[] { innerPoint.X, innerPoint.Y, innerPoint.Z, color.X, color.Y, color.Z });
            currentIndex++;
        }

        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * MathF.PI * 2.0f;
            Vector3 circlePoint = (tangent1 * MathF.Cos(angle) + tangent2 * MathF.Sin(angle)) * CircleRadius;
            
            Vector3 radialDir = Vector3.Normalize(circlePoint);
            Vector3 outerPoint = circlePoint + radialDir * CircleThickness;
            
            vertices.AddRange(new[] { outerPoint.X, outerPoint.Y, outerPoint.Z, color.X, color.Y, color.Z });
            currentIndex++;
        }

        for (int i = 0; i < segments; i++)
        {
            int next = (i + 1) % segments;

            indices.Add(innerRingStart + (uint)i);
            indices.Add(outerRingStart + (uint)i);
            indices.Add(innerRingStart + (uint)next);

            indices.Add(innerRingStart + (uint)next);
            indices.Add(outerRingStart + (uint)i);
            indices.Add(outerRingStart + (uint)next);
        }
    }
}
