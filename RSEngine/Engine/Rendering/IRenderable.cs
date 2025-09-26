namespace Engine;

public interface IRenderable
{
	public void Render(IRenderer renderer, RenderPassData data, CustomShaderArgs customShaderArgs = null);
}