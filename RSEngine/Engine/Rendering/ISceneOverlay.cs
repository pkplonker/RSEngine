namespace Engine;

public interface ISceneOverlay
{
    string Name { get; }
    bool Enabled { get; set; }
    void Render(IRenderer renderer, RenderPassData data);
}