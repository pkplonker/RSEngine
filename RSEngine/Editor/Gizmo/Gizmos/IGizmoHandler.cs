using System.Numerics;
using Engine;

namespace Editor;

// Base interface for all gizmo handlers
public interface IGizmoHandler
{
    GizmoBase GetGizmo();
    void StartDrag(IRenderable obj, GizmoAxis axis);
    void UpdateDrag(Vector2 mouseDelta, IRenderable obj, ICamera camera);
    void EndDrag();
}