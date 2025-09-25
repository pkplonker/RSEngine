using Engine;
using ImGuiNET;

namespace Editor.Controls
{
	public class MetadataPanel : IPanel
	{
		public string PanelName { get; set; } = "Metadata Viewer";
		private MetadataType? selectedTypeFilter = null;

		public void Draw(IRenderer renderer)
		{
			ImGui.Begin(PanelName);

			if (ImGui.BeginCombo("Filter by Type", selectedTypeFilter?.ToString() ?? "All"))
			{
				foreach (var type in Enum.GetValues(typeof(MetadataType)))
				{
					bool isSelected = (selectedTypeFilter?.Equals(type) ?? false);
					if (ImGui.Selectable(type.ToString(), isSelected))
					{
						selectedTypeFilter = (MetadataType)type;
					}

					if (isSelected)
					{
						ImGui.SetItemDefaultFocus();
					}
				}

				if (ImGui.Selectable("All", selectedTypeFilter == null))
				{
					selectedTypeFilter = null;
				}

				ImGui.EndCombo();
			}

			var metadatas = ResourceManager.Instance.GetMetadata(selectedTypeFilter);
			if (!metadatas.Any())
			{
				ImGui.Text("No metadata available.");
			}
			else
			{
				foreach (var metadata in metadatas)
				{
					if (ImGui.TreeNode($"{Path.GetFileName(metadata.Path) } {metadata.GUID.ToString()}"))
					{
						ImGui.Text($"Path: {metadata.Path}");
						ImGui.Text($"Type: {metadata.MetadataType}");

						ImGui.TreePop();
					}
				}
			}

			ImGui.End();
		}
	}
}