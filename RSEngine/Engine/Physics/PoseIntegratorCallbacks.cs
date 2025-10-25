using System.Numerics;
using BepuPhysics;
using BepuUtilities;

namespace Engine.Physics;

public struct PoseIntegratorCallbacks : IPoseIntegratorCallbacks
{
    public Vector3 Gravity;
    
    public PoseIntegratorCallbacks(Vector3 gravity)
    {
        Gravity = gravity;
    }

    public readonly AngularIntegrationMode AngularIntegrationMode => 
        AngularIntegrationMode.Nonconserving;
    
    public readonly bool AllowSubstepsForUnconstrainedBodies => false;
    
    public readonly bool IntegrateVelocityForKinematics => false;

    public void Initialize(Simulation simulation)
    {
    }

    public void PrepareForIntegration(float dt)
    {
    }

    public void IntegrateVelocity(Vector<int> bodyIndices, Vector3Wide position, 
        QuaternionWide orientation, BodyInertiaWide localInertia, 
        Vector<int> integrationMask, int workerIndex, Vector<float> dt, 
        ref BodyVelocityWide velocity)
    {
        // Apply gravity to all dynamic bodies //todo only when gravity enabled
        velocity.Linear.Y += Gravity.Y * dt;
    }
}