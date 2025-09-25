using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Engine;

public class RenderTarget : IRenderTarget
{
	public RenderTarget(int sizeX, int sizeY)
	{
		ViewportSize = new Vector2D<int>(sizeX, sizeY);
	}

	public void Bind(GL gl)
	{
		gl.Viewport(0, 0, (uint) ViewportSize.X, (uint) ViewportSize.Y);
		gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
	}

	public void ResizeViewport(GL gl, uint sizeX, uint sizeY)
	{
		unsafe
		{
			ViewportSize = new Vector2D<int>((int) sizeX, (int) sizeY);
		}
	}
	
	public void ResizeWindow(GL gl, uint sizeX, uint sizeY)
	{
		unsafe
		{
			ViewportSize = new Vector2D<int>((int) sizeX, (int) sizeY);
		}
	}

	public Vector2D<int> ViewportSize { get; set; }
}