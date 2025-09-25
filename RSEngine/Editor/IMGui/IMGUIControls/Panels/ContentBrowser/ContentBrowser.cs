using Engine;
using ImGuiNET;
using System.Numerics;

namespace Editor.Controls
{
	public class ContentBrowser : IPanel, IContentBrowser
	{
		public string PanelName { get; set; } = "Content Browser";
		public string? CurrentPath { get; set; } = string.Empty;
		private readonly ContentThumbnailView contentThumbnailView;
		private readonly ContentTreeView contentTreeView;

		private float leftPanelWidth = 375;
		private int splitterThickness = 10;
		private bool isDragging;
		private float minimumWidth = 100;

		public ContentBrowser(Action<IInspectable> selectionAction)
		{
			contentThumbnailView = new ContentThumbnailView(this, selectionAction);
			contentTreeView = new ContentTreeView(this);

			ProjectManager.ProjectChanged += OnProjectChanged;
			if (string.IsNullOrEmpty(CurrentPath))
			{
				CurrentPath = ProjectManager.ActiveProject?.Directory ?? string.Empty;
			}
		}

		private void OnProjectChanged(IProject? obj)
		{
			CurrentPath = obj?.Directory ?? string.Empty;
		}

		public void Draw(IRenderer renderer)
		{
			if (string.IsNullOrEmpty(CurrentPath))
			{
				CurrentPath = ProjectManager.ActiveProject?.Directory ?? string.Empty;
			}

			ImGui.Begin(PanelName);

			DrawTreePane();
			ImGui.SameLine();
			DrawSplitter();
			ImGui.SameLine();
			DrawThumbnailPane();

			ImGui.End();
		}

		private void DrawTreePane()
		{
			ImGui.BeginChild("LeftPanel", new Vector2(leftPanelWidth, 0), false);
			contentTreeView.Draw();
			ImGui.EndChild();
		}

		private void DrawThumbnailPane()
		{
			ImGui.BeginChild("RightPanel", new Vector2(0, 0), false);
			contentThumbnailView.Draw();
			ImGui.EndChild();
		}

		private void DrawSplitter()
		{
			Vector2 splitterPos = ImGui.GetCursorScreenPos();
			ImGui.GetWindowDrawList().AddRectFilled(splitterPos,
				new Vector2(splitterPos.X + splitterThickness, splitterPos.Y + ImGui.GetWindowHeight()),
				ImGui.GetColorU32(ImGuiCol.Button));

			ImGui.SetCursorScreenPos(splitterPos);
			ImGui.InvisibleButton("##splitter", new Vector2(splitterThickness, ImGui.GetWindowHeight()));

			if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0))
			{
				leftPanelWidth += ImGui.GetIO().MouseDelta.X;
				isDragging = true;
			}
			else if (!ImGui.IsMouseDragging(0) && isDragging)
			{
				isDragging = false;
			}

			leftPanelWidth = Math.Max(leftPanelWidth, minimumWidth);
			
			//this stops the windowWidth being a bit wonky before everything is fully constructed
			float windowWidth = ImGui.GetWindowWidth();
			if (windowWidth > minimumWidth)
			{
				leftPanelWidth = Math.Min(leftPanelWidth, windowWidth - (minimumWidth * 2));
			}
		}

		public static bool IsHigherUpInTheDirectory(string? path1, string? path2)
		{
			if (string.IsNullOrEmpty(path1) || string.IsNullOrEmpty(path2)) return false;
			var absolutePath1 = Path.GetFullPath(path1);
			var absolutePath2 = Path.GetFullPath(path2);

			return absolutePath2.StartsWith(absolutePath1, StringComparison.OrdinalIgnoreCase);
		}
	}
}