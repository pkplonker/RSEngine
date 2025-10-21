using System.Numerics;
using Engine;
using Engine.Logging;

namespace Editor;

public class GizmoController
{
    private readonly GizmoScene gizmoScene;
    private readonly IInputController inputController;
    private readonly ICamera camera;
    
    // Gizmo handlers
    private readonly Dictionary<GizmoType, IGizmoHandler> handlers;
    private IGizmoHandler? currentHandler;
    
    // State
    private GizmoType currentGizmoType = GizmoType.None;
    private bool isDragging = false;
    private bool mouseIsDown = false;
    private Vector2 mouseStartPosition;
    private IRenderable? selectedObject;

    public GizmoController(IInputController inputController, SelectionManager selectionManager, 
                          GizmoScene gizmoScene, ICamera camera)
    {
        this.gizmoScene = gizmoScene;
        this.inputController = inputController;
        this.camera = camera;
        
        // Initialize handlers
        handlers = new Dictionary<GizmoType, IGizmoHandler>
        {
            { GizmoType.Translate, new TranslationGizmoHandler() },
            { GizmoType.Rotate, new RotationGizmoHandler() },
            { GizmoType.Scale, new ScaleGizmoHandler() }
        };
        
        // Subscribe to events
        selectionManager.SelectionChanged += OnSelectionChanged;
        inputController.MouseMove += OnMouseMove;
        inputController.SubscribeToMouseButtonEvent(OnMouseButton);
        inputController.SubscribeToKeyEvent(OnKeyPress);
    }

    public void Initialize(Silk.NET.OpenGL.GL gl)
    {
        // Initialize all gizmo geometries
        foreach (var handler in handlers.Values)
        {
            handler.GetGizmo().Initialize(gl);
        }
    }

    private bool OnKeyPress(IInputController.Key key, IInputController.InputState state)
    {
        if (selectedObject == null) return false;
        if (state != IInputController.InputState.Pressed) return false;

        GizmoType newType = key switch
        {
            IInputController.Key.W => GizmoType.Translate,
            IInputController.Key.E => GizmoType.Rotate,
            IInputController.Key.R => GizmoType.Scale,
            _ => currentGizmoType
        };

        if (newType != currentGizmoType)
        {
            SwitchGizmoType(newType);
        }

        return false;
    }

    private void SwitchGizmoType(GizmoType newType)
    {
        currentGizmoType = newType;
        currentHandler = handlers.ContainsKey(newType) ? handlers[newType] : null;
        
        if (selectedObject != null)
        {
            gizmoScene.SetGizmo(currentGizmoType, selectedObject.ModelMatrix);
        }
    }

    private bool OnMouseButton(IInputController.MouseButton button, IInputController.InputState state)
    {
        if (button != IInputController.MouseButton.Left) return false;
        
        if (state == IInputController.InputState.Pressed)
        {
            mouseIsDown = true;
            mouseStartPosition = inputController.GetMousePosition();
        }
        else if (state == IInputController.InputState.Released)
        {
            mouseIsDown = false;
            
            if (isDragging)
            {
                currentHandler?.EndDrag();
                Logger.Log($"Finished {currentGizmoType} operation");
            }
            isDragging = false;
        }
        
        return false;
    }

    private void OnMouseMove(float x, float y)
    {
        if (!isDragging || currentHandler == null || selectedObject == null)
            return;

        Vector2 currentMousePos = new Vector2(x, y);
        Vector2 mouseDelta = currentMousePos - mouseStartPosition;

        currentHandler.UpdateDrag(mouseDelta, selectedObject, camera);
        
        gizmoScene.SetGizmo(currentGizmoType, selectedObject.ModelMatrix);
    }

    private void OnSelectionChanged(IRenderable? obj)
    {
        // Handle gizmo axis selection for dragging
        if (obj is GizmoAxisRenderable gizmoAxis && mouseIsDown && selectedObject != null && currentHandler != null)
        {
            isDragging = true;
            currentHandler.StartDrag(selectedObject, gizmoAxis.Axis);
            
            Logger.Log($"Started {currentGizmoType} on {gizmoAxis.Axis} axis");
            return;
        }
        
        // Clear selection
        if (obj == null)
        {
            selectedObject = null;
            gizmoScene.SetGizmo(GizmoType.None, Matrix4x4.Identity);
            return;
        }
        
        // Ignore if selecting the gizmo itself
        if (obj is IGizmoRenderable)
        {
            return;
        }
        
        // Reset drag state
        isDragging = false;
        
        // Set new selection
        selectedObject = obj;
        
        // Default to translate if no gizmo type is set
        if (currentGizmoType == GizmoType.None)
        {
            SwitchGizmoType(GizmoType.Translate);
        }
        else
        {
            gizmoScene.SetGizmo(currentGizmoType, obj.ModelMatrix);
        }
    }

    public void Dispose(Silk.NET.OpenGL.GL gl)
    {
        // Dispose all gizmo geometries
        foreach (var handler in handlers.Values)
        {
            handler.GetGizmo().Dispose(gl);
        }
    }

    public enum GizmoType
    {
        None,
        Translate,
        Rotate,
        Scale
    }
}