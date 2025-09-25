using ImGuiNET;
using Engine;

namespace Editor.Controls
{
	public class ContentTreeView
	{
		private readonly IContentBrowser contentBrowser;

		public ContentTreeView(IContentBrowser contentBrowser)

		{
			ArgumentNullException.ThrowIfNull(contentBrowser);
			this.contentBrowser = contentBrowser;
		}

		public void Draw()
		{
			var dir = ProjectManager.ActiveProject?.Directory;
			if (!string.IsNullOrEmpty(dir))
			{
				DrawDirectoryTree(dir);
			}
		}

		private void DrawDirectoryTree(string directoryPath)
		{
			foreach (var directory in new DirectoryInfo(directoryPath).GetDirectories())
			{
				var isDirectoryEmpty = !directory.GetDirectories().Any();
				bool isCurrentPath = contentBrowser.CurrentPath.StartsWith(directory.FullName);
				bool nodeOpen = false;

				ImGuiTreeNodeFlags nodeFlags = isCurrentPath ? ImGuiTreeNodeFlags.DefaultOpen  : ImGuiTreeNodeFlags.None;

				if (isDirectoryEmpty)
				{
					ImGui.Bullet();
					ImGui.SameLine();
					if (ImGui.Selectable(directory.Name, contentBrowser.CurrentPath == directory.FullName))
					{
						contentBrowser.CurrentPath = directory.FullName;
					}
				}
				else
				{
					nodeOpen = ImGui.TreeNodeEx(directory.Name, nodeFlags);

					if (ImGui.IsItemClicked())
					{
						contentBrowser.CurrentPath = directory.FullName;
					}

					if (nodeOpen)
					{
						DrawDirectoryTree(directory.FullName);
						ImGui.TreePop();
					}
				}
			}
		}
	}
}