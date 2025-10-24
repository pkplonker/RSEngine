using BepuPhysics;
using BepuPhysics.Collidables;

namespace Engine.Physics;

public class SphereColliderComponent : ColliderComponent
{
    public SphereColliderComponent(GameObject gameObject) : base(gameObject)
    {
    }

    public float Radius { get; set; } = 0.5f;
    
    public override IShape CreateShape()
    {
        return new Sphere(Radius);
    }
    
    public override BodyInertia? ComputeInertia(float mass)
    {
        var shape = new Sphere(Radius);
        return shape.ComputeInertia(mass);
    }
}