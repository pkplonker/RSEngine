using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;

namespace Engine.Physics;

// Base collider component - no simulation knowledge
public abstract class ColliderComponent : Component
{
    protected ColliderComponent(GameObject gameObject) : base(gameObject)
    {
    }

    public float Friction { get; set; } = 1f;
    public float Bounciness { get; set; } = 0f;
    public float SpeculativeMargin { get; set; } = 0.1f;
    
    public Vector3 Offset { get; set; } = Vector3.Zero;
    
    public abstract IShape CreateShape();
    
    public abstract BodyInertia? ComputeInertia(float mass);
}