using System.Numerics;
using Engine;
using Engine.Logging;

namespace Editor;

public class GizmoController
{
    private readonly GizmoScene gizmoScene;
    private readonly IInputController inputController;
    private GizmoType currentGizmoType = GizmoType.None;
    
    // Drag state
    private bool isDragging = false;
    private bool mouseIsDown = false;
    private GizmoAxis? dragAxis = null;
    private Vector3 dragStartPosition;
    private Vector3 objectStartPosition;
    private Vector2 mouseStartPosition;
    private IRenderable? selectedObject;
    private ICamera? camera;

    public GizmoController(IInputController inputController, SelectionManager selectionManager, 
                          GizmoScene gizmoScene, ICamera camera)
    {
        this.gizmoScene = gizmoScene;
        this.inputController = inputController;
        this.camera = camera;
        
        selectionManager.SelectionChanged += OnSelectionChanged;
        inputController.MouseMove += OnMouseMove;
        inputController.SubscribeToMouseButtonEvent(OnMouseButton);
    }

    private bool OnMouseButton(IInputController.MouseButton button, IInputController.InputState state)
    {
        if (button != IInputController.MouseButton.Left) return false;
        
        if (state == IInputController.InputState.Pressed)
        {
            mouseIsDown = true;
            mouseStartPosition = inputController.GetMousePosition();
            
            //Logger.Log($"Mouse down at {mouseStartPosition}");
        }
        else if (state == IInputController.InputState.Released)
        {
            mouseIsDown = false;
            
            if (isDragging)
            {
                //Logger.Log($"Stopped dragging");
            }
            isDragging = false;
            dragAxis = null;
        }
        
        return false;
    }

    private void OnMouseMove(float x, float y)
    {
        if (!isDragging || dragAxis == null || selectedObject == null || camera == null)
            return;

        Vector2 currentMousePos = new Vector2(x, y);
        Vector2 mouseDelta = currentMousePos - mouseStartPosition;

        //Logger.Log($"Mouse delta: {mouseDelta}, dragging {dragAxis} axis");

        Vector3 movement = CalculateAxisMovement(mouseDelta, dragAxis.Value);
        
        Vector3 newPosition = objectStartPosition + movement;

        UpdateObjectPosition(selectedObject, newPosition);
        
        gizmoScene.SetGizmo(currentGizmoType, selectedObject.ModelMatrix);
    }

    private Vector3 CalculateAxisMovement(Vector2 mouseDelta, GizmoAxis axis)
    {
        if (camera == null) return Vector3.Zero;

        Matrix4x4 viewMatrix = camera.GetView();
        Matrix4x4.Invert(viewMatrix, out Matrix4x4 invView);
        
        Vector3 cameraRight = new Vector3(invView.M11, invView.M12, invView.M13);
        Vector3 cameraUp = new Vector3(invView.M21, invView.M22, invView.M23);

        Vector3 axisVector = axis switch
        {
            GizmoAxis.X => Vector3.UnitX,
            GizmoAxis.Y => Vector3.UnitY,
            GizmoAxis.Z => Vector3.UnitZ,
            _ => Vector3.Zero
        };

        float distanceFromCamera = Vector3.Distance(dragStartPosition, 
            new Vector3(invView.M41, invView.M42, invView.M43));
        float scale = distanceFromCamera * 0.001f;

        Vector3 screenRight = cameraRight;
        Vector3 screenUp = cameraUp;

        float rightAlignment = Math.Abs(Vector3.Dot(axisVector, screenRight));
        float upAlignment = Math.Abs(Vector3.Dot(axisVector, screenUp));

        float movement = 0f;
        if (rightAlignment > upAlignment)
        {
            movement = mouseDelta.X * scale * Math.Sign(Vector3.Dot(axisVector, screenRight));
        }
        else
        {
            movement = -mouseDelta.Y * scale * Math.Sign(Vector3.Dot(axisVector, screenUp));
        }

        return axisVector * movement;
    }

    private Vector3 GetObjectPosition(IRenderable obj)
    {
        Matrix4x4 matrix = obj.ModelMatrix;
        return new Vector3(matrix.M41, matrix.M42, matrix.M43);
    }

    private void UpdateObjectPosition(IRenderable obj, Vector3 newPosition)
    {
        Matrix4x4 currentMatrix = obj.ModelMatrix;
        
        currentMatrix.M41 = newPosition.X;
        currentMatrix.M42 = newPosition.Y;
        currentMatrix.M43 = newPosition.Z;
        
        if (obj is Component component)
        {
           component.GameObject.Transform.Position = newPosition;
        }
        else
        {
            Logger.Log("UpdateObjectPosition failed as object is not a component");
        }
        
    }

    private void OnSelectionChanged(IRenderable? obj)
    {
        if (obj is GizmoAxisRenderable gizmoAxis && mouseIsDown && selectedObject != null)
        {
            isDragging = true;
            dragAxis = gizmoAxis.Axis;
            objectStartPosition = GetObjectPosition(selectedObject);
            dragStartPosition = objectStartPosition;
            
            //Logger.Log($"Started dragging on {gizmoAxis.Axis} axis");
            return;
        }
        
        if (obj == null)
        {
            selectedObject = null;
            gizmoScene.SetGizmo(GizmoType.None, Matrix4x4.Identity);
            return;
        }
        
        if (obj is IGizmoRenderable)
        {
            return;
        }
        
        isDragging = false;
        dragAxis = null;
        
       

        selectedObject = obj;
        
        if (currentGizmoType == GizmoType.None)
        {
            currentGizmoType = GizmoType.Translate;
        }
        
        gizmoScene.SetGizmo(currentGizmoType, obj.ModelMatrix);
    }

    public enum GizmoType
    {
        None,
        Translate,
        Rotate,
        Scale
    }
}