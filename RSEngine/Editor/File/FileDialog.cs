using System.Diagnostics;
using System.Text;
using Engine;
using Engine.Logging;

namespace Editor;

public static class FileDialog
{
	public static IEnumerable<string> OpenFileDialog(string? filterString = "", bool multiSelect = false)
	{
		OpenFileDialog openFileDialog = new OpenFileDialog();
		openFileDialog.InitialDirectory = ProjectManager.ActiveProject?.Directory ?? string.Empty;
		openFileDialog.Filter = filterString ?? "";
		openFileDialog.FilterIndex = 2;
		openFileDialog.RestoreDirectory = true;
		openFileDialog.Multiselect = multiSelect;
		if (openFileDialog.ShowDialog() == DialogResult.OK)
		{
			return openFileDialog.FileNames;
		}

		return Enumerable.Empty<string>();
	}

	public static string SelectFolderDialog(string initialDirectory = "")
	{
		using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
		{
			folderBrowserDialog.SelectedPath = initialDirectory;
			folderBrowserDialog.Description = "Select a folder for the new project";
			folderBrowserDialog.ShowNewFolderButton = true;

			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
			{
				return folderBrowserDialog.SelectedPath;
			}
		}

		return string.Empty;
	}

	public static string BuildFileDialogFilter(IEnumerable<string> fileExtensions)
	{
		var filterBuilder = new StringBuilder();

		if (fileExtensions.Any())
		{
			string combinedExtensions = string.Join(";", fileExtensions.Select(ext => "*." + ext.TrimStart('.')));
			filterBuilder.AppendFormat("Supported Files ({0})|{0}", combinedExtensions);
		}
		else
		{
			filterBuilder.Append("All files (*.*)|*.*");
		}

		return filterBuilder.ToString();
	}

	public static string? FilterByType(Type? MemberType) =>
		BuildFileDialogFilter(FileTypeExtensions.GetExtensions(MemberType));

	public static void Import()
	{
		var paths = FileDialog.OpenFileDialog(
			FileDialog.BuildFileDialogFilter(FileTypeExtensions.GetAllExtensions()), true);
		int result = paths.Count(FileImporter.Import);

		Logger.Info($"Imported {result}/{paths.Count()} assets");
	}
	public static void OpenFileWithDefaultApp(string? filePath)
	{
		try
		{
			Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
		}
		catch (Exception ex)
		{
			Console.WriteLine("An error occurred: " + ex);
		}
	}
}