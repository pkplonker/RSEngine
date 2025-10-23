using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Engine;

/// Render pass for object picking that encodes object and scene IDs as colors
public class SelectionRenderPass : IRenderPass
{
    public RenderTargetType TargetType => RenderTargetType.Picking;
    public string Name => "Selection";

    private readonly IShader? pickingShader;

    public SelectionRenderPass()
    {
        var res = ResourceManager.Instance.GetResourceByName("Picking");
        if (res != null && ResourceManager.Instance.TryGetResourceByGuid<Shader>(res.GUID, out var shader))
        {
            pickingShader = shader;
        }
    }

    public IRenderTarget? CreateRenderTarget(GL gl, uint width, uint height)
    {
        return RenderTargetFactory.Create(gl, TargetType, width, height);
    }

    public void ConfigureRenderState(GL gl)
    {
        gl.Disable(GLEnum.CullFace);
        gl.Disable(GLEnum.DepthTest);
        gl.Disable(GLEnum.Blend);
        gl.ClearColor(0, 0, 0, 0);
        gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
        gl.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);
    }

    public void RenderComponent(IRenderable component, RenderPassData data, IScene scene, IRenderer renderer)
    {
        if (pickingShader == null) return;
        
        uint objectId = component.RenderID.Value;
        byte sceneId = scene.SceneID;
        uint packedId = ((uint)sceneId << 24) | objectId;

        float r = (packedId & 0xFF) / 255.0f;
        float g = ((packedId >> 8) & 0xFF) / 255.0f;
        float b = ((packedId >> 16) & 0xFF) / 255.0f;
        float a = ((packedId >> 24) & 0xFF) / 255.0f;
        
        component.Render(renderer, data,
            new CustomShaderArgs(pickingShader,
                () => pickingShader.SetUniform("uObjectColor", new Vector4(r, g, b, a))));
    }

    public IRenderable? GetObjectAtPosition(IList<IScene> scenes, int screenX, int screenY, IRenderer renderer)
    {
        var pickingTarget = renderer.GetSceneRenderTarget(SceneController.ActiveScene, RenderTargetType.Picking) as PickingRenderTarget;
        if (pickingTarget == null) return null;

        PickedObject pickingObject = pickingTarget.ReadObjectIdAtPosition(renderer.Gl, screenX, screenY);
        if (!pickingObject.IsValid) return null;

        var targetScene = scenes.FirstOrDefault(x => x.SceneID == pickingObject.SceneId);
        if (targetScene == null) return null;

        return targetScene.ResolveSelection(pickingObject);
    }
}