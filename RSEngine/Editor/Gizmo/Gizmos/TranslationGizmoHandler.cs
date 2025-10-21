using System.Numerics;
using Engine;

namespace Editor;

public class TranslationGizmoHandler : IGizmoHandler
{
    private readonly TranslationGizmo gizmo;
    private Vector3 objectStartPosition;
    private Vector3 dragStartPosition;
    private GizmoAxis? currentAxis;

    public TranslationGizmoHandler()
    {
        gizmo = new TranslationGizmo();
    }

    public GizmoBase GetGizmo() => gizmo;

    public void StartDrag(IRenderable obj, GizmoAxis axis)
    {
        currentAxis = axis;
        objectStartPosition = GetObjectPosition(obj);
        dragStartPosition = objectStartPosition;
    }

    public void UpdateDrag(Vector2 mouseDelta, IRenderable obj, ICamera camera)
    {
        if (currentAxis == null || !(obj is Component component)) return;

        Vector3 movement = CalculateAxisMovement(mouseDelta, currentAxis.Value, camera, dragStartPosition);
        Vector3 newPosition = objectStartPosition + movement;

        component.GameObject.Transform.Position = newPosition;
    }

    public void EndDrag()
    {
        currentAxis = null;
    }

    private Vector3 GetObjectPosition(IRenderable obj)
    {
        Matrix4x4 matrix = obj.ModelMatrix;
        return new Vector3(matrix.M41, matrix.M42, matrix.M43);
    }

    private Vector3 CalculateAxisMovement(Vector2 mouseDelta, GizmoAxis axis, ICamera camera, Vector3 dragStart)
    {
        Matrix4x4 viewMatrix = camera.GetView();
        Matrix4x4.Invert(viewMatrix, out Matrix4x4 invView);
        
        Vector3 cameraRight = new Vector3(invView.M11, invView.M12, invView.M13);
        Vector3 cameraUp = new Vector3(invView.M21, invView.M22, invView.M23);
        Vector3 cameraPos = new Vector3(invView.M41, invView.M42, invView.M43);

        Vector3 axisVector = axis switch
        {
            GizmoAxis.X => Vector3.UnitX,
            GizmoAxis.Y => Vector3.UnitY,
            GizmoAxis.Z => Vector3.UnitZ,
            _ => Vector3.Zero
        };

        float distanceFromCamera = Vector3.Distance(dragStart, cameraPos);
        float scale = distanceFromCamera * 0.001f;

        float rightAlignment = Math.Abs(Vector3.Dot(axisVector, cameraRight));
        float upAlignment = Math.Abs(Vector3.Dot(axisVector, cameraUp));

        float movement = 0f;
        if (rightAlignment > upAlignment)
        {
            movement = mouseDelta.X * scale * Math.Sign(Vector3.Dot(axisVector, cameraRight));
        }
        else
        {
            movement = -mouseDelta.Y * scale * Math.Sign(Vector3.Dot(axisVector, cameraUp));
        }

        return axisVector * movement;
    }
}