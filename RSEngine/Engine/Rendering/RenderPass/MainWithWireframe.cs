using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Engine;

public class MainWithWireframePass : IRenderPass
{
    public RenderTargetType TargetType => RenderTargetType.MainWithWireframe;
    public string Name => "Main + Wireframe";

    private IShader? wireframeShader;
    private readonly string WIREFRAME_SHADER_NAME = "Wireframe";

    private IShader? WireframeShader
    {
        get
        {
            if (wireframeShader == null)
            {
                var res = ResourceManager.Instance.GetResourceByName(WIREFRAME_SHADER_NAME);
                if (res != null && ResourceManager.Instance.TryGetResourceByGuid<Shader>(res.GUID, out var ws))
                {
                    wireframeShader = ws;
                }
            }

            return wireframeShader;
        }
    }

    public IRenderTarget? CreateRenderTarget(GL gl, uint width, uint height)
    {
        // Use same framebuffer as main target
        unsafe
        {
            gl.GenFramebuffers(1, out Framebuffer framebuffer);
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.Handle);

            gl.GenTextures(1, out Silk.NET.OpenGL.Texture rt);
            gl.BindTexture(TextureTarget.Texture2D, rt.Handle);
            gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba,
                PixelType.UnsignedByte, null);

            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
                (int)TextureMinFilter.Linear);
            gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
                (int)TextureMagFilter.Linear);

            gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
                TextureTarget.Texture2D, rt.Handle, 0);

            gl.GenTextures(1, out Silk.NET.OpenGL.Texture dummyDepthTexture);
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            return new FrameBufferRenderTarget(framebuffer, rt, new Vector2D<int>((int)width, (int)height));
        }
    }

    public void ConfigureRenderState(GL gl)
    {
        gl.Disable(GLEnum.CullFace);
        gl.Enable(GLEnum.DepthTest);
        gl.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
        gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
        gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
    }

    public void RenderComponent(IRenderable component, RenderPassData data, IRenderer renderer)
    {
        component.Render(renderer, data);

        if (WireframeShader != null)
        {
            renderer.Gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line);
            renderer.Gl.PolygonOffset(-1.0f, -1.0f);
            renderer.Gl.Enable(GLEnum.PolygonOffsetLine);

            renderer.UseShader(WireframeShader);
            WireframeShader.SetUniform("uView", data.View);
            WireframeShader.SetUniform("uProjection", data.Projection);
            WireframeShader.SetUniform("uModel", component.ModelMatrix);
            WireframeShader.SetUniform("uWireframeColor", new Vector3(1, 1, 1));

            component.Render(renderer, data);

            renderer.Gl.Disable(GLEnum.PolygonOffsetLine);
            renderer.Gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
        }
    }
}