using Engine.Logging;

namespace Engine;

public class SceneOverlayRegistry
{
    private List<ISceneOverlay> overlays = new();
    
    public void RegisterOverlay(ISceneOverlay overlay)
    {
        overlays.Add(overlay);
        Logger.Info($"Registered scene overlay: {overlay.Name}");
    }
    
    public void UnregisterOverlay(ISceneOverlay overlay)
    {
        overlays.Remove(overlay);
        Logger.Info($"Unregistered scene overlay: {overlay.Name}");
    }
    
    public IEnumerable<ISceneOverlay> GetEnabledOverlays()
    {
        return overlays.Where(o => o.Enabled);
    }
}