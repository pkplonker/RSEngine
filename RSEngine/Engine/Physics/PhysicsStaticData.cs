using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;

namespace Engine.Physics;

internal class PhysicsStaticData
{
    public GameObject GameObject { get; set; }
    public StaticHandle StaticHandle { get; set; }
    public TypedIndex ShapeIndex { get; set; }
    public ColliderComponent Collider { get; set; }
    public Buffer<Triangle>? MeshBuffer { get; set; }
}