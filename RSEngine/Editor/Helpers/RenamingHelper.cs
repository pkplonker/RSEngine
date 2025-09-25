using Engine;
using ImGuiNET;

namespace Editor;

public class RenamingHelper
{
	private WeakReference<GameObject> objectToRename;
	private byte[] renameBuffer = new byte[256];
	private bool showRenamePopup;
	private bool isOpen;
	private const int RENAME_BUFFER_SIZE = 256;

	public RenamingHelper()
	{
		showRenamePopup = false;
		Array.Clear(renameBuffer, 0, renameBuffer.Length);
	}

	public void RequestRename(GameObject? objectToRename)
	{
		if (objectToRename == null) return;
		this.objectToRename = new WeakReference<GameObject>(objectToRename);
		Array.Clear(renameBuffer, 0, renameBuffer.Length);
		byte[] nameBytes = System.Text.Encoding.UTF8.GetBytes(objectToRename.Name);
		Array.Copy(nameBytes, renameBuffer, Math.Min(RENAME_BUFFER_SIZE - 1, nameBytes.Length));
		renameBuffer[RENAME_BUFFER_SIZE - 1] = 0;

		showRenamePopup = true;
	}

	public void DrawRenamePopup()
	{
		if (showRenamePopup)
		{
			ImGui.OpenPopup("Rename Object");
			showRenamePopup = false;
			isOpen = true;
		}

		if (ImGui.BeginPopupModal("Rename Object", ref isOpen, ImGuiWindowFlags.AlwaysAutoResize))
		{
			ImGui.InputText("##rename", renameBuffer, (uint) renameBuffer.Length);

			if (ImGui.Button("OK"))
			{
				GameObject obj;
				if (objectToRename.TryGetTarget(out obj))
				{
					obj.Name = System.Text.Encoding.UTF8.GetString(renameBuffer).TrimEnd('\0');
				}

				isOpen = false;
				ImGui.CloseCurrentPopup();
			}

			ImGui.SameLine();

			if (ImGui.Button("Cancel"))
			{
				ImGui.CloseCurrentPopup();
			}

			ImGui.EndPopup();
		}
	}
}