namespace Engine.Physics;

public abstract class StaticColliderComponent : Component
{
    // Just a marker component - the physics system will handle it
    public StaticColliderComponent(GameObject gameObject) : base(gameObject)
    {
    }
}