using System.Numerics;
using Engine;

namespace Editor;

public class RotationGizmoHandler : IGizmoHandler
{
    private readonly RotationGizmo gizmo;
    private Quaternion objectStartRotation;
    private GizmoAxis? currentAxis;

    public RotationGizmoHandler()
    {
        gizmo = new RotationGizmo();
    }

    public GizmoBase GetGizmo() => gizmo;

    public void StartDrag(IRenderable obj, GizmoAxis axis)
    {
        currentAxis = axis;
        objectStartRotation = GetObjectRotation(obj);
    }

    public void UpdateDrag(Vector2 mouseDelta, IRenderable obj, ICamera camera)
    {
        if (currentAxis == null || !(obj is Component component)) return;

        float rotationAmount = (mouseDelta.X + mouseDelta.Y) * 0.5f;
        float rotationRadians = rotationAmount * 0.01f;

        // Invert Z rotation
        if (currentAxis.Value == GizmoAxis.Z)
        {
            rotationRadians = -rotationRadians;
        }

        Vector3 rotationAxis = currentAxis.Value switch
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

    public void EndDrag()
    {
        currentAxis = null;
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
}