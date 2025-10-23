using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Engine;

public abstract class DebugRenderPass : IRenderPass
{
    public abstract RenderTargetType TargetType { get; }
    public abstract string Name { get; }
    public abstract string ShaderName { get; }
    
    protected IShader? debugShader;
    
    protected virtual IShader? DebugShader
    {
        get
        {
            if (debugShader == null)
            {
                var res = ResourceManager.Instance.GetResourceByName(ShaderName);
                if (res != null && ResourceManager.Instance.TryGetResourceByGuid<Shader>(res.GUID, out var ds))
                {
                    debugShader = ds;
                }
            }
            return debugShader;
        }
    }

    public virtual IRenderTarget? CreateRenderTarget(GL gl, uint width, uint height)
    {
        unsafe
        {
            gl.GenFramebuffers(1, out Framebuffer framebuffer);
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.Handle);

            gl.GenTextures(1, out Silk.NET.OpenGL.Texture rt);
            gl.BindTexture(TextureTarget.Texture2D, rt.Handle);
            gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, width, height, 0, 
                PixelFormat.Rgba, PixelType.UnsignedByte, null);

            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);

            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, rt.Handle, 0);

            gl.GenRenderbuffers(1, out uint depthRenderbuffer);
            gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, depthRenderbuffer);
            gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, width, height);
            gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                RenderbufferTarget.Renderbuffer, depthRenderbuffer);

            var status = gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != GLEnum.FramebufferComplete)
            {
                Logging.Logger.Error($"Debug render target framebuffer is not complete! Status: {status}");
            }

            gl.GenTextures(1, out Silk.NET.OpenGL.Texture dummyDepthTexture);
            
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            return new PickingRenderTarget(framebuffer, rt, dummyDepthTexture, new Vector2D<int>((int)width, (int)height));
        }
    }
    
    public virtual void ConfigureRenderState(GL gl)
    {
        gl.Disable(GLEnum.CullFace);
        gl.Enable(GLEnum.DepthTest);
        gl.DepthFunc(DepthFunction.Less);
        gl.DepthMask(true);
        ConfigureDebugRenderState(gl);
        gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
    }
    
    protected abstract void ConfigureDebugRenderState(GL gl);
    
    public virtual void RenderComponent(IRenderable component, RenderPassData data,IScene scene, IRenderer renderer)
    {
        if (DebugShader != null)
        {
            component.Render(renderer, data, 
                new CustomShaderArgs(DebugShader, () => SetupDebugUniforms()));
        }
        else
        {
            // Fallback to normal rendering
            component.Render(renderer, data);
        }
    }
    
    protected virtual void SetupDebugUniforms()
    {
        // Override in derived classes to set specific uniforms
    }
}
