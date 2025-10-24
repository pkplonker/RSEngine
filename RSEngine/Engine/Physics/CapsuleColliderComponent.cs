using BepuPhysics;
using BepuPhysics.Collidables;

namespace Engine.Physics;

public class CapsuleColliderComponent : ColliderComponent
{
    public CapsuleColliderComponent(GameObject gameObject, float radius, float length) : base(gameObject)
    {
        Radius = radius;
        Length = length;
    }

    public float Radius { get; set; } = 0.5f;
    public float Length { get; set; } = 2f;
    
    public override IShape CreateShape()
    {
        return new Capsule(Radius, Length);
    }
    
    public override BodyInertia? ComputeInertia(float mass)
    {
        var shape = new Capsule(Radius, Length);
        return shape.ComputeInertia(mass);
    }
}