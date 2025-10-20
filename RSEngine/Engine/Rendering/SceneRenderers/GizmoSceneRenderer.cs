namespace Engine;

public class GizmoSceneRenderer : ISceneRenderer<GizmoScene>
{
    public static readonly GizmoSceneRenderer Instance = new GizmoSceneRenderer();
    
    public void RenderScene(GizmoScene scene, IRenderer renderer, IRenderPass renderPass, RenderPassData data)
    {
        if (!scene.Enabled)
            return;
            
        scene.RenderGeometry(renderer, data, renderPass);
    }
}