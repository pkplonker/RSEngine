using System.Numerics;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Engine.Logging;

namespace Engine.Physics;

public class PhysicsSystem
{
    private Simulation simulation;
    private BufferPool bufferPool;
    private Dictionary<GameObject, PhysicsBodyData> dynamicBodies;
    private Dictionary<GameObject, PhysicsStaticData> staticBodies;

    public Vector3 Gravity { get; set; } = new Vector3(0, -10, 0);

    public PhysicsSystem()
    {
        dynamicBodies = new Dictionary<GameObject, PhysicsBodyData>();
        staticBodies = new Dictionary<GameObject, PhysicsStaticData>();
    }

    // Called when starting play mode or loading a scene
    public void Initialize()
    {
        bufferPool = new BufferPool();

        var targetThreadCount = Environment.ProcessorCount switch
        {
            <= 4 => 2,
            <= 8 => 4,
            <= 16 => 6,
            _ => 8
        };

        simulation = Simulation.Create(
            bufferPool,
            new NarrowPhaseCallbacks(),
            new PoseIntegratorCallbacks(Gravity),
            new SolveDescription(8, 1)
        );
    }

    // Called when stopping play mode or unloading a scene
    public void Shutdown()
    {
        // Clean up all bodies
        foreach (var bodyData in dynamicBodies.Values)
        {
            if (bodyData.ShapeIndex.Exists)
            {
                simulation.Shapes.Remove(bodyData.ShapeIndex);
            }

            if (bodyData.MeshBuffer.HasValue)
            {
                var buffer = bodyData.MeshBuffer.Value;
                bufferPool.Return(ref buffer);
            }
        }

        foreach (var staticData in staticBodies.Values)
        {
            if (staticData.ShapeIndex.Exists)
            {
                simulation.Shapes.Remove(staticData.ShapeIndex);
            }

            if (staticData.MeshBuffer.HasValue)
            {
                var buffer = staticData.MeshBuffer.Value;
                bufferPool.Return(ref buffer);
            }
        }

        dynamicBodies.Clear();
        staticBodies.Clear();

        simulation?.Dispose();
        bufferPool?.Clear();

        simulation = null;
        bufferPool = null;
    }

    // Register a game object with physics
    public void RegisterGameObject(GameObject gameObject)
    {
        if (simulation == null)
        {
            throw new InvalidOperationException("PhysicsSystem not initialized. Call Initialize() first.");
        }

        var rigidbody = gameObject.GetComponent<RigidbodyComponent>();
        var staticCollider = gameObject.GetComponent<StaticColliderComponent>();
        var collider = gameObject.GetComponent<ColliderComponent>();

        if (collider == null)
        {
            return;
        }

        try
        {
            if (rigidbody != null)
            {
                RegisterDynamicBody(gameObject, rigidbody, collider);
            }
            else if (staticCollider != null)
            {
                RegisterStaticBody(gameObject, collider);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"ERROR registering {gameObject.Name}: {ex.Message}");
            Logger.Error($"Stack trace: {ex.StackTrace}");
        }
    }

    private void RegisterDynamicBody(GameObject gameObject, RigidbodyComponent rigidbody, ColliderComponent collider)
    {
        Logger.Log($"=== RegisterDynamicBody: {gameObject.Name} ===");

        // Get transform scale
        var transform = gameObject.Transform;
        var scale = transform.Scale;

        // Create shape
        TypedIndex shapeIndex;
        Buffer<Triangle>? meshBuffer = null;

        try
        {
            if (collider is BoxColliderComponent boxCollider)
            {
                // Apply transform scale to collider size
                var scaledSize = boxCollider.Size * scale;

                if (scaledSize.X <= 0 || scaledSize.Y <= 0 || scaledSize.Z <= 0)
                {
                    throw new InvalidOperationException($"Box collider has invalid scaled size: {scaledSize}");
                }

                var box = new Box(scaledSize.X, scaledSize.Y, scaledSize.Z);
                shapeIndex = simulation.Shapes.Add(box);
            }
            else if (collider is SphereColliderComponent sphereCollider)
            {
                // For sphere, use the maximum scale component
                var scaledRadius = sphereCollider.Radius * Math.Max(Math.Max(scale.X, scale.Y), scale.Z);

                if (scaledRadius <= 0)
                {
                    throw new InvalidOperationException($"Sphere collider has invalid scaled radius: {scaledRadius}");
                }

                var sphere = new Sphere(scaledRadius);
                shapeIndex = simulation.Shapes.Add(sphere);
            }
            else if (collider is CapsuleColliderComponent capsuleCollider)
            {
                // Capsule: radius uses XZ, length uses Y
                var scaledRadius = capsuleCollider.Radius * Math.Max(scale.X, scale.Z);
                var scaledLength = capsuleCollider.Length * scale.Y;

                var capsule = new Capsule(scaledRadius, scaledLength);
                shapeIndex = simulation.Shapes.Add(capsule);
            }
            else
            {
                throw new NotSupportedException($"Collider type {collider.GetType().Name} is not supported");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"  ERROR creating shape: {ex.Message}");
            throw;
        }

        // Get transform
        var position = transform.Position + collider.Offset;
        var rotation = transform.Rotation;


        // Compute inertia
        BodyInertia inertia;
        if (rigidbody.IsKinematic)
        {
            inertia = new BodyInertia { InverseMass = 0 };
        }
        else
        {

            if (rigidbody.Mass <= 0)
            {
                throw new InvalidOperationException($"Rigidbody has invalid mass: {rigidbody.Mass}");
            }

            var computedInertia = collider.ComputeInertia(rigidbody.Mass);
            inertia = computedInertia ?? new BodyInertia { InverseMass = 1f / rigidbody.Mass };
        }

        // Create body
        var bodyDescription = BodyDescription.CreateDynamic(
            position,
            inertia,
            new CollidableDescription(shapeIndex, collider.SpeculativeMargin),
            new BodyActivityDescription(rigidbody.SleepThreshold)
        );

        var bodyHandle = simulation.Bodies.Add(bodyDescription);

        // Store data
        dynamicBodies[gameObject] = new PhysicsBodyData
        {
            GameObject = gameObject,
            BodyHandle = bodyHandle,
            ShapeIndex = shapeIndex,
            Rigidbody = rigidbody,
            Collider = collider,
            MeshBuffer = meshBuffer
        };
        
    }

    private void RegisterStaticBody(GameObject gameObject, ColliderComponent collider)
    {

        var transform = gameObject.Transform;
        var scale = transform.Scale;

        // Create shape
        TypedIndex shapeIndex;
        Buffer<Triangle>? meshBuffer = null;

        if (collider is MeshColliderComponent meshCollider)
        {
            var triangles = meshCollider.GetTriangles();
            bufferPool.Take<Triangle>(triangles.Length, out var buffer);

            unsafe
            {
                // Apply scale to mesh vertices
                for (int i = 0; i < triangles.Length; i++)
                {
                    var tri = triangles[i];
                    buffer[i] = new Triangle(
                        tri.A * scale,
                        tri.B * scale,
                        tri.C * scale
                    );
                }
            }

            meshBuffer = buffer;

            var meshShape = new BepuPhysics.Collidables.Mesh(buffer, Vector3.One, bufferPool);
            shapeIndex = simulation.Shapes.Add(meshShape);
        }
        else if (collider is BoxColliderComponent boxCollider)
        {
            var scaledSize = boxCollider.Size * scale;
            var box = new Box(scaledSize.X, scaledSize.Y, scaledSize.Z);
            shapeIndex = simulation.Shapes.Add(box);
        }
        else if (collider is SphereColliderComponent sphereCollider)
        {
            var scaledRadius = sphereCollider.Radius * Math.Max(Math.Max(scale.X, scale.Y), scale.Z);
            var sphere = new Sphere(scaledRadius);
            shapeIndex = simulation.Shapes.Add(sphere);
        }
        else if (collider is CapsuleColliderComponent capsuleCollider)
        {
            var scaledRadius = capsuleCollider.Radius * Math.Max(scale.X, scale.Z);
            var scaledLength = capsuleCollider.Length * scale.Y;
            var capsule = new Capsule(scaledRadius, scaledLength);
            shapeIndex = simulation.Shapes.Add(capsule);
        }
        else
        {
            throw new NotSupportedException($"Collider type {collider.GetType().Name} is not supported");
        }

        // Get transform
        var position = transform.Position + collider.Offset;
        var rotation = transform.Rotation;

        // Create static body - pass TypedIndex directly
        var staticDescription = new StaticDescription(
            position,
            rotation,
            shapeIndex // Just the shape index, not CollidableDescription
        );

        var staticHandle = simulation.Statics.Add(staticDescription);

        // Store data
        staticBodies[gameObject] = new PhysicsStaticData
        {
            GameObject = gameObject,
            StaticHandle = staticHandle,
            ShapeIndex = shapeIndex,
            Collider = collider,
            MeshBuffer = meshBuffer
        };
    }

    // Unregister a game object (when destroyed)
    // Unregister a game object (when destroyed)
    public void UnregisterGameObject(GameObject gameObject)
    {
        if (dynamicBodies.TryGetValue(gameObject, out var bodyData))
        {
            if (simulation != null && simulation.Bodies.BodyExists(bodyData.BodyHandle))
            {
                simulation.Bodies.Remove(bodyData.BodyHandle);
            }

            if (bodyData.ShapeIndex.Exists && simulation != null)
            {
                simulation.Shapes.Remove(bodyData.ShapeIndex);
            }

            if (bodyData.MeshBuffer.HasValue && bufferPool != null)
            {
                var buffer = bodyData.MeshBuffer.Value;
                bufferPool.Return(ref buffer);
            }

            dynamicBodies.Remove(gameObject);
        }

        if (staticBodies.TryGetValue(gameObject, out var staticData))
        {
            if (simulation != null && simulation.Statics.StaticExists(staticData.StaticHandle))
            {
                simulation.Statics.Remove(staticData.StaticHandle);
            }

            if (staticData.ShapeIndex.Exists && simulation != null)
            {
                simulation.Shapes.Remove(staticData.ShapeIndex);
            }

            if (staticData.MeshBuffer.HasValue && bufferPool != null)
            {
                var buffer = staticData.MeshBuffer.Value;
                bufferPool.Return(ref buffer);
            }

            staticBodies.Remove(gameObject);
        }
    }

    // Update physics simulation
    public void Update(float deltaTime)
    {
        if (simulation == null) return;

        // Apply pending forces/impulses
        foreach (var bodyData in dynamicBodies.Values)
        {
            var rb = bodyData.Rigidbody;
            var bodyReference = simulation.Bodies.GetBodyReference(bodyData.BodyHandle);

            var force = rb.ConsumePendingForce();
            if (force != Vector3.Zero)
            {
                bodyReference.Awake = true;
                bodyReference.Velocity.Linear += force * bodyReference.LocalInertia.InverseMass;
            }

            var impulse = rb.ConsumePendingImpulse();
            if (impulse != Vector3.Zero)
            {
                bodyReference.Awake = true;
                bodyReference.Velocity.Linear += impulse * bodyReference.LocalInertia.InverseMass;
            }

            var torque = rb.ConsumePendingTorque();
            if (torque != Vector3.Zero)
            {
                bodyReference.Awake = true;
                bodyReference.Velocity.Angular += torque;
            }

            // Apply drag
            if (rb.Drag > 0)
            {
                bodyReference.Velocity.Linear *= (1f - rb.Drag * deltaTime);
            }

            if (rb.AngularDrag > 0)
            {
                bodyReference.Velocity.Angular *= (1f - rb.AngularDrag * deltaTime);
            }
        }

        // Step simulation
        simulation.Timestep(deltaTime);

        // Sync transforms from physics to game objects
        foreach (var bodyData in dynamicBodies.Values)
        {
            var bodyReference = simulation.Bodies.GetBodyReference(bodyData.BodyHandle);
            var transform = bodyData.GameObject.Transform;

            transform.Position = bodyReference.Pose.Position - bodyData.Collider.Offset;
            transform.Rotation = bodyReference.Pose.Orientation;
        }
    }

// Helper methods for direct physics queries
    public Vector3 GetVelocity(GameObject gameObject)
    {
        if (dynamicBodies.TryGetValue(gameObject, out var bodyData))
        {
            return simulation.Bodies.GetBodyReference(bodyData.BodyHandle).Velocity.Linear;
        }

        return Vector3.Zero;
    }

    public void SetVelocity(GameObject gameObject, Vector3 velocity)
    {
        if (dynamicBodies.TryGetValue(gameObject, out var bodyData))
        {
            var bodyReference = simulation.Bodies.GetBodyReference(bodyData.BodyHandle);
            bodyReference.Velocity.Linear = velocity;
            bodyReference.Awake = true;
        }
    }

    public Vector3 GetAngularVelocity(GameObject gameObject)
    {
        if (dynamicBodies.TryGetValue(gameObject, out var bodyData))
        {
            return simulation.Bodies.GetBodyReference(bodyData.BodyHandle).Velocity.Angular;
        }

        return Vector3.Zero;
    }

    public void SetAngularVelocity(GameObject gameObject, Vector3 angularVelocity)
    {
        if (dynamicBodies.TryGetValue(gameObject, out var bodyData))
        {
            var bodyReference = simulation.Bodies.GetBodyReference(bodyData.BodyHandle);
            bodyReference.Velocity.Angular = angularVelocity;
            bodyReference.Awake = true;
        }
    }
}