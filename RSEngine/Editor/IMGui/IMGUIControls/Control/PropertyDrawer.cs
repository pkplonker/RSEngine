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
			if (contextMenuItems != null && ImGui.BeginPopupContextItem("context_menu"))
			{
				foreach (var menuItem in contextMenuItems)
				{
					if (ImGui.MenuItem($"{menuItem?.Name}##"))
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
			else
			{
				if (memberInfo?.MemberType == typeof(Guid))
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
			}
		}
		catch (Exception e)
		{
			Logger.Error($"Failed {e}");
		}
	}
}