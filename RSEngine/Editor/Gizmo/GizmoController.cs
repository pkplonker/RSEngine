using System.Numerics;
using Engine;
using Engine.Logging;

namespace Editor;

public class GizmoController
{
    private readonly GizmoScene gizmoScene;
    private readonly IInputController inputController;
    private GizmoType currentGizmoType = GizmoType.None;
    
    private bool isDragging = false;
    private bool mouseIsDown = false;
    private GizmoAxis? dragAxis = null;
    private Vector3 dragStartPosition;
    private Vector3 objectStartPosition;
    private Quaternion objectStartRotation;
    private Vector3 objectStartScale;
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
        inputController.SubscribeToKeyEvent(OnKeyPress);
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
            currentGizmoType = newType;
            gizmoScene.SetGizmo(currentGizmoType, selectedObject.ModelMatrix);
            Logger.Log($"Switched to {currentGizmoType} gizmo");
        }

        return false;
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
                Logger.Log($"Finished {currentGizmoType} operation on {dragAxis} axis");
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

        switch (currentGizmoType)
        {
            case GizmoType.Translate:
                HandleTranslate(mouseDelta);
                break;
            case GizmoType.Rotate:
                HandleRotate(mouseDelta);
                break;
            case GizmoType.Scale:
                HandleScale(mouseDelta);
                break;
        }
        
        gizmoScene.SetGizmo(currentGizmoType, selectedObject.ModelMatrix);
    }

    private void HandleTranslate(Vector2 mouseDelta)
    {
        if (selectedObject == null || dragAxis == null) return;

        Vector3 movement = CalculateAxisMovement(mouseDelta, dragAxis.Value);
        Vector3 newPosition = objectStartPosition + movement;

        UpdateObjectPosition(selectedObject, newPosition);
    }

   private void HandleRotate(Vector2 mouseDelta)
    {
        if (selectedObject == null || dragAxis == null || !(selectedObject is Component component)) 
            return;

        float rotationAmount = (mouseDelta.X + mouseDelta.Y) * 0.5f;
        float rotationRadians = rotationAmount * 0.01f;

        if (dragAxis.Value == GizmoAxis.Z)
        {
            rotationRadians = -rotationRadians;
        }

        Vector3 rotationAxis = dragAxis.Value switch
        {
            GizmoAxis.X => Vector3.UnitX,
            GizmoAxis.Y => Vector3.UnitY,
            GizmoAxis.Z => Vector3.UnitZ,
            _ => Vector3.UnitY
        };

        Quaternion deltaRotation = Quaternion.CreateFromAxisAngle(rotationAxis, rotationRadians);
        Quaternion newRotation = objectStartRotation * deltaRotation;

        component.GameObject.Transform.Rotation = newRotation;
    }

    private void HandleScale(Vector2 mouseDelta)
    {
        if (selectedObject == null || dragAxis == null || !(selectedObject is Component component)) 
            return;

        float scaleDelta = (mouseDelta.X - mouseDelta.Y) * 0.001f;
        
        if (dragAxis.Value == GizmoAxis.Z)
        {
            scaleDelta = -scaleDelta;
        }
        
        float scaleFactor = 1.0f + scaleDelta;

        Vector3 newScale = dragAxis.Value switch
        {
            GizmoAxis.X => objectStartScale * new Vector3(scaleFactor, 1, 1),
            GizmoAxis.Y => objectStartScale * new Vector3(1, scaleFactor, 1),
            GizmoAxis.Z => objectStartScale * new Vector3(1, 1, scaleFactor),
            _ => objectStartScale * scaleFactor
        };

        newScale = Vector3.Max(newScale, new Vector3(0.001f, 0.001f, 0.001f));

        component.GameObject.Transform.Scale = newScale;
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

    private Quaternion GetObjectRotation(IRenderable obj)
    {
        if (obj is Component component)
        {
            return component.GameObject.Transform.Rotation;
        }
        
        Matrix4x4 matrix = obj.ModelMatrix;
        Matrix4x4.Decompose(matrix, out _, out Quaternion rotation, out _);
        return rotation;
    }

    private Vector3 GetObjectScale(IRenderable obj)
    {
        if (obj is Component component)
        {
            return component.GameObject.Transform.Scale;
        }
        
        Matrix4x4 matrix = obj.ModelMatrix;
        Matrix4x4.Decompose(matrix, out Vector3 scale, out _, out _);
        return scale;
    }

    private void UpdateObjectPosition(IRenderable obj, Vector3 newPosition)
    {
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
            objectStartRotation = GetObjectRotation(selectedObject);
            objectStartScale = GetObjectScale(selectedObject);
            dragStartPosition = objectStartPosition;
            
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