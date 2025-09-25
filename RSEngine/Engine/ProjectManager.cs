using Editor;
using Engine.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace Engine;

public static class ProjectManager
{
    private static IProject? activeProject;

    private const string TestProjectPath =
        @"C:\Users\pkplo\Documents\CodeProjects\LunaEngine\LunaEngine\Projects\Test2\Test2\Test2.LunaProject";

    public static event Action<IProject?> ProjectChanged;
    public static event Action<IProject?> ProjectCreated;

    static ProjectManager()
    {
    }
#if DEBUG
    public static void LoadTestProject()
    {
        LoadProject(TestProjectPath);
    }
#endif
    public static IProject? ActiveProject
    {
        get => activeProject;
        set
        {
            if (activeProject != value)
            {
                activeProject = value;
                ProjectChanged?.Invoke(ActiveProject);
            }
        }
    }

    public static string ProjectExtension { get; } = ".RSProject";

    public static bool CreateNewProject(string dir, string name, bool setActive = true)
    {
        var combinedPath = Path.Join(dir, name);
        if (Path.Exists(combinedPath))
        {
            Logger.Warning("Not a valid directory");
            return false;
        }

        Directory.CreateDirectory(combinedPath);
        var projectFilePath = Path.Combine(combinedPath, name + ProjectExtension);
        var assetsDirectory = Path.Combine(combinedPath, "assets");
        Directory.CreateDirectory(assetsDirectory);
        var coreAssetsDirectory = Path.Combine(combinedPath, "core/assets");
        Directory.CreateDirectory(coreAssetsDirectory);

        IProject newProject = new Project(projectFilePath, name, assetsDirectory, coreAssetsDirectory);
        ActiveProject = newProject;
        File.WriteAllText(projectFilePath, JsonConvert.SerializeObject(newProject, Formatting.Indented));
        Logger.Info($"New project created at {projectFilePath}");
        if (setActive) ActiveProject = newProject;
        ProjectCreated?.Invoke(newProject);
        SceneController.ActiveScene = new Scene();
        return true;
    }

    public static IProject? LoadProject(string? path)
    {
        void Warning()
        {
            Logger.Warning($"Project file not found at path: {path}");
        }

        if (string.IsNullOrEmpty(path))
        {
            Warning();
            return null;
        }

        if (!File.Exists(path))
        {
            Warning();
            return null;
        }

        try
        {
            string json = File.ReadAllText(path);
            Project? project = JsonConvert.DeserializeObject<Project>(json);

            if (project != null)
            {
                ActiveProject = project;
                if (!string.IsNullOrEmpty(project.ScenePath))
                {
                    var scene = new SceneDeserializer(project.ScenePath.MakeProjectAbsolute()).Deserialize();
                    SceneController.ActiveScene = scene;
                }
            }

            return project;
        }
        catch (JsonException ex)
        {
            Logger.Error($"Failed to load project from path {path}: {ex}");
            return null;
        }
    }

    public static void Save()
    {
        ActiveProject.ScenePath = SceneController.ActiveScene.Path;
        ActiveProject?.Save();
    }
}