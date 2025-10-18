using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Engine;

public class SelectionRenderPass : IRenderPass
{
    public RenderTargetType TargetType => RenderTargetType.Picking;
    public string Name => "Selection";
    
    private IShader? pickingShader;
    private readonly string PICKING_SHADER_NAME = "Picking";
    
    private IShader? PickingShader
    {
        get
        {
            if (pickingShader == null)
            {
                var res = ResourceManager.Instance.GetResourceByName(PICKING_SHADER_NAME);
                if (res != null && ResourceManager.Instance.TryGetResourceByGuid<Shader>(res.GUID, out var ps))
                {
                    pickingShader = ps;
                }
            }
            return pickingShader;
        }
    }
    
    public IRenderTarget? CreateRenderTarget(GL gl, uint width, uint height)
    {
        unsafe
        {
            gl.GenFramebuffers(1, out Framebuffer framebuffer);
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.Handle);

            gl.GenTextures(1, out Silk.NET.OpenGL.Texture rt);
            gl.BindTexture(TextureTarget.Texture2D, rt.Handle);
            gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba,
                PixelType.UnsignedByte, null);

            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Nearest);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMinFilter.Nearest);

            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, rt.Handle, 0);

            gl.GenTextures(1, out Silk.NET.OpenGL.Texture dummyDepthTexture);
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            return new PickingRenderTarget(framebuffer, rt, dummyDepthTexture, new Vector2D<int>((int)width, (int)height));
        }
    }
    
    public void ConfigureRenderState(GL gl)
    {
        gl.Disable(GLEnum.CullFace);
        gl.Disable(GLEnum.DepthTest);
        gl.ClearColor(0, 0, 0, 0);
        gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
        gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);

    }
    
    public void RenderComponent(IRenderable component, RenderPassData data, IRenderer renderer)
    {
        if (PickingShader != null)
        {
            uint objectId = component.RenderID;
            float r = (objectId & 0xFF) / 255.0f;
            float g = ((objectId >> 8) & 0xFF) / 255.0f;
            float b = ((objectId >> 16) & 0xFF) / 255.0f;

            component.Render(renderer, data,
                new CustomShaderArgs(PickingShader,
                    () => PickingShader.SetUniform("uObjectColor", new Vector3(r, g, b))));
        }
    }
    
    public IRenderable? GetObjectAtPosition(IScene scene, int screenX, int screenY, IRenderer renderer)
    {
        var pickingTarget = renderer.GetSceneRenderTarget(scene, RenderTargetType.Picking) as PickingRenderTarget;
        if (pickingTarget == null) return null;

        uint objectId = pickingTarget.ReadObjectIdAtPosition(renderer.Gl, screenX, screenY);
        if (objectId == 0) return null;

        return scene.Renderables
            .FirstOrDefault(r => r?.RenderID == objectId);
    }
}