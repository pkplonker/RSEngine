using Engine;
using Engine.Logging;
using ImGuiNET;

namespace Editor.Controls;

public class SettingsPanel : IPanel
{
	public string PanelName { get; set; } = "Settings";
	private string settingsCategory = "Test Params";
	private readonly IInputController inputController;
	private bool isPanelOpen = false;
	private bool requestClose;

	public SettingsPanel(IInputController inputController)
	{
		this.inputController = inputController;
		string s = EditorSettings.GetSetting("Test String", settingsCategory, true, "Testing123");
		float f = EditorSettings.GetSetting("Test Float", settingsCategory, true, 5f);

		int i = EditorSettings.GetSetting("Test Int", settingsCategory, true, (int) 1);
		bool b = EditorSettings.GetSetting("Test Bool", settingsCategory, true, true);
	}

	private bool HandleKeyPress(IInputController.Key key, IInputController.InputState inputState)
	{
		if (isPanelOpen && key == IInputController.Key.Escape && inputState == IInputController.InputState.Pressed)
		{
			requestClose = true;
			return true;
		}

		return false;
	}

	private void OpenPanel()
	{
		isPanelOpen = true;
		inputController.SubscribeToKeyEvent(HandleKeyPress);
	}

	private void ClosePanel()
	{
		isPanelOpen = false;
		inputController.UnsubscribeToKeyEvent(HandleKeyPress);
		ImGui.CloseCurrentPopup();
		requestClose = false;
	}

	public void Draw(IRenderer renderer)
	{
		if (ImGui.BeginPopupModal(PanelName))
		{
			if (!isPanelOpen)
			{
				OpenPanel();
			}

			if (requestClose || ImGui.Button("Close"))
			{
				ClosePanel();
			}

			ImGui.Separator();
			if (ImGui.BeginTabBar("category"))
			{
				foreach (var category in EditorSettings.settingsDict.Values.Where(x => x.Exposed)
					         .Select(x => x.Category).Distinct())
				{
					if (ImGui.BeginTabItem(category))
					{
						foreach (var setting in EditorSettings.settingsDict.Values.Where(x =>
							         x.Category == category && x.Exposed))
						{
							ImGui.Text(setting.Name);
							ImGui.SameLine();
							Logger.Log($"{setting} {setting.GetHashCode()}");

							switch (setting.Prop)
							{
								case int intValue:
									UndoableImGui.UndoableDragInt($"##{setting.Name}",
										$"Modified Setting {setting.Name} ",
										() => (int) setting.Prop,
										(newValue) => setting.Prop = newValue,
										labelWidth: 200);
									break;
								case float floatValue:
									UndoableImGui.UndoableDragFloat($"##{setting.Name}",
										$"Modified Setting {setting.Name} ",
										() => (float) setting.Prop,
										(newValue) => setting.Prop = newValue,
										labelWidth: 200);
									break;
								case bool boolValue:
									UndoableImGui.UndoableCheckbox($"##{setting.Name}",
										$"Modified Setting {setting.Name} ",
										() => (bool) setting.Prop,
										(newValue) => setting.Prop = newValue);
									break;

								case string stringValue:
									UndoableImGui.UndoableTextBox(
										$"##{setting.Name}",
										$"Modified Setting - {setting.Name})",
										() => (string) setting.Prop,
										(newValue) => setting.Prop = newValue
									);
									break;

								default:
									// Handle unknown types
									Console.WriteLine("Unknown type");
									break;
							}
						}

						ImGui.EndTabItem();
					}
				}
			}

			ImGui.EndPopup();
		}
	}
}