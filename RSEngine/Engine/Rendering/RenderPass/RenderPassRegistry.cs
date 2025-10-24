using Engine.Logging;

namespace Engine;

/// Registry for managing render passes mapped to render target types
public class RenderPassRegistry
{
    private Dictionary<RenderTargetType, IRenderPass> renderPasses = new();
    
    public void RegisterRenderPass(IRenderPass renderPass)
    {
        renderPasses[renderPass.TargetType] = renderPass;
        Logger.Info($"Registered render pass: {renderPass.Name} for target {renderPass.TargetType}");
    }
    
    public void UnregisterRenderPass(RenderTargetType targetType)
    {
        if (renderPasses.Remove(targetType))
        {
            Logger.Info($"Unregistered render pass for target {targetType}");
        }
    }
    
    public IRenderPass? GetRenderPass(RenderTargetType targetType)
    {
        return renderPasses.TryGetValue(targetType, out var pass) ? pass : null;
    }
    
    public IEnumerable<IRenderPass> GetAllRenderPasses()
    {
        return renderPasses.Values;
    }
}