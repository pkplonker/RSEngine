namespace Engine;

public interface ITransformNode
{
    GameObject? GameObject { get; }
    public HashSet<ITransformNode> children { get; } 
    void SetParent(ITransformNode? newParent);
    bool HasChildren { get; }
    IReadOnlyList<ITransformNode> GetChildren { get; }
    Guid GUID { get; set; }
    IReadOnlyList<ITransformNode> ChildrenRecursive { get; }
    IEnumerable<GameObject> ChildrenAsGameObjectsRecursive { get; }
    IEnumerable<GameObject> ChildrenAsGameObjects { get; }
}