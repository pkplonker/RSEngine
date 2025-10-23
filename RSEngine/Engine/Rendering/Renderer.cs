using System.Drawing;
using System.Numerics;
using Engine.Logging;
using ImGuiNET;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Engine;

/// Main rendering system that manages scenes, render targets, and rendering pipeline execution
public class Renderer : IRenderer
{
    public GL Gl { get; private set; }
    public Vector2D<int> WindowSize { get; set; }
    public int DrawCalls { get; private set; }
    public int MaterialsUsed { get; private set; }
    public int ShadersUsed { get; private set; }
    public uint Triangles { get; set; }
    public uint Vertices { get; set; }
    public RenderPassRegistry RenderPassRegistry { get; private set; } = new();
    public SceneOverlayRegistry OverlayRegistry { get; private set; } = new();

    private const int ColorVal = 50;
    private readonly Vector4D<int> clearColor = new(ColorVal, ColorVal, ColorVal, 255);
    
    private IShader lastShader;
    private Dictionary<IScene, SceneRenderInfo> scenes = new();

    public void AddScene(IScene? scene, Vector2D<uint> size, out IRenderTarget? renderTarget, bool toFrameBuffer)
    {
        renderTarget = null;
        if (scene == null) return;

        if (!scenes.ContainsKey(scene))
        {
            var sceneRenderTargets = new SceneRenderTargets();

            var mainTarget = GenerateMainRenderTarget(size.X, size.Y, toFrameBuffer);
            
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

            scenes.Add(scene, new SceneRenderInfo(sceneRenderTargets));
            Logger.Info($"Added scene to renderer {scene.Name}");
        }

        renderTarget = scenes[scene].Targets.GetTarget(RenderTargetType.Main);
    }

    public void EnsureRenderTarget(IScene scene, RenderTargetType type)
    {
        if (scenes.TryGetValue(scene, out var info) && info.Targets.GetTarget(type) == null)
        {
            var renderPass = RenderPassRegistry.GetRenderPass(type);
            if (renderPass != null)
            {
                var mainTarget = info.Targets.GetTarget(RenderTargetType.Main);
                var newTarget = renderPass.CreateRenderTarget(Gl,
                    (uint)mainTarget.ViewportSize.X,
                    (uint)mainTarget.ViewportSize.Y);
                if (newTarget != null)
                {
                    info.Targets.AddTarget(type, newTarget);
                }
            }
        }
    }

    public void RenderUpdate()
    {
        using (var tracker = new PerformanceTracker(nameof(RenderUpdate)))
        {
            ResetRenderStats();

            unsafe
            {
                var processedRenderTargets = new HashSet<IRenderTarget>();

                foreach (var (scene, info) in scenes)
                {
                    foreach (var (targetType, renderTarget) in info.Targets.GetAllTargets())
                    {
                        bool isFirstSceneForTarget = !processedRenderTargets.Contains(renderTarget);
                        RenderScene(renderTarget, scene, targetType, isFirstSceneForTarget);
                        processedRenderTargets.Add(renderTarget);
                    }
                }

                Gl.Viewport(0, 0, (uint)WindowSize.X, (uint)WindowSize.Y);
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }
        }
    }

    private void RenderScene(IRenderTarget renderTarget, IScene scene, RenderTargetType targetType, bool clearTarget)
    {
        renderTarget.Bind(Gl);

        if (scene.ActiveCamera == null)
        {
            Logger.Warning("No active camera to render with");
            return;
        }

        var renderPass = RenderPassRegistry.GetRenderPass(targetType);

        if (clearTarget)
        {
            if (renderPass != null)
            {
                renderPass.ConfigureRenderState(Gl);
            }
            else
            {
                ConfigureDefaultRenderState();
            }
        }

        var renderPassData = new RenderPassData(scene.ActiveCamera.GetView(), scene.ActiveCamera.GetProjection());
        
        scene.RenderUsing(this, renderPass, renderPassData);

        if (targetType != RenderTargetType.Picking)
        {
            foreach (var overlay in OverlayRegistry.GetEnabledOverlays())
            {
                overlay.Render(this, renderPassData);
            }
        }
    }

    private void ConfigureDefaultRenderState()
    {
        Gl.Disable(GLEnum.CullFace);
        Gl.Enable(GLEnum.DepthTest);
        Gl.ClearColor(clearColor);
        Gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
        Gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }

    private void ResetRenderStats()
    {
        DrawCalls = 0;
        ShadersUsed = 0;
        lastShader = null;
        MaterialsUsed = 0;
        Triangles = 0;
        Vertices = 0;
    }

    public void RemoveScene(IScene? oldScene)
    {
        if (oldScene == null || !scenes.ContainsKey(oldScene)) return;

        var info = scenes[oldScene];
        
        foreach (var sharedScene in info.SharedWithScenes)
        {
            if (scenes.ContainsKey(sharedScene))
            {
                scenes.Remove(sharedScene);
            }
        }

        scenes.Remove(oldScene);
        Logger.Info($"Removed scene from renderer {oldScene.Name}");
    }

    public void Resize(Vector2D<int> size)
    {
        WindowSize = size;
        foreach (var info in scenes.Values)
        {
            info.Targets.ResizeAll(Gl, (uint)size.X, (uint)size.Y);
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

    public void SetRenderTargetSize(IScene? scene, Vector2D<float> size)
    {
        if (scene == null) return;
        unsafe
        {
            if (scenes.TryGetValue(scene, out var info))
            {
                foreach (var (_, target) in info.Targets.GetAllTargets())
                {
                    target?.ResizeViewport(Gl, (uint)size.X, (uint)size.Y);
                }
            }
        }
    }

    public void Close()
    {
    }

    private unsafe IRenderTarget GenerateMainRenderTarget(uint sizeX, uint sizeY, bool useFrameBuffer)
    {
        if (useFrameBuffer)
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
        else
        {
            return new RenderTarget((int)sizeX, (int)sizeY);
        }
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
        return scenes.TryGetValue(scene, out var info) ? info.Targets.GetTarget(type) : null;
    }
    
    public void ShareRenderTargets(IScene sourceScene, IScene targetScene)
    {
        if (sourceScene == null || targetScene == null) return;
    
        if (scenes.TryGetValue(sourceScene, out var sourceInfo))
        {
            var targetInfo = new SceneRenderInfo(sourceInfo.Targets);
            scenes[targetScene] = targetInfo;
            sourceInfo.SharedWithScenes.Add(targetScene);
            
            Logger.Info($"Scene '{targetScene.Name}' now shares render targets with '{sourceScene.Name}'");
        }
    }
}