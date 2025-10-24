using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;

namespace Engine.Physics;

internal class PhysicsBodyData
{
    public GameObject GameObject { get; set; }
    public BodyHandle BodyHandle { get; set; }
    public TypedIndex ShapeIndex { get; set; }
    public RigidbodyComponent Rigidbody { get; set; }
    public ColliderComponent Collider { get; set; }
    public Buffer<Triangle>? MeshBuffer { get; set; } // For mesh colliders
}