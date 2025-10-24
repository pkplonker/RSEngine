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
            gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.Rgba8, width, height, 0, 
                PixelFormat.Rgba, PixelType.UnsignedByte, null);

            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Nearest);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Nearest);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS,
                (int)TextureWrapMode.ClampToEdge);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT,
                (int)TextureWrapMode.ClampToEdge);

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
                Logging.Logger.Error($"Picking framebuffer is not complete! Status: {status}");
                Logging.Logger.Error($"Width: {width}, Height: {height}");
                Logging.Logger.Error($"Framebuffer: {framebuffer.Handle}, Color: {rt.Handle}, Depth: {depthRenderbuffer}");
            }
            else
            {
                Logging.Logger.Info($"Picking framebuffer created successfully: {width}x{height}");
            }

            gl.GenTextures(1, out Silk.NET.OpenGL.Texture dummyDepthTexture);
            
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);

            return new PickingRenderTarget(framebuffer, rt, dummyDepthTexture,
                new Vector2D<int>((int)width, (int)height));
        }
    }

    public void ConfigureRenderState(GL gl)
    {
        gl.Disable(GLEnum.CullFace);
        gl.Enable(GLEnum.DepthTest); // ENABLE depth test for picking!
        gl.DepthFunc(DepthFunction.Less);
        gl.DepthMask(true);
        gl.Disable(GLEnum.Blend);
        gl.ClearColor(0, 0, 0, 0);
        gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
        gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }

    public void RenderComponent(IRenderable component, RenderPassData data, IScene scene, IRenderer renderer)
    {
        if (PickingShader != null)
        {
            uint objectId = component.RenderID.Value;

            byte sceneId = scene.SceneID;

            uint packedId = ((uint)sceneId << 24) | objectId;

            float r = (packedId & 0xFF) / 255.0f;
            float g = ((packedId >> 8) & 0xFF) / 255.0f;
            float b = ((packedId >> 16) & 0xFF) / 255.0f;
            float a = ((packedId >> 24) & 0xFF) / 255.0f;
        
            component.Render(renderer, data,
                new CustomShaderArgs(PickingShader,
                    () => PickingShader.SetUniform("uObjectColor", new Vector4(r, g, b, a))));
        }
    }

    public IRenderable? GetObjectAtPosition(IList<IScene> scenes, int screenX, int screenY, IRenderer renderer)
    {
        var pickingTarget = renderer.GetSceneRenderTarget(SceneController.ActiveScene, RenderTargetType.Picking) as PickingRenderTarget;
        if (pickingTarget == null) return null;

        PickedObject pickingObject = pickingTarget.ReadObjectIdAtPosition(renderer.Gl, screenX, screenY);
        if (!pickingObject.IsValid) return null;

        var targetScene = scenes.FirstOrDefault(x=> x.SceneID == pickingObject.SceneId);
        if(targetScene == null) return null;

        return targetScene.ResolveSelection(pickingObject);
    }
}