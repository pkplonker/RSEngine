using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using Engine.Logging;

namespace Engine.Physics;

[ComponentName("Mesh Collider")]
public class MeshColliderComponent : ColliderComponent
{
    private Guid meshGuid;
    
    // Reference to your mesh resource
    public MeshColliderComponent(GameObject gameObject) : base(gameObject)
    {
    }

    public string MeshResourceId { get; set; }
    
    public Guid Mesh
    {
        get => meshGuid;
        set => meshGuid = value;
    }
    
    public override IShape CreateShape()
    {
        if (meshGuid == null && !string.IsNullOrEmpty(MeshResourceId))
        {
            // Try to load from resource manager todo
           
        }
        
        if (meshGuid == null)
        {
            throw new InvalidOperationException("Mesh is null. Set mesh before creating shape.");
        }
        
        // Return a placeholder - the PhysicsSystem will handle the actual mesh creation
        // since it needs the buffer pool
        return null; // Special case handled by PhysicsSystem
    }
    
    public override BodyInertia? ComputeInertia(float mass)
    {
        // Mesh colliders typically don't have inertia (static only)
        return null;
    }
    
    public Triangle[] GetTriangles()
    {
        if (meshGuid == null) return Array.Empty<Triangle>();

        if (!ResourceManager.Instance.TryGetResourceByGuid(meshGuid, out var meshResource) || meshResource is not Mesh mesh)
        {
            Logger.Warning($"Failed to get mesh resource for mesh collider {meshGuid}");
            return Array.Empty<Triangle>();
        }

        var vertices = mesh.Vertices; 
        var indices = mesh.Indices; 
        
        const int vertexStride = 14;
        const int positionOffset = 0; 
    
        var triangleCount = indices.Length / 3;
        var triangles = new Triangle[triangleCount];
    
        for (int i = 0; i < triangleCount; i++)
        {
            int baseIndex = i * 3;
        
            // Extract each vertex position
            var v0 = GetVertexPosition(vertices, indices[baseIndex], vertexStride, positionOffset);
            var v1 = GetVertexPosition(vertices, indices[baseIndex + 1], vertexStride, positionOffset);
            var v2 = GetVertexPosition(vertices, indices[baseIndex + 2], vertexStride, positionOffset);
        
            triangles[i] = new Triangle(v0, v1, v2);
        }
    
        return triangles;
    }

    private Vector3 GetVertexPosition(float[] vertices, uint index, int stride, int offset)
    {
        int startIndex = (int)index * stride + offset;
        return new Vector3(
            vertices[startIndex],
            vertices[startIndex + 1],
            vertices[startIndex + 2]
        );
    }
}