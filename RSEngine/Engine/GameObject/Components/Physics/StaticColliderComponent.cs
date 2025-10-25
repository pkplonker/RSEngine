namespace Engine.Physics;

[ComponentName("Static Collider")]
public class StaticColliderComponent : Component
{
    // Just a marker component - the physics system will handle it
    public StaticColliderComponent(GameObject gameObject) : base(gameObject)
    {
    }
}