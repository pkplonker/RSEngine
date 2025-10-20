using System.Collections;

namespace Engine;

public interface IScene
{
	ICamera? ActiveCamera { get; set; }
	string Name { get; set; }

	string Path { get; set; }
	void RenderUsing(IRenderer renderer, IRenderPass renderPass, RenderPassData data);
	byte SceneID { get; }
	private static byte currentID;
	protected static byte GetNextId() => ++currentID;
	IRenderable? ResolveSelection(PickedObject pickingObject);
}

public interface IGameObjectScene : IScene, ITransformNode
{
	static string Extension => ".Scene";

	IEnumerable<IRenderable> Renderables { get; }
	void Update();
	void Clear();
}