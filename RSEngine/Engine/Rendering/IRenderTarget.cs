using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Engine;

public interface IRenderTarget
{
	public Vector2D<int> ViewportSize { get; set; }

	public void Bind(GL gl);
	public void ResizeViewport(GL gl, uint sizeX, uint sizeY);

	public void ResizeWindow(GL gl, uint sizeX, uint sizeY);
}