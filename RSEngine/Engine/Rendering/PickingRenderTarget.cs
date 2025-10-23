using Engine.Logging;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Engine;

/// Render target specialized for object picking that can read object IDs from pixel colors
public class PickingRenderTarget : IRenderTarget
{
    public Silk.NET.OpenGL.Texture texture { get; private set; }
    public Silk.NET.OpenGL.Texture depthTexture { get; private set; }
    public Framebuffer frameBuffer { get; private set; }
    public Vector2D<int> ViewportSize { get; set; }

    public IntPtr GetTextureHandlePtr() => (IntPtr)texture.Handle;

    public PickingRenderTarget(Framebuffer framebuffer, Silk.NET.OpenGL.Texture texture,
        Silk.NET.OpenGL.Texture depthTexture, Vector2D<int> size)
    {
        this.frameBuffer = framebuffer;
        this.texture = texture;
        this.depthTexture = depthTexture;
        this.ViewportSize = size;
    }

    public unsafe void ResizeViewport(GL gl, uint sizeX, uint sizeY)
    {
        ViewportSize = new Vector2D<int>((int)sizeX, (int)sizeY);
        gl.BindTexture(TextureTarget.Texture2D, texture.Handle);
        gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba, sizeX, sizeY, 0, PixelFormat.Rgba,
            PixelType.UnsignedByte, null);
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