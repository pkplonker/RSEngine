using System.Numerics;

namespace Engine;

public interface IRenderable
{
	public Matrix4x4 ModelMatrix { get; }
	public void Render(IRenderer renderer, RenderPassData data, CustomShaderArgs customShaderArgs = null);
	public uint RenderID { get; }
	public static uint CurrentID;
}