namespace Engine;

public interface ISceneRenderer<in TScene> where TScene : IScene
{
    void RenderScene(TScene scene, IRenderer renderer, IRenderPass renderPass, RenderPassData data);
}