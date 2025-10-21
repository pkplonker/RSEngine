using System.Numerics;
using Editor;
using Silk.NET.OpenGL;

public class TranslationGizmo : GizmoBase
{
    public float ArrowThickness { get; set; } = 0.1f;
    
    public override void Initialize(GL gl)
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

        SetupBuffers(gl, vertices.ToArray(), indices.ToArray());
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

        // Arrow Shaft (Cylinder)
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

        // Arrow Head (Cone)
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
}