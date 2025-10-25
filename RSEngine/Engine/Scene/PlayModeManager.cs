using Engine.Physics;
using System.Numerics;
using Engine.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Engine;

public enum PlayMode
{
    Edit,
    Play,
    Paused
}

public class PlayModeManager
{
    private static PlayModeManager? instance;
    public static PlayModeManager Instance => instance ??= new PlayModeManager();

    private PhysicsSystem? physicsSystem;
    private PlayMode currentMode = PlayMode.Edit;
    private JObject? sceneSnapshot;
    private static readonly JsonSerializer serializer;
    
    static PlayModeManager()
    {
        serializer = new JsonSerializer
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Converters = { new GuidEnumerableConverter() }
        };
    }
    
    public PlayMode CurrentMode
    {
        get => currentMode;
        private set
        {
            if (currentMode != value)
            {
                var oldMode = currentMode;
                currentMode = value;
                OnPlayModeChanged?.Invoke(oldMode, value);
            }
        }
    }

    public event Action<PlayMode, PlayMode>? OnPlayModeChanged;
    
    private Scene? activeScene;

    private PlayModeManager()
    {
    }

    public void SetActiveScene(Scene scene)
    {
        activeScene = scene;
    }

    public void EnterPlayMode()
    {
        if (CurrentMode == PlayMode.Play) return;
        
        // Create in-memory snapshot of the scene
        if (activeScene != null)
        {
            try
            {
                sceneSnapshot = SerializeSceneToMemory(activeScene);
                Logger.Info("Scene snapshot created in memory");
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to create scene snapshot: {e}");
                sceneSnapshot = null;
            }
        }
        
        // Initialize physics
        physicsSystem = new PhysicsSystem();
        physicsSystem.Initialize();
        
        // Register all game objects in the scene with physics components
        if (activeScene != null)
        {
            foreach (var gameObject in activeScene.ChildrenAsGameObjectsRecursive)
            {
                RegisterGameObjectPhysics(gameObject);
            }
        }
        
        CurrentMode = PlayMode.Play;
    }

    public void ExitPlayMode()
    {
        if (CurrentMode == PlayMode.Edit) return;
        
        // Shutdown physics
        physicsSystem?.Shutdown();
        physicsSystem = null;
        
        // Restore scene from in-memory snapshot
        if (activeScene != null && sceneSnapshot != null)
        {
            try
            {
                DeserializeSceneFromMemory(activeScene, sceneSnapshot);
                Logger.Info("Scene restored from snapshot");
            }
            catch (Exception e)
            {
                Logger.Error($"Failed to restore scene from snapshot: {e}");
            }
            finally
            {
                sceneSnapshot = null;
            }
        }
        
        CurrentMode = PlayMode.Edit;
    }

    public void TogglePause()
    {
        if (CurrentMode == PlayMode.Play)
            CurrentMode = PlayMode.Paused;
        else if (CurrentMode == PlayMode.Paused)
            CurrentMode = PlayMode.Play;
    }

    public void Update(float deltaTime)
    {
        if (CurrentMode != PlayMode.Play) return;
        
        // Update physics
        physicsSystem?.Update(deltaTime);
        
        // Update game logic
        activeScene?.Update();
    }

    private void RegisterGameObjectPhysics(GameObject gameObject)
    {
        var collider = gameObject.GetComponent<ColliderComponent>();
        if (collider != null)
        {
            physicsSystem?.RegisterGameObject(gameObject);
        }
    }

    public void OnGameObjectCreated(GameObject gameObject)
    {
        if (CurrentMode == PlayMode.Play)
        {
            RegisterGameObjectPhysics(gameObject);
        }
    }

    public void OnGameObjectDestroyed(GameObject gameObject)
    {
        if (CurrentMode == PlayMode.Play)
        {
            physicsSystem?.UnregisterGameObject(gameObject);
        }
    }
    
    private JObject SerializeSceneToMemory(Scene scene)
    {
        var rootObject = new JObject();
        
        foreach (var gameObject in scene.ChildrenAsGameObjectsRecursive)
        {
            var gameObjectJObject = new JObject();
            var componentsJObject = new JObject();
            
            // Serialize transform
            var transformJObject = new JObject();
            SceneSerializer.SerializeComponent(gameObject.Transform).Properties().ToList()
                .ForEach(p => transformJObject.Add(p.Name, p.Value));
            gameObjectJObject.Add("transform", transformJObject);
            
            // Serialize other properties
            var goPropsJObject = SceneSerializer.SerializeComponent(gameObject);
            foreach (var prop in goPropsJObject.Properties())
            {
                if (prop.Name != "Transform")
                {
                    gameObjectJObject.Add(prop.Name, prop.Value);
                }
            }
            
            // Serialize components
            var components = gameObject.GetComponents();
            foreach (var component in components)
            {
                componentsJObject.Add(component.GetType().AssemblyQualifiedName,
                    SceneSerializer.SerializeComponent(component));
            }
            
            if (componentsJObject.Count > 0)
            {
                gameObjectJObject.Add("components", componentsJObject);
            }
            
            rootObject.Add(gameObject.Name, gameObjectJObject);
        }
        
        return rootObject;
    }
    
    private void DeserializeSceneFromMemory(Scene scene, JObject rootObject)
    {
        var gameObjectLookup = new Dictionary<Guid, GameObject>();
        var parentToChildrenGuids = new Dictionary<Guid, List<Guid>>();
        
        // Clear current scene
        var currentObjects = scene.ChildrenAsGameObjectsRecursive.ToList();
        foreach (var obj in currentObjects)
        {
            scene.RemoveGameObject(obj);
        }
        
        // Deserialize game objects
        foreach (var goToken in rootObject)
        {
            var go = new GameObject { Name = goToken.Key };
            var gameObjectJObject = goToken.Value as JObject;
            
            // Deserialize transform
            var transformObject = gameObjectJObject["transform"] as JObject;
            if (transformObject != null)
            {
                SceneDeserializer.DeserializeProperties(go.Transform, transformObject);
                var childGuids = ExtractChildGuids(transformObject);
                parentToChildrenGuids[go.Transform.GUID] = childGuids;
            }
            
            // Deserialize GameObject properties
            SceneDeserializer.DeserializeProperties(go, gameObjectJObject);
            
            // Deserialize components
            var componentsObject = gameObjectJObject["components"] as JObject;
            if (componentsObject != null)
            {
                foreach (var component in componentsObject)
                {
                    var componentType = Type.GetType(component.Key);
                    if (componentType != null)
                    {
                        var constructor = componentType.GetConstructor(new[] { typeof(GameObject) });
                        if (constructor != null)
                        {
                            var comp = (IComponent)constructor.Invoke(new object[] { go });
                            SceneDeserializer.DeserializeProperties(comp, component.Value as JObject);
                            go.AddComponent(comp);
                        }
                    }
                }
            }
            
            gameObjectLookup.Add(go.Transform.GUID, go);
            scene.AddGameObject(go);
        }
        
        // Restore parent-child relationships
        foreach (var kvp in parentToChildrenGuids)
        {
            if (gameObjectLookup.TryGetValue(kvp.Key, out var parent))
            {
                foreach (var childGuid in kvp.Value)
                {
                    if (gameObjectLookup.TryGetValue(childGuid, out var child))
                    {
                        child.Transform.SetParent(parent.Transform);
                    }
                }
            }
        }
    }
    
    private List<Guid> ExtractChildGuids(JObject transformObject)
    {
        var childGuids = new List<Guid>();
        var childrenGuidsToken = transformObject["ChildrenGuids"];
        if (childrenGuidsToken is JArray childrenArray)
        {
            foreach (var childToken in childrenArray)
            {
                if (Guid.TryParse(childToken.ToString(), out Guid childGuid))
                {
                    childGuids.Add(childGuid);
                }
            }
        }
        return childGuids;
    }
}