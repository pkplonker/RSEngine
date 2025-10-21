using System.Numerics;

namespace Engine;

public interface IRenderable
{
	public Matrix4x4 ModelMatrix { get; }
	public void Render(IRenderer renderer, RenderPassData data, CustomShaderArgs customShaderArgs = null);
	public RenderID24 RenderID { get; }
	public static RenderID24 CurrentID = RenderID24.Zero;
}