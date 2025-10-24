using System.Collections;
using System.Numerics;
using System.Reflection;
using Editor.Properties;
using Engine;
using Engine.Logging;
using ImGuiNET;

namespace Editor.Controls;

public class PropertyDrawer : IPropertyDrawer
{
	private IRenderer renderer;

	public PropertyDrawer(IRenderer renderer)
	{
		this.renderer = renderer;
	}

	public void CreateNestedHeader(int depth,
		string? name, Action content, IEnumerable<ContextMenuItem> contextMenuItems, Action? handleDragDrop = null)
	{
		if (depth > 0)
		{
			ImGui.Indent();
			Vector4 currentColor = ImGui.GetStyle().Colors[(int) ImGuiCol.Header];

			float darkenFactor = 1.2f;
			Vector4 darkerColor = new Vector4(
				currentColor.X * darkenFactor,
				currentColor.Y * darkenFactor,
				currentColor.Z * darkenFactor,
				currentColor.W);

			ImGui.PushStyleColor(ImGuiCol.Header, darkerColor);
		}

		if (ImGui.CollapsingHeader(name, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.Framed))
		{
			if (contextMenuItems != null && ImGui.BeginPopupContextItem($"context_menu_{name}"))
			{
				foreach (var menuItem in contextMenuItems)
				{
					if (ImGui.MenuItem($"{menuItem?.Name}##{name}_{menuItem?.Name}"))
					{
						menuItem?.Action?.Invoke();
					}
				}

				ImGui.EndPopup();
			}

			handleDragDrop?.Invoke();

			content?.Invoke();
		}

		if (depth > 0)
		{
			ImGui.PopStyleColor();
			ImGui.Unindent();
		}
	}

	public void DrawObject(object? component, string? name = null, int depth = 0)
	{
		if (component == null) return;
		CreateNestedHeader(depth, string.IsNullOrEmpty(name) ? component.GetType().Name : name,
			() => ProcessProps(component, ++depth), null);
	}

	public void ProcessProps(object component, int depth)
	{
		var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
		var type = component.GetType();

		var members = type.GetProperties(flags).Cast<MemberInfo>()
			.Concat(type.GetFields(flags)
				.Where(field =>
				{
					if (field.Name.EndsWith("k__BackingField"))
					{
						return false;
					}

					var attri = field.GetCustomAttributes<InspectableAttribute>().FirstOrDefault();
					if (attri == null)
					{
						return false;
					}

					return attri.Show;
				}))
			.Where(x =>
			{
				var attri = x.GetCustomAttributes<InspectableAttribute>().FirstOrDefault();
				if (attri == null)
				{
					return true;
				}

				return attri.Show;
			})
			.Select<MemberInfo, IMemberAdapter>(memberInfo => { return CreateMemberAdapter(memberInfo); });

		foreach (var memberAdapter in members)
		{
			ProcessMember(component, memberAdapter, depth);
		}
	}

	private static IMemberAdapter CreateMemberAdapter(MemberInfo memberInfo)
	{
		return memberInfo switch
		{
			PropertyInfo prop => new PropertyAdapter(prop),
			FieldInfo field => new FieldAdapter(field),
			_ => throw new InvalidOperationException("Unsupported member type.")
		};
	}

	private void ProcessMember(object component, IMemberAdapter memberInfo, int depth)
	{
		try
		{
			var value = memberInfo.GetValue(component);

			if (value is IEnumerable enumerable && !(value is string))
			{
				foreach (var item in enumerable)
				{
					DrawObject(item, null, depth);
				}
			}
			else if (memberInfo?.MemberType == typeof(Guid))
			{
				var resGuid = memberInfo.GetCustomAttribute<ResourceGuidAttribute>();

				if (resGuid != null)
				{
					ResourceManager.Instance.TryGetResourceByGuid((Guid) memberInfo.GetValue(component),
						out var resource);
					if (CustomEditorLoader.TryGetEditor(resGuid.ResourceGuidType, out var editor))
					{
						editor.Draw(resource, component, memberInfo, renderer, depth);
					}
				}
				else if (CustomEditorLoader.TryGetEditor(memberInfo.GetType(), out var editor))
				{
					editor.Draw(memberInfo.GetValue(component), component, memberInfo, renderer);
				}
				else
				{
					ImGui.Text($"{memberInfo.Name}: {value}");
				}
			}
			else
			{
				var uiAttribute = memberInfo.GetCustomAttribute<UIControlAttribute>();
        
				if (uiAttribute?.ReadOnly == true)
				{
					RenderReadOnlyProperty(memberInfo, component, uiAttribute);
					return;
				}
        
				switch (uiAttribute)
				{
					case RangeAttribute range:
						RenderRangeControl(memberInfo, component, range);
						break;
                
					case SliderAttribute slider:
						RenderSliderControl(memberInfo, component, slider);
						break;
                
					case ColorAttribute color:
						RenderColorControl(memberInfo, component, color);
						break;
                
					case DropdownAttribute dropdown:
						RenderDropdownControl(memberInfo, component, dropdown);
						break;
                
					case TextInputAttribute textInput:
						RenderTextInputControl(memberInfo, component, textInput);
						break;
                
					case CheckboxAttribute checkbox:
						RenderCheckboxControl(memberInfo, component, checkbox);
						break;
                
					case VectorAttribute vector:
						RenderVectorControl(memberInfo, component, vector);
						break;
                
					default:
						RenderDefaultControl(memberInfo, component);
						break;
				}
        
				// Show tooltip if available
				if (!string.IsNullOrEmpty(uiAttribute?.Tooltip) && ImGui.IsItemHovered())
				{
					ImGui.SetTooltip(uiAttribute.Tooltip);
				}
			}
		}
		catch (Exception e)
		{
			Logger.Error($"Failed {e}");
		}
	}
	 private static void RenderRangeControl(IMemberAdapter memberInfo, object component, RangeAttribute range)
    {
        var label = range.Label ?? memberInfo.Name;
        
        if (memberInfo.MemberType == typeof(float))
        {
            var value = (float)memberInfo.GetValue(component);
            var min = range.Min == float.MinValue ? null : (float?)range.Min;
            var max = range.Max == float.MaxValue ? null : (float?)range.Max;
            
            ImGuiHelpers.DrawFloat(label, value, 
                newValue => memberInfo.SetValue(component, newValue),
                min, max, range.Step, range.Speed);
        }
        else if (memberInfo.MemberType == typeof(int))
        {
            var value = (int)memberInfo.GetValue(component);
            ImGuiHelpers.DrawInt(label, value,
                newValue => memberInfo.SetValue(component, newValue),
                (int)range.Min, (int)range.Max, (int)range.Step, range.Speed);
        }
    }
    
    private static void RenderSliderControl(IMemberAdapter memberInfo, object component, SliderAttribute slider)
    {
        var label = slider.Label ?? memberInfo.Name;
        
        if (memberInfo.MemberType == typeof(float))
        {
            var value = (float)memberInfo.GetValue(component);
            if (ImGui.SliderFloat(label, ref value, slider.Min, slider.Max, slider.Format))
            {
                memberInfo.SetValue(component, value);
            }
        }
    }
    
    private static void RenderColorControl(IMemberAdapter memberInfo, object component, ColorAttribute color)
    {
        var label = color.Label ?? memberInfo.Name;
        
        if (memberInfo.MemberType == typeof(Vector4))
        {
            var value = (Vector4)memberInfo.GetValue(component);
            var flags = color.ShowAlpha ? ImGuiColorEditFlags.None : ImGuiColorEditFlags.NoAlpha;
            if (color.HDR) flags |= ImGuiColorEditFlags.HDR;
            
            if (ImGui.ColorEdit4(label, ref value, flags))
            {
                memberInfo.SetValue(component, value);
            }
        }
        else if (memberInfo.MemberType == typeof(Vector3))
        {
            var value = (Vector3)memberInfo.GetValue(component);
            if (ImGui.ColorEdit3(label, ref value))
            {
                memberInfo.SetValue(component, value);
            }
        }
    }
    
    private static void RenderDropdownControl(IMemberAdapter memberInfo, object component, DropdownAttribute dropdown)
    {
        var label = dropdown.Label ?? memberInfo.Name;
        
        if (memberInfo.MemberType == typeof(int))
        {
            var currentIndex = (int)memberInfo.GetValue(component);
            if (ImGui.Combo(label, ref currentIndex, dropdown.Options, dropdown.Options.Length))
            {
                memberInfo.SetValue(component, currentIndex);
            }
        }
        else if (memberInfo.MemberType == typeof(string))
        {
            var currentValue = (string)memberInfo.GetValue(component) ?? "";
            var currentIndex = Array.IndexOf(dropdown.Options, currentValue);
            if (currentIndex == -1) currentIndex = 0;
            
            if (ImGui.Combo(label, ref currentIndex, dropdown.Options, dropdown.Options.Length))
            {
                memberInfo.SetValue(component, dropdown.Options[currentIndex]);
            }
        }
    }
    
    private static void RenderTextInputControl(IMemberAdapter memberInfo, object component, TextInputAttribute textInput)
    {
        var label = textInput.Label ?? memberInfo.Name;
        
        if (memberInfo.MemberType == typeof(string))
        {
            var value = (string)memberInfo.GetValue(component) ?? "";
            
            if (textInput.Multiline)
            {
                if (ImGui.InputTextMultiline(label, ref value, (uint)textInput.MaxLength, new Vector2(0, 100)))
                {
                    memberInfo.SetValue(component, value);
                }
            }
            else
            {
                if (ImGui.InputText(label, ref value, (uint)textInput.MaxLength))
                {
                    memberInfo.SetValue(component, value);
                }
            }
        }
    }
    
    private static void RenderCheckboxControl(IMemberAdapter memberInfo, object component, CheckboxAttribute checkbox)
    {
        var label = checkbox.Label ?? memberInfo.Name;
        
        if (memberInfo.MemberType == typeof(bool))
        {
            var value = (bool)memberInfo.GetValue(component);
            if (ImGui.Checkbox(label, ref value))
            {
                memberInfo.SetValue(component, value);
            }
        }
    }
    
    private static void RenderVectorControl(IMemberAdapter memberInfo, object component, VectorAttribute vector)
    {
        var label = vector.Label ?? memberInfo.Name;
        var min = vector.Min == float.MinValue ? null : (float?)vector.Min;
        var max = vector.Max == float.MaxValue ? null : (float?)vector.Max;
        
        if (memberInfo.MemberType == typeof(Vector2))
        {
            var value = (Vector2)memberInfo.GetValue(component);
            ImGuiHelpers.DrawVec2(label, value,
                newValue => memberInfo.SetValue(component, newValue),
                min, max, vector.Step, vector.Speed);
        }
        else if (memberInfo.MemberType == typeof(Vector3))
        {
            var value = (Vector3)memberInfo.GetValue(component);
            ImGuiHelpers.DrawVec3(label, value,
                newValue => memberInfo.SetValue(component, newValue),
                min, max, vector.Step, vector.Speed);
        }
        else if (memberInfo.MemberType == typeof(Vector4))
        {
            var value = (Vector4)memberInfo.GetValue(component);
            ImGuiHelpers.DrawVec4(label, value,
                newValue => memberInfo.SetValue(component, newValue),
                min, max, vector.Step, vector.Speed);
        }
    }
    
    private static void RenderReadOnlyProperty(IMemberAdapter memberInfo, object component, UIControlAttribute attribute)
    {
        var label = attribute.Label ?? memberInfo.Name;
        var value = memberInfo.GetValue(component)?.ToString() ?? "null";
        
        ImGui.Text($"{label}: {value}");
    }
    
    private static void RenderDefaultControl(IMemberAdapter memberInfo, object component)
    {
        // Fallback to your existing logic
        if (memberInfo?.MemberType == typeof(Vector4))
        {
            ImGuiHelpers.DrawVec4Color(memberInfo.Name, (Vector4)memberInfo.GetValue(component),
                x => memberInfo.SetValue(component, x));
        }
        else if (memberInfo?.MemberType == typeof(float))
        {
            var value = (float)memberInfo.GetValue(component);
            if (ImGui.DragFloat(memberInfo.Name, ref value))
            {
                memberInfo.SetValue(component, value);
            }
        }
        else
        {
            Logger.Error($"Unsupported member type. {memberInfo?.MemberType} for property output");
        }
    }
}