using System.Numerics;
using Engine;
using ImGuiNET;
using Silk.NET.Maths;

namespace Editor.Controls;

public class EditorViewport
{
	private Vector2 currentSize;

	private float aspectRatio;

	private const string VIEWPORT_ASPECTRATIO = "ViewportAspectRatio";
	public event Action<bool> IsActive; 
	private readonly Dictionary<string, float> aspectRatios = new()
	{
		{"16:9(HD/QHD/4K)", 16f / 9f}, {"16:10", 16f / 10f}, {"4:3", 1920.0f / 1080.0f}, {"32:9", 32.0f / 9.0f}
	};

	private int currentLevel;

	public EditorViewport()
	{
		currentLevel = EditorSettings.GetSetting(VIEWPORT_ASPECTRATIO, "Viewport", true, 0);
		aspectRatio = aspectRatios.ElementAt(currentLevel)
			.Value;
	}

	private Vector2D<float> CalculateSizeForAspectRatio(Vector2D<float> currentSize, float aspectRatio)
	{
		float currentAspectRatio = currentSize.X / currentSize.Y;

		float newWidth, newHeight;

		if (currentAspectRatio > aspectRatio)
		{
			newHeight = currentSize.Y;
			newWidth = newHeight * aspectRatio;
		}
		else
		{
			newWidth = currentSize.X;
			newHeight = newWidth / aspectRatio;
		}

		return new Vector2D<float>(newWidth, newHeight);
	}

	public void Update(string panelName, IEditorCamera camera, IScene? scene, IInputController inputController,
		IRenderer renderer, ref Vector2 currentSize)
	{
		currentSize = this.currentSize;
		ImGui.Begin(panelName,
			ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

		UndoableImGui.UndoableCombo("##aspectRatio", "Modified viewport aspect ratio", () => currentLevel,
			(val) =>
			{
				currentLevel = val;
				aspectRatio = aspectRatios.ElementAt(currentLevel).Value;
				EditorSettings.SaveSetting(VIEWPORT_ASPECTRATIO, currentLevel);
			}, aspectRatios.Keys, 300);

		float usedHeight = ImGui.GetCursorPosY();
		Vector2 size = ImGui.GetContentRegionAvail();
		bool isActive = false;
		if (ImGui.IsWindowFocused() || (ImGui.IsWindowHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right)))
		{
			ImGui.SetWindowFocus();
			camera.SetActive(true, inputController);
			IsActive?.Invoke(true);
		}
		else
		{
			camera.SetActive(false, inputController);
			IsActive?.Invoke(false);
		}

		if (scene != null)
		{
			var aspectSize = HandleResize(camera, scene, renderer, size);

			IRenderTarget? rt = renderer.GetSceneRenderTarget(scene);
			if (rt != null && rt is FrameBufferRenderTarget fbrtt)
			{
				Vector2 offset = new Vector2((size.X - aspectSize.X) * 0.5f,
					usedHeight + (size.Y - aspectSize.Y) * 0.5f);

				ImGui.SetCursorPos(offset);

				ImGui.Image(fbrtt.GetTextureHandlePtr(),
					(Vector2) aspectSize, Vector2.Zero,
					Vector2.One,
					Vector4.One,
					Vector4.Zero);
			}
		}

		ImGui.End();
		
	}

	private Vector2D<float> HandleResize(IEditorCamera camera, IScene scene, IRenderer renderer, Vector2 size)
	{
		Vector2D<float> aspectSize =
			CalculateSizeForAspectRatio(new Vector2D<float>(size.X, size.Y), aspectRatio);

		if (size != currentSize)
		{
			renderer.SetRenderTargetSize(scene, aspectSize);
			camera.AspectRatio = aspectRatio;
			currentSize = size;
		}

		return aspectSize;
	}
}