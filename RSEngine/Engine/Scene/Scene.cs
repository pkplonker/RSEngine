namespace Engine;

[Inspectable]
public class Scene : Transform, IGameObjectScene
{
    public string Path { get; set; } = string.Empty;
    public byte SceneID { get; set; }
    private static readonly StandardSceneRenderer sceneRenderer = new StandardSceneRenderer();
    
    public void RenderUsing(IRenderer renderer, IRenderPass renderPass, RenderPassData data)
    {
        sceneRenderer.RenderScene(this, renderer, renderPass, data);
    }


    public IEnumerable<IRenderable> Renderables =>
        ChildrenAsGameObjectsRecursive
            .Select(x => x.TryGetComponent<MeshRenderer>(out var component) ? component : null)
            .WhereNotNull()
            .OfType<IRenderable>();

    public Scene(string? name = "") : base(null)
    {
        if (!string.IsNullOrEmpty(name))
        {
            this.Name = name;
        }

        SceneID = IScene.GetNextId();
    }


    private string name = "Default Scene";

    public string Name
    {
        get => name;
        set
        {
            if (value != name)
            {
                this.name = value;
                if (!string.IsNullOrEmpty(ProjectManager.ActiveProject?.Directory))
                {
                    Path = System.IO.Path.Combine(ProjectManager.ActiveProject?.Directory, $"{Name}{IGameObjectScene.Extension}");
                }
            }
        }
    }

    private ICamera? activeCamera;

    public ICamera? ActiveCamera
    {
        get
        {
            if (activeCamera != null)
                return activeCamera;

            return ChildrenAsGameObjectsRecursive
                .Select(go => go.GetComponent<Camera>())?
                .FirstOrDefault(camera => camera?.Main ?? false) ?? null;
        }
        set { activeCamera = value; }
    }

    public void Update()
    {
        foreach (var go in ChildrenAsGameObjectsRecursive)
        {
            go?.Update();
        }
    }

    public void Clear()
    {
        ClearRelationshipsRecursive(this);
    }

    private void ClearRelationshipsRecursive(ITransformNode node)
    {
        if (node == null) return;

        foreach (var child in node.ChildrenRecursive)
        {
            node.SetParent(null);
            ClearRelationshipsRecursive(child);
        }

        children.Clear();
    }

    public void AddGameObject(GameObject cameraGo)
    {
        cameraGo.Transform.SetParent(this);
    }

    public IRenderable? ResolveSelection(PickedObject pickingObject) =>
        Renderables.FirstOrDefault(r => r?.RenderID == pickingObject.ObjectId);
}