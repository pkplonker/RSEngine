using Silk.NET.OpenGL;

namespace Engine;

public enum RenderTargetType
{
    Main,
    Picking,
    Debug
}

public class SceneRenderTargets
{
    private Dictionary<RenderTargetType, IRenderTarget> targets = new();
    
    public void AddTarget(RenderTargetType type, IRenderTarget target)
    {
        targets[type] = target;
    }
    
    public IRenderTarget? GetTarget(RenderTargetType type)
    {
        return targets.TryGetValue(type, out var target) ? target : null;
    }
    
    public void RemoveTarget(RenderTargetType type)
    {
        targets.Remove(type);
    }
    
    public IEnumerable<(RenderTargetType type, IRenderTarget target)> GetAllTargets()
    {
        return targets.Select(kvp => (kvp.Key, kvp.Value));
    }
    
    public void ResizeAll(GL gl, uint width, uint height)
    {
        foreach (var target in targets.Values)
        {
            target?.ResizeWindow(gl, width, height);
        }
    }
}