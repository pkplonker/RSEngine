using System.Numerics;
using Engine.AssetManagement;
using Engine.Logging;
using Silk.NET.OpenGL;

namespace Engine;

[ComponentName("Mesh Filter")]
public class MeshFilter : Component
{
    [Inspectable(false)]
    [Serializable]
    [ResourceGuid(typeof(Mesh))]
    public HashSet<Guid> meshes { get; set; } = new();

    public MeshFilter(GameObject gameObject) : base(gameObject)
    {
    }

    public void AddMesh(Guid guid)
    {
        meshes.Add(guid);
    }

    public MeshFilter Clone(MeshFilter newMeshFilter)
    {
        foreach (var mesh in meshes)
        {
            newMeshFilter.meshes.Clear();
            newMeshFilter.AddMesh(mesh);
        }

        return newMeshFilter;
    }

    public void UpdateMesh(GL gl,List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<uint> indices,
        int meshIndex = 0)
    {
        if (meshes.Count == 0)
        {
            CreateMesh(gl,vertices, normals, uvs, indices);
        }
        else if (meshIndex < meshes.Count)
        {
            UpdateMesh(gl,meshes.ElementAt(meshIndex), vertices, normals, uvs, indices);
        }
        else
        {
            Logger.Error("Invalid mesh index");
        }
    }

    public void UpdateMesh(GL gl,Guid meshGuid, List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs,
        List<uint> indices)
    {
        if (!ResourceManager.Instance.TryGetResourceByGuid<Mesh>(meshGuid, out var meshResource))
        {
            Logger.Error($"Mesh with GUID {meshGuid} not found");
            return;
        }

        var tangents = CalculateTangents(vertices, normals, uvs, indices);

        var vertexArray = BuildVertices(vertices, normals, tangents, uvs);
        var indexArray = indices.ToArray();

        meshResource.UpdateMeshData(vertexArray, indexArray);
    }

    private void CreateMesh(GL gl,List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<uint> indices)
    {
        var tangents = CalculateTangents(vertices, normals, uvs, indices);

        var vertexArray = BuildVertices(vertices, normals, tangents, uvs);
        var indexArray = indices.ToArray();

        var metaData = new MeshMetadata();
        var mesh = MeshCreator.Create(gl, vertexArray, indexArray, metaData.GUID);

        var resource = ResourceManager.Instance.RegisterRuntimeResource(metaData, mesh);
        meshes.Add(resource.GUID);
    }

    private float[] BuildVertices(List<Vector3> positions, List<Vector3> normals, List<Vector3> tangents,
        List<Vector2> uvs)
    {
        if (positions.Count != normals.Count || positions.Count != tangents.Count || positions.Count != uvs.Count)
        {
            throw new ArgumentException("Position, normal, tangent, and UV lists must have the same length");
        }

        var vertices = new List<float>(positions.Count * 14);

        for (int i = 0; i < positions.Count; i++)
        {
            // Position (3 floats)
            vertices.Add(positions[i].X);
            vertices.Add(positions[i].Y);
            vertices.Add(positions[i].Z);

            // Normal (3 floats)
            vertices.Add(normals[i].X);
            vertices.Add(normals[i].Y);
            vertices.Add(normals[i].Z);

            // Tangent (3 floats)
            vertices.Add(tangents[i].X);
            vertices.Add(tangents[i].Y);
            vertices.Add(tangents[i].Z);

            // UV (2 floats)
            vertices.Add(uvs[i].X);
            vertices.Add(uvs[i].Y);

            // Bitangent (3 floats) - Calculate from normal and tangent
            var bitangent = Vector3.Normalize(Vector3.Cross(normals[i], tangents[i]));
            vertices.Add(bitangent.X);
            vertices.Add(bitangent.Y);
            vertices.Add(bitangent.Z);
        }

        return vertices.ToArray();
    }

    private List<Vector3> CalculateTangents(List<Vector3> positions, List<Vector3> normals, List<Vector2> uvs,
        List<uint> indices)
    {
        var tangents = new List<Vector3>(new Vector3[positions.Count]);

        for (int i = 0; i < indices.Count; i += 3)
        {
            uint i0 = indices[i];
            uint i1 = indices[i + 1];
            uint i2 = indices[i + 2];

            Vector3 pos0 = positions[(int)i0];
            Vector3 pos1 = positions[(int)i1];
            Vector3 pos2 = positions[(int)i2];

            Vector2 uv0 = uvs[(int)i0];
            Vector2 uv1 = uvs[(int)i1];
            Vector2 uv2 = uvs[(int)i2];

            Vector3 edge1 = pos1 - pos0;
            Vector3 edge2 = pos2 - pos0;
            Vector2 deltaUV1 = uv1 - uv0;
            Vector2 deltaUV2 = uv2 - uv0;

            float denominator = deltaUV1.X * deltaUV2.Y - deltaUV2.X * deltaUV1.Y;
            float f = Math.Abs(denominator) > float.Epsilon ? 1.0f / denominator : 0.0f;

            Vector3 tangent = new Vector3(
                f * (deltaUV2.Y * edge1.X - deltaUV1.Y * edge2.X),
                f * (deltaUV2.Y * edge1.Y - deltaUV1.Y * edge2.Y),
                f * (deltaUV2.Y * edge1.Z - deltaUV1.Y * edge2.Z)
            );

            tangents[(int)i0] += tangent;
            tangents[(int)i1] += tangent;
            tangents[(int)i2] += tangent;
        }

        for (int i = 0; i < tangents.Count; i++)
        {
            Vector3 t = tangents[i];
            Vector3 n = normals[i];

            if (t.LengthSquared() < float.Epsilon)
            {
                Vector3 c1 = Vector3.Cross(n, Vector3.UnitZ);
                Vector3 c2 = Vector3.Cross(n, Vector3.UnitX);
                t = c1.LengthSquared() > c2.LengthSquared() ? c1 : c2;
            }

            t = t - n * Vector3.Dot(n, t);
            tangents[i] = Vector3.Normalize(t);
        }

        return tangents;
    }
}