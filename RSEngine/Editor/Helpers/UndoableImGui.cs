using System.Text;
using ImGuiNET;

namespace Editor;

public static class UndoableImGui
{
	private static Dictionary<string, float> sliderStates = new();
	private static Dictionary<string, string> textStates = new();
	private const float DEFAULT_LABEL_WIDTH = 400;

	public static bool UndoableButton(string label, Memento memento)
	{
		bool result = false;
		if (ImGui.Button(label))
		{
			UndoManager.RecordAndPerform(memento);
			result = true;
		}

		return result;
	}

	public static void UndoableCheckbox(string label, string actionDescription, Func<bool> getValue,
		Action<bool> setValue)
	{
		bool originalValue = getValue();
		bool currentValue = originalValue;

		if (!label.StartsWith("##"))
		{
			ImGui.Text(label);
		}

		ImGui.SameLine();
		if (ImGui.Checkbox($"##{label}", ref currentValue))
		{
			setValue(currentValue);

			if (originalValue != currentValue)
			{
				UndoManager.RecordAndPerform(new Memento(
					() => setValue(currentValue),
					() => setValue(originalValue),
					actionDescription));
			}
		}
	}

	public static bool UndoableCombo(
		string label,
		string actionDescription,
		Func<int> getCurrentIndex,
		Action<int> setCurrentIndex,
		IEnumerable<string> items,
		float labelWidth = DEFAULT_LABEL_WIDTH,
		bool stretch = true)
	{
		int currentIndex = getCurrentIndex();
		label = DrawLabel(label, labelWidth);
		if (stretch) ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		var originalIndex = currentIndex;
		if (ImGui.Combo(label, ref currentIndex, items.ToArray(), items.Count()))
		{
			UndoManager.RecordAndPerform(new Memento(
				() => setCurrentIndex(currentIndex),
				() => setCurrentIndex(originalIndex),
				actionDescription));
			return true;
		}

		return false;
	}

	private static string DrawLabel(string label, float width)
	{
		if (!label.StartsWith("##"))
		{
			ImGui.Text(label);
			ImGui.SameLine(width);
		}

		label = label.Insert(0, "##");

		return label;
	}

	public static void UndoableDragFloat(
		string label,
		string actionDescription,
		Func<float> getValue,
		Action<float> setValue,
		float min = float.MinValue,
		float max = float.MaxValue,
		float speed = 1.0f,
		float labelWidth = DEFAULT_LABEL_WIDTH,
		bool stretch = true)
	{
		float value = getValue();
		label = DrawLabel(label, labelWidth);
		if (stretch) ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		bool valueChanged = ImGui.DragFloat(label, ref value, speed, min, max);
		HandleSliderState(label, value);
		HandleValueChange(valueChanged, label, value, setValue, actionDescription);
	}

	public static void UndoableDragInt(
		string label,
		string actionDescription,
		Func<int> getValue,
		Action<int> setValue,
		int min = int.MinValue,
		int max = int.MaxValue,
		float speed = 1.0f,
		float labelWidth = DEFAULT_LABEL_WIDTH,
		bool stretch = true)
	{
		int value = getValue();
		label = DrawLabel(label, labelWidth);
		if (stretch) ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		bool valueChanged = ImGui.DragInt(label, ref value, speed, min, max);
		HandleSliderState(label, value);
		HandleValueChange(valueChanged, label, value, setValue, actionDescription);
	}

	private static void HandleValueChange<T>(bool valueChanged, string label, T value, Action<T> setValue,
		string actionDescription)
	{
		if (valueChanged)
		{
			setValue(value);
		}

		if (ImGui.IsItemDeactivatedAfterEdit())
		{
			float originalVal = sliderStates.ContainsKey(label) ? sliderStates[label] : ConvertToFloat(value);

			if (ConvertToFloat(value) != originalVal)
			{
				UndoManager.RecordAndPerform(new Memento(
					() => setValue(value),
					() => setValue(ConvertFromFloat<T>(originalVal)),
					actionDescription));
			}
		}
	}

	private static void HandleValueChange(bool valueChanged, string label, string value, Action<string> setValue,
		string actionDescription)
	{
		if (valueChanged)
		{
			setValue(value);
		}

		if (ImGui.IsItemDeactivatedAfterEdit())
		{
			string originalVal = textStates.ContainsKey(label) ? textStates[label] : value;

			if (value != originalVal)
			{
				UndoManager.RecordAndPerform(new Memento(
					() => setValue(value),
					() => setValue(originalVal),
					actionDescription));
			}
		}
	}

	private static void HandleSliderState<T>(string label, T value)
	{
		if (ImGui.IsItemActivated())
		{
			sliderStates[label] = ConvertToFloat(value);
		}
	}

	private static void HandleTextState(string label, string value)
	{
		if (ImGui.IsItemActivated())
		{
			textStates[label] = value;
		}
	}

	private static float ConvertToFloat<T>(T value)
	{
		try
		{
			return (float) Convert.ChangeType(value, typeof(float));
		}
		catch (InvalidCastException)
		{
			throw new InvalidOperationException($"Cannot convert type {typeof(T).Name} to float.");
		}
	}

	private static T ConvertFromFloat<T>(float value)
	{
		try
		{
			return (T) Convert.ChangeType(value, typeof(T));
		}
		catch (InvalidCastException)
		{
			throw new InvalidOperationException($"Cannot convert float to type {typeof(T).Name}.");
		}
	}

	public static void UndoableTextBox(
		string label,
		string actionDescription,
		Func<string> getValue,
		Action<string> setValue,
		int bufferSize = 100,
		float labelWidth = DEFAULT_LABEL_WIDTH,
		bool stretch = true)
	{
		string value = getValue();
		label = DrawLabel(label, labelWidth);

		byte[] buffer = Encoding.UTF8.GetBytes(value.PadRight(bufferSize, '\0'));

		if (stretch) ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
		bool valueChanged = ImGui.InputText(label, buffer, (uint) buffer.Length);

		string newValue = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
		HandleTextState(label, value);
		HandleValueChange(valueChanged, label, newValue, setValue, actionDescription);
	}
}