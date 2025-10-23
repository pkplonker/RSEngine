namespace Engine;

/// Encapsulates render target information for a scene including shared scene references
internal class SceneRenderInfo
{
    public SceneRenderTargets Targets { get; set; }
    public List<IScene> SharedWithScenes { get; set; } = new();
    
    public SceneRenderInfo(SceneRenderTargets targets)
    {
        Targets = targets;
    }
}