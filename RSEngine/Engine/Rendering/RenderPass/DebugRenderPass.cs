using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Engine;

/// Base class for debug render passes that use a custom shader to render scene components
public abstract class DebugRenderPass : IRenderPass
{
    public abstract RenderTargetType TargetType { get; }
    public abstract string Name { get; }
    
    private readonly string shaderName;
    private IShader? debugShaderCache;
    
    protected IShader? debugShader
    {
        get
        {
            if (debugShaderCache == null)
            {
                var res = ResourceManager.Instance.GetResourceByName(shaderName);
                if (res != null && ResourceManager.Instance.TryGetResourceByGuid<Shader>(res.GUID, out var shader))
                {
                    debugShaderCache = shader;
                }
            }
            return debugShaderCache;
        }
    }
    
    protected DebugRenderPass(string shaderName)
    {
        this.shaderName = shaderName;
    }

    public virtual IRenderTarget? CreateRenderTarget(GL gl, uint width, uint height)
    {
        return RenderTargetFactory.Create(gl, TargetType, width, height);
    }
    
    public void ConfigureRenderState(GL gl)
    {
        gl.Disable(GLEnum.CullFace);
        gl.Enable(GLEnum.DepthTest);
        ConfigureDebugRenderState(gl);
        gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
    }
    
    protected abstract void ConfigureDebugRenderState(GL gl);
    
    public void RenderComponent(IRenderable component, RenderPassData data, IScene scene, IRenderer renderer)
    {
        if (debugShader != null)
        {
            component.Render(renderer, data, 
                new CustomShaderArgs(debugShader, () => SetupDebugUniforms()));
        }
        else
        {
            component.Render(renderer, data);
        }
    }
    
    protected virtual void SetupDebugUniforms()
    {
    }
}