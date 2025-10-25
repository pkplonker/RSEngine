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
    private const string VIEWPORT_RENDERPASS = "ViewportRenderPass";
    
    public event Action<bool> IsActive; 
    private readonly Dictionary<string, float> aspectRatios = new()
    {
        {"16:9(HD/QHD/4K)", 16f / 9f}, {"16:10", 16f / 10f}, {"4:3", 4.0f / 3.0f}, {"32:9", 32.0f / 9.0f}
    };

    private int currentLevel;
    private int currentRenderPassIndex;
    private readonly IInputController inputController;
    private readonly SelectionManager selectionManager;
    private readonly IRenderer iRenderer;
    private bool isViewportHovered;

    public EditorViewport(IRenderer iRenderer, IInputController inputController, SelectionManager selectionManager)
    {
        currentLevel = EditorSettings.GetSetting(VIEWPORT_ASPECTRATIO, "Viewport", true, 0);
        currentRenderPassIndex = EditorSettings.GetSetting(VIEWPORT_RENDERPASS, "Viewport", true, 0);
        
        aspectRatio = aspectRatios.ElementAt(currentLevel).Value;
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

    public void Update(string panelName, IEditorCamera camera, IScene scene, IInputController inputController,
        IRenderer renderer, ref Vector2 currentSize)
    {
        currentSize = this.currentSize;
        ImGui.Begin(panelName,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        var enumValues = Enum.GetValues<RenderTargetType>();
        
        var comboWidth = 300f;
        
        // Left side - Play Mode Controls
        RenderPlayModeControls(scene);
        
        // Middle - Aspect Ratio
        ImGui.SameLine();
        ImGui.SetNextItemWidth(comboWidth);
        UndoableImGui.UndoableCombo("##aspectRatio", "Modified viewport aspect ratio", () => currentLevel,
            (val) =>
            {
                currentLevel = val;
                aspectRatio = aspectRatios.ElementAt(currentLevel).Value;
                EditorSettings.SaveSetting(VIEWPORT_ASPECTRATIO, currentLevel);
            }, aspectRatios.Keys, 0, stretch: false, skipLabel:true);

        // Right side - Render Pass
        ImGui.SameLine();
        ImGui.SetNextItemWidth(comboWidth);
        UndoableImGui.UndoableCombo("##renderPass", "Changed viewport render pass", () => currentRenderPassIndex,
            (val) =>
            {
                currentRenderPassIndex = val;
                EditorSettings.SaveSetting(VIEWPORT_RENDERPASS, currentRenderPassIndex);
            }, Enum.GetNames<RenderTargetType>(), 0, stretch: false, skipLabel: true);
        
        ImGui.Separator();

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

            var selectedRenderTargetType = enumValues[currentRenderPassIndex];
            if (selectedRenderTargetType != RenderTargetType.Main && selectedRenderTargetType != RenderTargetType.Picking)
            {
                renderer.EnsureRenderTarget(scene, selectedRenderTargetType);
            }
            IRenderTarget? rt = renderer.GetSceneRenderTarget(scene, selectedRenderTargetType);
            
            if (rt != null)
            {
                Vector2 offset = new Vector2((size.X - aspectSize.X) * 0.5f,
                    usedHeight + (size.Y - aspectSize.Y) * 0.5f);

                currentOffset = ImGui.GetWindowPos() + offset;
                currentAspectSize = aspectSize;

                ImGui.SetCursorPos(offset);

                IntPtr textureHandle = GetTextureHandle(rt);
                if (textureHandle != IntPtr.Zero)
                {
                    ImGui.Image(textureHandle,
                        (Vector2)aspectSize, new Vector2(0, 1),
                        new Vector2(1, 0),
                        Vector4.One,
                        Vector4.Zero);
                    
                    var imageMin = ImGui.GetItemRectMin();
                    var imageMax = ImGui.GetItemRectMax();
                    isViewportHovered = ImGui.IsMouseHoveringRect(imageMin, imageMax);
                }
                else
                {
                    ImGui.SetCursorPos(offset);
                    ImGui.Button($"No {selectedRenderTargetType} target", (Vector2)aspectSize);
                }
            }
        }

        ImGui.End();
    }

    private void RenderPlayModeControls(IScene scene)
    {
        var playMode = PlayModeManager.Instance.CurrentMode;
        
        var buttonSize = new Vector2(70, 0);
        
        // Play button
        if (playMode == PlayMode.Play)
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.6f, 0.2f, 1f));
        
        if (ImGui.Button("Play", buttonSize))
        {
            if (playMode == PlayMode.Edit && scene is Scene gameScene)
            {
                PlayModeManager.Instance.SetActiveScene(gameScene);
                PlayModeManager.Instance.EnterPlayMode();
            }
        }
        
        if (playMode == PlayMode.Play)
            ImGui.PopStyleColor();
        
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Enter Play Mode (F5)");
        
        ImGui.SameLine();
        
        // Pause button
        ImGui.BeginDisabled(playMode == PlayMode.Edit);
        
        if (playMode == PlayMode.Paused)
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.8f, 0.6f, 0.2f, 1f));
        
        if (ImGui.Button("Pause", buttonSize))
        {
            PlayModeManager.Instance.TogglePause();
        }
        
        if (playMode == PlayMode.Paused)
            ImGui.PopStyleColor();
        
        ImGui.EndDisabled();
        
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Pause/Resume (F6)");
        
        ImGui.SameLine();
        
        // Stop button
        ImGui.BeginDisabled(playMode == PlayMode.Edit);
        
        if (ImGui.Button("Stop", buttonSize))
        {
            PlayModeManager.Instance.ExitPlayMode();
        }
        
        ImGui.EndDisabled();
        
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip("Exit Play Mode (Shift+F5)");
        
        ImGui.SameLine();
        
        // Display current mode
        var modeText = playMode switch
        {
            PlayMode.Edit => "Editing",
            PlayMode.Play => "Playing",
            PlayMode.Paused => "Paused",
            _ => "Unknown"
        };
        
        var modeColor = playMode switch
        {
            PlayMode.Edit => new Vector4(0.7f, 0.7f, 0.7f, 1f),
            PlayMode.Play => new Vector4(0.2f, 1f, 0.2f, 1f),
            PlayMode.Paused => new Vector4(1f, 1f, 0.2f, 1f),
            _ => new Vector4(1f, 1f, 1f, 1f)
        };
        
        ImGui.TextColored(modeColor, $"{modeText}");
    }

    private IntPtr GetTextureHandle(IRenderTarget renderTarget)
    {
        return renderTarget switch
        {
            FrameBufferRenderTarget fbrtt => fbrtt.GetTextureHandlePtr(),
            PickingRenderTarget prt => prt.GetTextureHandlePtr(),
            _ => IntPtr.Zero
        };
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
                // Only do selection on Main render target
                var selectedRenderTargetType = (RenderTargetType)currentRenderPassIndex;
                if (selectedRenderTargetType == RenderTargetType.Main || selectedRenderTargetType == RenderTargetType.Picking)
                {
                    selectionManager.SelectObjectAtPosition((int)framebufferPos.Value.X, (int)framebufferPos.Value.Y, iRenderer);
                }
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