namespace Engine;

public class StandardSceneRenderer : ISceneRenderer<IGameObjectScene>
{
    public void RenderScene(IGameObjectScene scene, IRenderer renderer, IRenderPass renderPass, RenderPassData data)
    {
        foreach (var renderable in scene.Renderables)
        {
            if (renderPass != null)
            {
                renderPass.RenderComponent(renderable, data, scene, renderer);
            }
            else
            {
                renderable.Render(renderer, data);
            }
        }
    }
}