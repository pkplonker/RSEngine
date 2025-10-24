using BepuPhysics;
using BepuPhysics.Collidables;

namespace Engine.Physics;

[ComponentName("Capsule Collider")]
public class CapsuleColliderComponent : ColliderComponent
{
    public CapsuleColliderComponent(GameObject gameObject) : base(gameObject)
    {
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