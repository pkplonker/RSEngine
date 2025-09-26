using Silk.NET.OpenGL;

namespace Engine;

public interface IRenderPass
{
    RenderTargetType TargetType { get; }
    string Name { get; }
    void ConfigureRenderState(GL gl);
    void RenderComponent(IRenderableComponent component, RenderPassData data, GameObject gameObject, IRenderer renderer);
    IRenderTarget? CreateRenderTarget(GL gl, uint width, uint height);
}