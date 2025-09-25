using System.Numerics;
using System.Runtime.InteropServices;
using Editor.Controls;
using Editor.Properties;
using Engine;
using Engine.Logging;
using ImGuiNET;

namespace Editor.IMGUIControls;

public abstract class BaseCustomEditor : ICustomEditor
{
	protected static PropertyDrawer? propertyDrawer;

	public abstract void Draw(object component, object owningObject, IMemberAdapter memberInfoToSetObjectOnOwner,
		IRenderer renderer, int depth = 0);

	protected virtual void Draw<T>(object component, object owningObject, IMemberAdapter memberInfoToSetObjectOnOwner,
		IRenderer renderer, int depth = 0) where T : class
	{
		propertyDrawer ??= new PropertyDrawer(renderer);
		var dropTarget = () => DropTarget<T>(owningObject, memberInfoToSetObjectOnOwner);
		var test = new List<ContextMenuItem>() {new ContextMenuItem("Test", () => Logger.Info("Testing"))};
		if (component is T comp)
		{
			propertyDrawer.CreateNestedHeader(depth,
				CustomEditorBase.GenerateName<T>(memberInfoToSetObjectOnOwner) ?? component.GetType().Name,
				() => DropProps(comp, memberInfoToSetObjectOnOwner, owningObject, depth),
				test, dropTarget);
		}
		else
		{
			propertyDrawer.CreateNestedHeader(depth,
				CustomEditorBase.GenerateName<T>(memberInfoToSetObjectOnOwner),
				() => DrawEmptyContent(memberInfoToSetObjectOnOwner, dropTarget), test, dropTarget);
		}
	}

	protected abstract void DropProps(object component, IMemberAdapter memberInfoToSetObjectOnOwner,
		object owningObject, int depth = 0);

	private static Vector2 emptyImageSize = new(50, 50);

	protected static unsafe void DropTarget<T>(object component, IMemberAdapter? memberInfo) where T : class
	{
		if (component == null || memberInfo == null) return;
		if (ImGui.BeginDragDropTarget())
		{
			Logger.Info($"hit {component}");
			var payload = ImGui.AcceptDragDropPayload("Metadata");
			if (payload.NativePtr != (void*) IntPtr.Zero)
			{
				var guidPtr = ImGui.AcceptDragDropPayload("Metadata").Data;
				Guid guid = Marshal.PtrToStructure<Guid>(guidPtr);
				Type targetType = typeof(T);
				Type? resourceType = ResourceManager.Instance.GetTypeFromGuid(guid);

				if (targetType.IsAssignableFrom(resourceType))
				{
					memberInfo.SetValue(component, guid);
				}
			}

			ImGui.EndDragDropTarget();
		}
	}

	public void DrawEmptyContent(IMemberAdapter? memberInfo, Action dragDropAction)
	{
		DrawTexture(memberInfo, IconLoader.LoadIcon(@"resources/icons/AddTexture.png".MakeAbsolute()),
			emptyImageSize, dragDropAction);
	}

	protected static void DrawTexture(IMemberAdapter? memberInfo, IntPtr texturePtr, Vector2 size,
		Action drapDropAction)
	{
		if (ImGui.ImageButton($"Select Texture##{memberInfo?.Name}", texturePtr, size))
		{
			Logger.Info("Pressed select texture");
		}

		drapDropAction?.Invoke();
	}
}