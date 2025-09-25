namespace Engine;

public interface IScene : ITransform
{
	string Name { get; set; }
	ICamera? ActiveCamera { get; set; }

	static string Extension => ".Scene";

	void Update();
	void Clear();
	void AddGameObject(GameObject cameraGo);
	string Path { get; set; }
}