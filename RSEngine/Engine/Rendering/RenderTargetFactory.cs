using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Engine;

/// Factory for creating render targets of different types with consolidated framebuffer creation logic
public static class RenderTargetFactory
{
    public static IRenderTarget Create(GL gl, RenderTargetType type, uint width, uint height)
    {
        return type switch
        {
            RenderTargetType.Main => new RenderTarget((int)width, (int)height),
            RenderTargetType.Picking => CreatePickingTarget(gl, width, height),
            RenderTargetType.Debug => CreateFrameBufferTarget(gl, width, height),
            _ => new RenderTarget((int)width, (int)height)
        };
    }

    private static unsafe PickingRenderTarget CreatePickingTarget(GL gl, uint width, uint height)
    {
        var (framebuffer, colorTexture, depthTexture) = CreateFrameBuffer(gl, width, height, useNearestFilter: true);
        return new PickingRenderTarget(framebuffer, colorTexture, depthTexture, new Vector2D<int>((int)width, (int)height));
    }

    private static unsafe FrameBufferRenderTarget CreateFrameBufferTarget(GL gl, uint width, uint height)
    {
        var (framebuffer, colorTexture, _) = CreateFrameBuffer(gl, width, height, useNearestFilter: false);
        return new FrameBufferRenderTarget(framebuffer, colorTexture, new Vector2D<int>((int)width, (int)height));
    }

    private static unsafe (Framebuffer framebuffer, Silk.NET.OpenGL.Texture colorTexture, Silk.NET.OpenGL.Texture depthTexture) 
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

        gl.GenTextures(1, out Silk.NET.OpenGL.Texture depthTexture);

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        return (framebuffer, colorTexture, depthTexture);
    }
}