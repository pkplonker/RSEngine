using System.Collections;

namespace Engine;

public interface IScene : ITransformNode
{
	string Name { get; set; }
	ICamera? ActiveCamera { get; set; }

	static string Extension => ".Scene";

	void Update();
	void Clear();
	void AddGameObject(GameObject cameraGo);
	string Path { get; set; }
	IEnumerable<IRenderable> Renderables { get; }
	byte SceneID { get; }
	private static byte currentID;
	protected static byte GetNextId() => ++currentID;
}