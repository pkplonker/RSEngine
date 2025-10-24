using Engine.Physics;
using System.Numerics;

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
}