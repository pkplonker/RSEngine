using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.Constraints;

namespace Engine.Physics;

public struct NarrowPhaseCallbacks : INarrowPhaseCallbacks
{
    public void Initialize(Simulation simulation)
    {
    }

    public bool AllowContactGeneration(int workerIndex, CollidableReference a, 
        CollidableReference b, ref float speculativeMargin)
    {
        // Return true to allow collision between these objects
        return true;
    }

    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, 
        int childIndexA, int childIndexB)
    {
        return true;
    }

    public bool ConfigureContactManifold<TManifold>(int workerIndex, 
        CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) 
        where TManifold : unmanaged, IContactManifold<TManifold>
    {
        // Configure material properties (friction, bounciness)
        pairMaterial.FrictionCoefficient = 1f;
        pairMaterial.MaximumRecoveryVelocity = 2f;
        pairMaterial.SpringSettings = new SpringSettings(30, 1);
        return true;
    }

    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, 
        int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
    {
        return true;
    }

    public void Dispose()
    {
    }
}