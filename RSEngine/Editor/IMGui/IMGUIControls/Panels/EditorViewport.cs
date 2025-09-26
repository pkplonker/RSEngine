using System.Numerics;
using Engine;
using ImGuiNET;
using Silk.NET.Maths;

namespace Editor.Controls;

public class EditorViewport
{
	private Vector2 currentSize;
	private Vector2 currentOffset;
	private Vector2D<float> currentAspectSize;
	private float aspectRatio;

	private const string VIEWPORT_ASPECTRATIO = "ViewportAspectRatio";
	public event Action<bool> IsActive; 
	private readonly Dictionary<string, float> aspectRatios = new()
	{
		{"16:9(HD/QHD/4K)", 16f / 9f}, {"16:10", 16f / 10f}, {"4:3", 4.0f / 3.0f}, {"32:9", 32.0f / 9.0f}
	};

	private int currentLevel;
	private readonly IInputController inputController;
	private readonly SelectionManager selectionManager;
	private readonly IRenderer iRenderer;
	private bool isViewportHovered;

	public EditorViewport(IRenderer iRenderer,IInputController inputController, SelectionManager selectionManager)
	{
		currentLevel = EditorSettings.GetSetting(VIEWPORT_ASPECTRATIO, "Viewport", true, 0);
		aspectRatio = aspectRatios.ElementAt(currentLevel)
			.Value;
		this.inputController = inputController;
		this.selectionManager = selectionManager;
		this.iRenderer = iRenderer;
		inputController.SubscribeToMouseButtonEvent(HandleMousePress);

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

          IRenderTarget? rt = renderer.GetSceneRenderTarget(scene, RenderTargetType.Main);
          if (rt != null && rt is FrameBufferRenderTarget fbrtt)
          {
             Vector2 offset = new Vector2((size.X - aspectSize.X) * 0.5f,
                usedHeight + (size.Y - aspectSize.Y) * 0.5f);

             // Store current viewport bounds for mouse coordinate conversion
             currentOffset = ImGui.GetWindowPos() + offset;
             currentAspectSize = aspectSize;

             ImGui.SetCursorPos(offset);

             ImGui.Image(fbrtt.GetTextureHandlePtr(),
                (Vector2) aspectSize, Vector2.Zero,
                Vector2.One,
                Vector4.One,
                Vector4.Zero);
                
             // Check if mouse is over the viewport image
             var imageMin = ImGui.GetItemRectMin();
             var imageMax = ImGui.GetItemRectMax();
             isViewportHovered = ImGui.IsMouseHoveringRect(imageMin, imageMax);
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

	private bool HandleMousePress(IInputController.MouseButton button, IInputController.InputState state)
	{
		if (button == IInputController.MouseButton.Left && 
		    state == IInputController.InputState.Pressed && 
		    isViewportHovered)
		{
			var mousePos = ImGui.GetMousePos();
			var framebufferPos = ScreenToFramebuffer(mousePos);
          
			if (framebufferPos.HasValue)
			{
				selectionManager.SelectObjectAtPosition(SceneController.ActiveScene,
					(int)framebufferPos.Value.X, (int)framebufferPos.Value.Y, iRenderer);
			}
          
			return true;
		}
       
		return false;
	}
	private Vector2? ScreenToFramebuffer(Vector2 screenPos)
	{
		if (!isViewportHovered) return null;
       
		float relativeX = screenPos.X - currentOffset.X;
		float relativeY = screenPos.Y - currentOffset.Y;
       
		if (relativeX < 0 || relativeX > currentAspectSize.X || 
		    relativeY < 0 || relativeY > currentAspectSize.Y)
			return null;
       
		return new Vector2(relativeX, relativeY);
	}
    
	public void Dispose()
	{
		inputController.UnsubscribeToMouseButtonEvent(HandleMousePress);
	}
}