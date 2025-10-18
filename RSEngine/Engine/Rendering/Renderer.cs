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

    private Dictionary<IScene, SceneRenderTargets> sceneTargets = new();
    private Dictionary<IRenderTarget, List<IScene>> renderTargetScenes = new();

    public RenderPassRegistry RenderPassRegistry { get; private set; } = new();
    public SceneOverlayRegistry OverlayRegistry { get; private set; } = new();

    public void AddScene(IScene? scene, Vector2D<uint> size, out IRenderTarget? renderTarget, bool toFrameBuffer)
    {
        renderTarget = null;
        if (scene == null) return;

        if (!sceneTargets.ContainsKey(scene))
        {
            var sceneRenderTargets = new SceneRenderTargets();

            var mainTarget = GenerateIRenderTarget(size.X, size.Y, toFrameBuffer);
            sceneRenderTargets.AddTarget(RenderTargetType.Main, mainTarget);

            if (toFrameBuffer)
            {
                var pickingTarget = RenderPassRegistry.GetRenderPass(RenderTargetType.Picking)
                    ?.CreateRenderTarget(Gl, size.X, size.Y);
                if (pickingTarget != null)
                {
                    sceneRenderTargets.AddTarget(RenderTargetType.Picking, pickingTarget);
                }
            }

            sceneTargets.Add(scene, sceneRenderTargets);

            // Track render target to scene mapping
            if (!renderTargetScenes.ContainsKey(mainTarget))
            {
                renderTargetScenes[mainTarget] = new List<IScene>();
            }

            renderTargetScenes[mainTarget].Add(scene);

            Logger.Info($"Added scene to renderer {scene.Name}");
        }

        renderTarget = sceneTargets[scene].GetTarget(RenderTargetType.Main);
    }

    public void EnsureRenderTarget(IScene scene, RenderTargetType type)
    {
        if (sceneTargets.TryGetValue(scene, out var targets) && targets.GetTarget(type) == null)
        {
            var renderPass = RenderPassRegistry.GetRenderPass(type);
            if (renderPass != null)
            {
                var newTarget = renderPass.CreateRenderTarget(Gl,
                    (uint)targets.GetTarget(RenderTargetType.Main).ViewportSize.X,
                    (uint)targets.GetTarget(RenderTargetType.Main).ViewportSize.Y);
                if (newTarget != null)
                {
                    targets.AddTarget(type, newTarget);
                }
            }
        }
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
                // Group scenes by render target to avoid redundant clears
                var processedRenderTargets = new HashSet<IRenderTarget>();

                foreach (var (scene, targets) in sceneTargets)
                {
                    foreach (var (targetType, renderTarget) in targets.GetAllTargets())
                    {
                        // Only clear and setup once per render target
                        bool isFirstSceneForTarget = !processedRenderTargets.Contains(renderTarget);

                        var renderPass = RenderPassRegistry.GetRenderPass(targetType);
                        if (renderPass != null)
                        {
                            RenderSceneWithPass(renderTarget, scene, renderPass, isFirstSceneForTarget);
                        }
                        else if (targetType == RenderTargetType.Main)
                        {
                            RenderSceneMain(renderTarget, scene, isFirstSceneForTarget);
                        }

                        processedRenderTargets.Add(renderTarget);
                    }
                }

                Gl.Viewport(0, 0, (uint)WindowSize.X, (uint)WindowSize.Y);
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }
        }
    }

    private void RenderSceneWithPass(IRenderTarget renderTarget, IScene scene, IRenderPass renderPass, bool clearTarget)
    {
        renderTarget.Bind(Gl);

        if (clearTarget)
        {
            renderPass.ConfigureRenderState(Gl);
        }

        if (scene.ActiveCamera == null)
        {
            Logger.Warning("No active camera to render with");
            return;
        }

        var renderPassData = new RenderPassData(scene.ActiveCamera.GetView(), scene.ActiveCamera.GetProjection());

        foreach (var renderable in scene.Renderables)
        {
            if (renderable != null)
            {
                renderPass.RenderComponent(renderable, renderPassData, scene,this);
            }
        }

        if (renderPass.TargetType != RenderTargetType.Picking)
        {
            foreach (var overlay in OverlayRegistry.GetEnabledOverlays())
            {
                overlay.Render(this, renderPassData);
            }
        }
    }

    private void RenderSceneMain(IRenderTarget renderTarget, IScene scene, bool clearTarget)
    {
        renderTarget.Bind(Gl);

        if (clearTarget)
        {
            Gl.Disable(GLEnum.CullFace);
            Gl.Enable(GLEnum.DepthTest);
            Gl.ClearColor(clearColor);
            Gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
            Gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
        }

        if (scene.ActiveCamera == null)
        {
            Logger.Warning("No active camera to render with");
            return;
        }

        var renderPassData = new RenderPassData(scene.ActiveCamera.GetView(), scene.ActiveCamera.GetProjection());

        foreach (var renderable in scene.Renderables)
        {
            if (renderable != null)
            {
                renderable.Render(this, renderPassData);
            }
        }

        foreach (var overlay in OverlayRegistry.GetEnabledOverlays())
        {
            overlay.Render(this, renderPassData);
        }
    }

    public void RemoveScene(IScene? oldScene)
    {
        if (oldScene != null && sceneTargets.ContainsKey(oldScene))
        {
            var targets = sceneTargets[oldScene];
            var mainTarget = targets.GetTarget(RenderTargetType.Main);

            // Remove scene from render target tracking
            if (mainTarget != null && renderTargetScenes.ContainsKey(mainTarget))
            {
                renderTargetScenes[mainTarget].Remove(oldScene);
                if (renderTargetScenes[mainTarget].Count == 0)
                {
                    renderTargetScenes.Remove(mainTarget);
                }
            }

            sceneTargets.Remove(oldScene);
            Logger.Info($"Removed scene from renderer {oldScene.Name}");
        }
    }

    public void Resize(Vector2D<int> size)
    {
        WindowSize = size;
        foreach (var targets in sceneTargets.Values)
        {
            targets.ResizeAll(Gl, (uint)size.X, (uint)size.Y);
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
            if (sceneTargets.TryGetValue(scene, out var targets))
            {
                foreach (var (_, target) in targets.GetAllTargets())
                {
                    target?.ResizeViewport(Gl, (uint)size.X, (uint)size.Y);
                }
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

    public IRenderTarget? GetSceneRenderTarget(IScene? scene, RenderTargetType type = RenderTargetType.Main)
    {
        if (scene == null) return null;
        return sceneTargets.TryGetValue(scene, out var targets) ? targets.GetTarget(type) : null;
    }
}