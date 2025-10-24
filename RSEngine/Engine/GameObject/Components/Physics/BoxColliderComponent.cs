using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;

namespace Engine.Physics;

[ComponentName("Box Collider")]
public class BoxColliderComponent : ColliderComponent
{
    public BoxColliderComponent(GameObject gameObject) : base(gameObject)
    {
    }

    public Vector3 Size { get; set; } = Vector3.One;
    
    public override IShape CreateShape()
    {
        return new Box(Size.X, Size.Y, Size.Z);
    }
    
    public override BodyInertia? ComputeInertia(float mass)
    {
        var shape = new Box(Size.X, Size.Y, Size.Z); 
        return shape.ComputeInertia(mass);
    }
}