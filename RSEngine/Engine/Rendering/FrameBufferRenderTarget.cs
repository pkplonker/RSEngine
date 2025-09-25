using System.Drawing;
using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Engine;

public class FrameBufferRenderTarget : IRenderTarget
{
	public Silk.NET.OpenGL.Texture texture { get; private set; }
	public Framebuffer frameBuffer { get; private set; }
	public Vector2D<int> ViewportSize { get; set; }
	public IntPtr GetTextureHandlePtr() => (IntPtr) texture.Handle;

	public FrameBufferRenderTarget(Framebuffer framebuffer, Silk.NET.OpenGL.Texture texture, Vector2D<int> size)
	{
		this.frameBuffer = framebuffer;
		this.texture = texture;
		this.ViewportSize = size;
	}

	public void ResizeViewport(GL gl, uint sizeX, uint sizeY)
	{
		unsafe
		{
			ViewportSize = new Vector2D<int>((int) sizeX, (int) sizeY);
			gl.BindTexture(TextureTarget.Texture2D, texture.Handle);
			gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba, sizeX, sizeY, 0, PixelFormat.Rgba,
				PixelType.UnsignedByte, null);
		}
	}

	public void ResizeWindow(GL gl, uint sizeX, uint sizeY)
	{
		
	}

	public void Bind(GL gl)
	{
		gl.Viewport(0, 0, (uint) ViewportSize.X, (uint) ViewportSize.Y);
		gl.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer.Handle);
	}
}

