using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Engine;

/// <summary>
/// Factory for creating render targets with properly configured framebuffers, color textures, and depth buffers.
/// Consolidates all framebuffer creation logic to ensure consistency across render target types.
/// </summary>
public static class RenderTargetFactory
{
    public static IRenderTarget Create(GL gl, RenderTargetType type, uint width, uint height)
    {
        return type switch
        {
            RenderTargetType.Main => CreateMainTarget(gl, width, height),
            RenderTargetType.Picking => CreatePickingTarget(gl, width, height),
            RenderTargetType.Debug => CreateDebugTarget(gl, width, height),
            _ => new RenderTarget((int)width, (int)height)
        };
    }

    private static unsafe IRenderTarget CreateMainTarget(GL gl, uint width, uint height)
    {
        var (framebuffer, colorTexture, depthBuffer) = CreateFrameBuffer(gl, width, height, false);
        return new FrameBufferRenderTarget(framebuffer, colorTexture, depthBuffer, new Vector2D<int>((int)width, (int)height));
    }

    private static unsafe IRenderTarget CreatePickingTarget(GL gl, uint width, uint height)
    {
        var (framebuffer, colorTexture, depthBuffer) = CreateFrameBuffer(gl, width, height, true);
        return new PickingRenderTarget(framebuffer, colorTexture, depthBuffer, new Vector2D<int>((int)width, (int)height));
    }

    private static unsafe IRenderTarget CreateDebugTarget(GL gl, uint width, uint height)
    {
        var (framebuffer, colorTexture, depthBuffer) = CreateFrameBuffer(gl, width, height, false);
        return new FrameBufferRenderTarget(framebuffer, colorTexture, depthBuffer, new Vector2D<int>((int)width, (int)height));
    }

    private static unsafe (Framebuffer framebuffer, Silk.NET.OpenGL.Texture colorTexture, Renderbuffer depthBuffer) 
        CreateFrameBuffer(GL gl, uint width, uint height, bool useNearestFilter)
    {
        gl.GenFramebuffers(1, out Framebuffer framebuffer);
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.Handle);

        gl.GenTextures(1, out Silk.NET.OpenGL.Texture colorTexture);
        gl.BindTexture(TextureTarget.Texture2D, colorTexture.Handle);
        gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba, width, height, 0, 
            PixelFormat.Rgba, PixelType.UnsignedByte, null);

        var filter = useNearestFilter ? (int)TextureMinFilter.Nearest : (int)TextureMinFilter.Linear;
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, filter);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, filter);

        gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, colorTexture.Handle, 0);

        gl.GenRenderbuffers(1, out Renderbuffer depthBuffer);
        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthBuffer.Handle);
        gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, width, height);
        gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer, depthBuffer.Handle);

        var status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        if (status != GLEnum.FramebufferComplete)
        {
            throw new Exception($"Framebuffer is not complete: {status}");
        }

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        return (framebuffer, colorTexture, depthBuffer);
    }
}