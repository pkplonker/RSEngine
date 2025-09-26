using System.Drawing;
using System.Numerics;
using Engine.Logging;
using ImGuiNET;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Engine;

public class Renderer : IRenderer
{
    public GL Gl { get; private set; }

    private const int colorVal = 50;
    private Vector4D<int> clearColor = new(colorVal, colorVal, colorVal, 255);
    private Vector4D<int> pickingClearColor = new(0, 0, 0, 0);

    public Vector2D<int> WindowSize { get; set; }
    private IShader lastShader;
    public int DrawCalls { get; private set; }
    public int MaterialsUsed { get; private set; }
    public int ShadersUsed { get; private set; }
    public uint Triangles { get; set; }
    public uint Vertices { get; set; }
    private Dictionary<IScene, IRenderTarget> sceneRenderTargets = new();
    private Dictionary<IScene, PickingRenderTarget> scenePickingTargets = new();
    private Dictionary<uint, GameObject> pickableObjects = new();
    private uint nextObjectId = 1;
    private string PICKING_SHADER = "Picking";
    private IShader? pickingShader;

    private IShader? PickingShader
    {
        get
        {
            if (pickingShader == null)
            {
                var res = ResourceManager.Instance.GetResourceByName(PICKING_SHADER);
                if (res != null &&
                    ResourceManager.Instance.TryGetResourceByGuid<Shader>(res.GUID, out var ps))
                {
                    pickingShader = ps;
                    Logger.Info("Picking shader loaded successfully");
                }
                else
                {
                    Logger.Warning("Missing Picking Shader");
                }
            }

            return pickingShader;
        }
    }

    public void AddScene(IScene? scene, Vector2D<uint> size, out IRenderTarget? renderTarget, bool toFrameBuffer)
    {
        renderTarget = null;
        if (scene == null) return;

        if (!sceneRenderTargets.ContainsKey(scene))
        {
            renderTarget = GenerateIRenderTarget(size.X, size.Y, toFrameBuffer);
            sceneRenderTargets.Add(scene, renderTarget);

            // Also create picking render target if using framebuffer
            if (toFrameBuffer)
            {
                var pickingTarget = GeneratePickingRenderTarget(size.X, size.Y);
                scenePickingTargets.Add(scene, pickingTarget);
            }

            Logger.Info($"Added scene to renderer {scene.Name}");
        }

        renderTarget = sceneRenderTargets[scene];
    }

    private unsafe PickingRenderTarget GeneratePickingRenderTarget(uint sizeX, uint sizeY)
    {
        Gl.GenFramebuffers(1, out Framebuffer framebuffer);
        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.Handle);

        Gl.GenTextures(1, out Silk.NET.OpenGL.Texture rt);
        Gl.BindTexture(TextureTarget.Texture2D, rt.Handle);
        Gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba, sizeX, sizeY, 0, PixelFormat.Rgba,
            PixelType.UnsignedByte, null);

        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Nearest);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Nearest);

        Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, rt.Handle, 0);

        Gl.GenTextures(1, out Silk.NET.OpenGL.Texture dummyDepthTexture);

        return new PickingRenderTarget(framebuffer, rt, dummyDepthTexture, new Vector2D<int>((int)sizeX, (int)sizeY));
    }

    public void RenderUpdate()
    {
        using (var tracker = new PerformanceTracker(nameof(RenderUpdate)))
        {
            DrawCalls = 0;
            ShadersUsed = 0;
            lastShader = null;
            MaterialsUsed = 0;
            Triangles = 0;
            Vertices = 0;

            unsafe
            {
                // Render normal scenes
                foreach (var kvp in sceneRenderTargets)
                {
                    DrawScene(kvp.Value, kvp.Key, false);
                }

                // Render picking passes every frame
                foreach (var kvp in scenePickingTargets)
                {
                    if (sceneRenderTargets.ContainsKey(kvp.Key))
                    {
                        DrawScene(kvp.Value, kvp.Key, true);
                    }
                }

                Gl.Viewport(0, 0, (uint)WindowSize.X, (uint)WindowSize.Y);
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }
        }
    }

    private void DrawScene(IRenderTarget renderTarget, IScene scene, bool isPickingPass)
    {
        renderTarget.Bind(Gl);
        
        Gl.Disable(GLEnum.CullFace);
        Gl.Enable(GLEnum.DepthTest);

        if (isPickingPass)
        {
            Gl.Disable(GLEnum.DepthTest);
            Gl.ClearColor(pickingClearColor);
        }
        else
        {
            Gl.Enable(GLEnum.DepthTest);
            Gl.ClearColor(clearColor);
        }

        Gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

        if (scene.ActiveCamera == null)
        {
            Logger.Warning("No active camera to render with");
            return;
        }

        var renderPassData = new RenderPassData(scene.ActiveCamera.GetView(), scene.ActiveCamera.GetProjection());

        foreach (var gameObject in scene.ChildrenAsGameObjectsRecursive)
        {
            var renderableComponent = gameObject?.GetComponent<IRenderableComponent>();
            if (renderableComponent != null)
            {
                if (isPickingPass)
                {
                    RenderForPicking(renderableComponent, renderPassData, gameObject.ID);
                }
                else
                {
                    renderableComponent.Render(this, renderPassData);
                }
            }
        }
    }


    private void RenderForPicking(IRenderableComponent component, RenderPassData data, uint objectId)
    {
        if (PickingShader != null)
        {
            float r = (objectId & 0xFF) / 255.0f;
            float g = ((objectId >> 8) & 0xFF) / 255.0f;
            float b = ((objectId >> 16) & 0xFF) / 255.0f;

            component.Render(this, data,
                new CustomShaderArgs(PickingShader,
                    () => PickingShader.SetUniform("uObjectColor", new Vector3(r, g, b))));
        }
    }

    public void Resize(Vector2D<int> size)
    {
        WindowSize = size;
        foreach (var target in sceneRenderTargets)
        {
            target.Value?.ResizeWindow(Gl, (uint)size.X, (uint)size.Y);
        }
        foreach (var target in scenePickingTargets)
        {
            target.Value?.ResizeWindow(Gl, (uint)size.X, (uint)size.Y);
        }
    }

    public void Load(IWindow window)
    {
        using var tracker = new PerformanceTracker(nameof(RenderUpdate));
        unsafe
        {
            Gl = GL.GetApi(window);
            WindowSize = window.Size;

            Gl.Enable(GLEnum.CullFace);
            Gl.CullFace(GLEnum.Back);
            Gl.FrontFace(FrontFaceDirection.Ccw);
        }
    }

    private unsafe IRenderTarget GenerateIRenderTarget(uint sizeX, uint sizeY, bool useFrameBuffer)
    {
        if (useFrameBuffer)
        {
            return GenerateFrameBufferRenderTarget(sizeX, sizeY);
        }
        else
        {
            return GenerateRenderTarget();
        }
    }

    private IRenderTarget GenerateRenderTarget()
    {
        return new RenderTarget(WindowSize.X, WindowSize.Y);
    }

    private unsafe FrameBufferRenderTarget GenerateFrameBufferRenderTarget(uint sizeX, uint sizeY)
    {
        Gl.GenFramebuffers(1, out Framebuffer framebuffer);
        Gl.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.Handle);

        Gl.GenTextures(1, out Silk.NET.OpenGL.Texture rt);
        Gl.BindTexture(TextureTarget.Texture2D, rt.Handle);
        Gl.TexImage2D(GLEnum.Texture2D, 0, InternalFormat.Rgba, sizeX, sizeY, 0, PixelFormat.Rgba,
            PixelType.UnsignedByte, null);

        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
            (int)TextureMinFilter.Linear);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter,
            (int)TextureMagFilter.Linear);

        Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, rt.Handle, 0);
        return new FrameBufferRenderTarget(framebuffer, rt, new Vector2D<int>((int)sizeX, (int)sizeY));
    }

    public void SetRenderTargetSize(IScene? scene, Vector2D<float> size)
    {
        if (scene == null) return;
        unsafe
        {
            if (sceneRenderTargets.TryGetValue(scene, out var rt))
            {
                rt.ResizeViewport(Gl, (uint)size.X, (uint)size.Y);
            }
            if (scenePickingTargets.TryGetValue(scene, out var sprt))
            {
                sprt.ResizeViewport(Gl, (uint)size.X, (uint)size.Y);
            }
        }
    }

    public void Close()
    {
    }

    public unsafe void DrawElements(Silk.NET.OpenGL.PrimitiveType primativeType, uint indicesLength,
        DrawElementsType elementsTyp)
    {
        DrawCalls++;
        Triangles += indicesLength / 3;
        Vertices += indicesLength;

        Gl.DrawElements(primativeType, indicesLength, elementsTyp, null);
    }

    public void UseShader(IShader? shader)
    {
        if (lastShader != shader)
        {
            ShadersUsed++;
            shader?.Use();
            lastShader = shader;
        }
    }

    public void UseMaterial(IMaterial material, RenderPassData data, Matrix4x4 modelMatrix)
    {
        if (material == null || material.ShaderGUID == null || material.ShaderGUID == Guid.Empty)
        {
            UseDefaultShader(data, modelMatrix);
            return;
        }

        var shaderGuid = material.ShaderGUID;
        if (ResourceManager.Instance.TryGetResourceByGuid<Shader>(shaderGuid, out var shader))
        {
            UseShader(shader);
            MaterialsUsed++;
            material.Use(this, data, modelMatrix);
        }
    }

    private void UseDefaultShader(RenderPassData data, Matrix4x4 modelMatrix)
    {
        var res = ResourceManager.Instance.GetResourceByName("boing");
        if (res != null && ResourceManager.Instance.TryGetResourceByGuid<Shader>(res.GUID, out var boing))
        {
            boing.Use();
            lastShader = boing;
            UseShader(null);
            boing.SetUniform("uView", data.View);
            boing.SetUniform("uProjection", data.Projection);
            boing.SetUniform("uModel", modelMatrix);
        }
        else
        {
            Gl.UseProgram(0);
            Logger.Warning("Unable to boing");
        }
    }

    public IRenderTarget? GetSceneRenderTarget(IScene? scene)
        => scene == null ? null : sceneRenderTargets.TryGetValue(scene, out var rt) ? rt : null;


    public void RemoveScene(IScene? oldScene)
    {
        if (oldScene != null)
        {
            if (sceneRenderTargets.ContainsKey(oldScene))
            {
                sceneRenderTargets.Remove(oldScene);
            }

            if (scenePickingTargets.ContainsKey(oldScene))
            {
                scenePickingTargets.Remove(oldScene);
            }

            Logger.Info($"Removed scene from renderer {oldScene.Name}");
        }
    }
}