using System.Numerics;
using Engine;
using Silk.NET.OpenGL;

namespace Editor;

public class GizmoScene : IScene
{
    public bool Enabled { get; set; } = true;

    private IShader? gizmoShader;
    private IShader? pickingShader;
    private Matrix4x4 modelMatrix;
    private GizmoController.GizmoType currentGizmoType = GizmoController.GizmoType.None;
    
    private TranslationGizmo? translationGizmo;
    private RotationGizmo? rotationGizmo;
    private ScaleGizmo? scaleGizmo;
    
    private static readonly GizmoSceneRenderer renderer = new GizmoSceneRenderer();
    
    public float GizmoScreenSize { get; set; } = 0.15f;

    // Picking IDs for each axis
    private static readonly RenderID24 X_AXIS_ID = new RenderID24(1);
    private static readonly RenderID24 Y_AXIS_ID = new RenderID24(2);
    private static readonly RenderID24 Z_AXIS_ID = new RenderID24(3);

    public GizmoScene()
    {
        SceneID = IScene.GetNextId();
    }

    public string Name { get; set; } = "Gizmo Scene";
    public ICamera? ActiveCamera { get; set; }
    public string Path { get; set; }

    public void RenderUsing(IRenderer renderer, IRenderPass renderPass, RenderPassData data)
    {
        GizmoSceneRenderer.Instance.RenderScene(this, renderer, renderPass, data);
    }

    public byte SceneID { get; }

    public IRenderable? ResolveSelection(PickedObject pickingObject)
    {
        if (pickingObject.ObjectId == X_AXIS_ID.Value)
        {
            return new GizmoAxisRenderable(GizmoAxis.X);
        }
        if (pickingObject.ObjectId == Y_AXIS_ID.Value)
        {
            return new GizmoAxisRenderable(GizmoAxis.Y);
        }
        if (pickingObject.ObjectId == Z_AXIS_ID.Value)
        {
            return new GizmoAxisRenderable(GizmoAxis.Z);
        }

        return null;
    }

    private IShader? GizmoShader
    {
        get
        {
            if (gizmoShader == null)
            {
                var res = ResourceManager.Instance.GetResourceByName("Gizmo");
                if (res != null && ResourceManager.Instance.TryGetResourceByGuid<Engine.Shader>(res.GUID, out var gs))
                {
                    gizmoShader = gs;
                }
            }

            return gizmoShader;
        }
    }

    private IShader? PickingShader
    {
        get
        {
            if (pickingShader == null)
            {
                var res = ResourceManager.Instance.GetResourceByName("Picking");
                if (res != null && ResourceManager.Instance.TryGetResourceByGuid<Engine.Shader>(res.GUID, out var ps))
                {
                    pickingShader = ps;
                }
            }

            return pickingShader;
        }
    }

    public void Initialize(GL gl)
    {
        translationGizmo ??= new TranslationGizmo();
        rotationGizmo ??= new RotationGizmo();
        scaleGizmo ??= new ScaleGizmo();
        
        translationGizmo.Initialize(gl);
        rotationGizmo.Initialize(gl);
        scaleGizmo.Initialize(gl);
    }

    internal unsafe void RenderGeometry(IRenderer renderer, RenderPassData data, IRenderPass renderPass)
    {
        if (currentGizmoType == GizmoController.GizmoType.None) return;

        if (translationGizmo == null || rotationGizmo == null || scaleGizmo == null)
            Initialize(renderer.Gl);

        GizmoBase? activeGizmo = currentGizmoType switch
        {
            GizmoController.GizmoType.Translate => translationGizmo,
            GizmoController.GizmoType.Rotate => rotationGizmo,
            GizmoController.GizmoType.Scale => scaleGizmo,
            _ => null
        };

        if (activeGizmo == null) return;

        bool isPicking = renderPass?.TargetType == RenderTargetType.Picking;
        IShader? shader = isPicking ? PickingShader : GizmoShader;
        
        if (shader == null) return;

        renderer.UseShader(shader);

        if (!isPicking)
        {
            renderer.Gl.Disable(EnableCap.DepthTest);
            renderer.Gl.Enable(EnableCap.Blend);
            renderer.Gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        }

        Matrix4x4 scaledModelMatrix = CalculateScreenSpaceGizmoMatrix(modelMatrix, data.View, data.Projection);

        activeGizmo.Render(renderer.Gl, shader, scaledModelMatrix, data.View, data.Projection, isPicking, SceneID);

        if (!isPicking)
        {
            renderer.Gl.Enable(EnableCap.DepthTest);
            renderer.Gl.Disable(EnableCap.Blend);
        }
    }

    private Matrix4x4 CalculateScreenSpaceGizmoMatrix(Matrix4x4 objectMatrix, Matrix4x4 view, Matrix4x4 projection)
    {
        Vector3 position = new Vector3(objectMatrix.M41, objectMatrix.M42, objectMatrix.M43);
        
        Matrix4x4.Invert(view, out Matrix4x4 invView);
        Vector3 cameraPos = new Vector3(invView.M41, invView.M42, invView.M43);
        float distance = Vector3.Distance(position, cameraPos);
        
        float gizmoScale = distance * GizmoScreenSize;
        
        Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(gizmoScale);
        Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(position);
        
        return scaleMatrix * translationMatrix;
    }

    public void SetGizmo(GizmoController.GizmoType gizmoType, Matrix4x4 objModelMatrix)
    {
        this.currentGizmoType = gizmoType;
        modelMatrix = objModelMatrix;
    }
    
    public void Dispose(GL gl)
    {
        translationGizmo?.Dispose(gl);
        rotationGizmo?.Dispose(gl);
        scaleGizmo?.Dispose(gl);
    }
}

public enum GizmoAxis
{
    X,
    Y,
    Z
}
