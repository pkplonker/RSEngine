using Engine;
using Engine.Logging;

namespace Editor;

public static class FileImporter
{
    public static void ImportAllFromDirectory(string? path)
    {
        if (string.IsNullOrEmpty(path) || !Path.Exists(path)) return;
        var paths = ResourceManager.Instance.GetFilesFromFolder(path, FileTypeExtensions.GetAllExtensions());
        int result = paths.Count(p => Import(p));
        Logger.Info($"Imported {result}/{paths.Count()} assets");
    }

    public static bool Import(string path)
    {
        string metadataFilePath = Path.ChangeExtension(path, Metadata.MetadataFileExtension);
        string relativePath = path.MakeProjectRelative();

        if (string.IsNullOrEmpty(relativePath) || ResourceManager.Instance.MetadataExistsWithPath(relativePath))
        {
            return false;
        }

        if (File.Exists(metadataFilePath))
        {
            return false;
        }

        var ext = Path.GetExtension(path);
        Type type = FileTypeExtensions.GetTypeFromExtension(ext);
        var metadata = Metadata.Create(type, relativePath);

        if (!ResourceManager.Instance.AddMetaData(metadata))
        {
            return false;
        }

        metadata.Serialize();
        return true;
    }
}