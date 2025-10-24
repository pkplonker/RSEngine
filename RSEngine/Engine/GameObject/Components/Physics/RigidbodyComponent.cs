using System.Numerics;

namespace Engine.Physics;

[ComponentName("Rigidbody")]
public class RigidbodyComponent : Component
{
    private bool isKinematic = false;
    private float mass = 1f;

    public RigidbodyComponent(GameObject gameObject) : base(gameObject)
    {
    }

    public bool IsKinematic
    {
        get => isKinematic;
        set => isKinematic = value;
    }
    
    public float Mass
    {
        get => mass;
        set => mass = Math.Max(value, 0.001f);
    }
    
    public bool UseGravity { get; set; } = true;
    public float Drag { get; set; } = 0f;
    public float AngularDrag { get; set; } = 0.05f;
    public float SleepThreshold { get; set; } = 0.01f;
    
    // Cache for applying forces before physics system is ready
    private Vector3 pendingForce = Vector3.Zero;
    private Vector3 pendingImpulse = Vector3.Zero;
    private Vector3 pendingTorque = Vector3.Zero;
    
    public void ApplyForce(Vector3 force)
    {
        pendingForce += force;
    }
    
    public void ApplyImpulse(Vector3 impulse)
    {
        pendingImpulse += impulse;
    }
    
    public void ApplyTorque(Vector3 torque)
    {
        pendingTorque += torque;
    }
    
    // Physics system will read and clear these
    internal Vector3 ConsumePendingForce()
    {
        var force = pendingForce;
        pendingForce = Vector3.Zero;
        return force;
    }
    
    internal Vector3 ConsumePendingImpulse()
    {
        var impulse = pendingImpulse;
        pendingImpulse = Vector3.Zero;
        return impulse;
    }
    
    internal Vector3 ConsumePendingTorque()
    {
        var torque = pendingTorque;
        pendingTorque = Vector3.Zero;
        return torque;
    }
}