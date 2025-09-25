using Silk.NET.Assimp;

namespace Engine;

[Inspectable]
public class Scene : Transform, IScene
{
	public Scene() : base(null) { }
	public string Name { get; set; } = "Default Scene";
	public ICamera? ActiveCamera { get; set; }

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

	private void ClearRelationshipsRecursive(Transform node)
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
}