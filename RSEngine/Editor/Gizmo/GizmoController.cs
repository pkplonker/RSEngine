using System.Numerics;
using Engine;

namespace Editor;

public class GizmoController
{
    private readonly GizmoScene gizmoScene;
    private readonly SelectionManager selectionManager;
    private GizmoType currentGizmoType = GizmoType.None;

    public GizmoController(SelectionManager selectionManager, GizmoScene gizmoScene)
    {
        this.gizmoScene = gizmoScene;
        this.selectionManager = selectionManager;
        selectionManager.SelectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(IRenderable? obj)
    {
        if (obj is IGizmoRenderable)
        {
            return;
        }
        if (obj == null )
        {
            gizmoScene.SetGizmo(GizmoType.None, Matrix4x4.Identity);
            return;
        }

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