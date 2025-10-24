using Engine.Logging;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Engine;

/// <summary>
/// Specialized render target for object picking that encodes object IDs as pixel colors.
/// Includes depth buffer for proper occlusion and supports reading object IDs from screen coordinates.
/// </summary>
public class PickingRenderTarget : IRenderTarget
{
    public Silk.NET.OpenGL.Texture texture { get; private set; }
    public Renderbuffer depthBuffer { get; private set; }
    public Framebuffer frameBuffer { get; private set; }
    public Vector2D<int> ViewportSize { get; set; }

    public IntPtr GetTextureHandlePtr() => (IntPtr)texture.Handle;

    public PickingRenderTarget(Framebuffer framebuffer, Silk.NET.OpenGL.Texture texture,
        Renderbuffer depthBuffer, Vector2D<int> size)
    {
        this.frameBuffer = framebuffer;
        this.texture = texture;
        this.depthBuffer = depthBuffer;
        this.ViewportSize = size;
    }

    public unsafe void ResizeViewport(GL gl, uint sizeX, uint sizeY)
    {
        ViewportSize = new Vector2D<int>((int)sizeX, (int)sizeY);
        
        gl.BindTexture(TextureTarget.Texture2D, texture.Handle);
        gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, sizeX, sizeY, 0, PixelFormat.Rgba,
            PixelType.UnsignedByte, null);
        
        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer.Handle);
        gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent16, sizeX, sizeY);
    }

    public void ResizeWindow(GL gl, uint sizeX, uint sizeY)
    {
    }

    public void Bind(GL gl)
    {
        gl.Viewport(0, 0, (uint)ViewportSize.X, (uint)ViewportSize.Y);
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer.Handle);
    }

    public unsafe PickedObject ReadObjectIdAtPosition(GL gl, int x, int y)
    {
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer.Handle);

        x = Math.Max(0, Math.Min(x, ViewportSize.X - 1));
        y = ViewportSize.Y - 1 - y;
        y = Math.Max(0, Math.Min(y, ViewportSize.Y - 1));

        byte* pixelData = stackalloc byte[4];
        gl.ReadPixels(x, y, 1, 1, PixelFormat.Rgba, PixelType.UnsignedByte, pixelData);

        return PickedObject.FromColor(pixelData[0], pixelData[1], pixelData[2], pixelData[3]);
    }
}