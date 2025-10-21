namespace Engine;

public static class SceneController
{
    static SceneController()
    {
    }

    private static void OnProjectChanged(IProject? obj)
    {
        ActiveScene = null;
    }

    private static IGameObjectScene? activeScene;

    public static IGameObjectScene? ActiveScene
    {
        get => activeScene;
        set
        {
            if (activeScene == null || value != activeScene)
            {
                var oldScene = activeScene;
                activeScene = value;
                OnActiveSceneChanged?.Invoke(activeScene, oldScene);
            }
        }
    }

    public static Action<IScene?, IScene?> OnActiveSceneChanged { get; set; }
}