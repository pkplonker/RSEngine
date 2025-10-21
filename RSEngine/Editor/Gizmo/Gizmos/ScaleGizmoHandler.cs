using System.Numerics;
using Engine;

namespace Editor;

public class ScaleGizmoHandler : IGizmoHandler
{
    private readonly ScaleGizmo gizmo;
    private Vector3 objectStartScale;
    private GizmoAxis? currentAxis;

    public ScaleGizmoHandler()
    {
        gizmo = new ScaleGizmo();
    }

    public GizmoBase GetGizmo() => gizmo;

    public void StartDrag(IRenderable obj, GizmoAxis axis)
    {
        currentAxis = axis;
        objectStartScale = GetObjectScale(obj);
    }

    public void UpdateDrag(Vector2 mouseDelta, IRenderable obj, ICamera camera)
    {
        if (currentAxis == null || !(obj is Component component)) return;

        float scaleDelta = (mouseDelta.X - mouseDelta.Y) * 0.001f;
        
        // Invert Z scale
        if (currentAxis.Value == GizmoAxis.Z)
        {
            scaleDelta = -scaleDelta;
        }
        
        float scaleFactor = 1.0f + scaleDelta;

        Vector3 newScale = currentAxis.Value switch
        {
            GizmoAxis.X => objectStartScale * new Vector3(scaleFactor, 1, 1),
            GizmoAxis.Y => objectStartScale * new Vector3(1, scaleFactor, 1),
            GizmoAxis.Z => objectStartScale * new Vector3(1, 1, scaleFactor),
            _ => objectStartScale * scaleFactor
        };

        newScale = Vector3.Max(newScale, new Vector3(0.001f, 0.001f, 0.001f));

        component.GameObject.Transform.Scale = newScale;
    }

    public void EndDrag()
    {
        currentAxis = null;
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
}