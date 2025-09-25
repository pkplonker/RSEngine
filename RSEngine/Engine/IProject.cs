namespace Engine;

public interface IProject
{
	string ProjectFilePath { get; set; }
	string? Directory { get; }
	string Name { get; set; }
	string AssetsDirectory { get; set; }
	string CoreAssetsDirectory { get; set; }
	void Save();
	public string ScenePath { get; set; }
}