using Engine.Logging;

namespace Engine;

public class SelectionManager
{
    private SelectionRenderPass selectionPass;
    private GameObject? selectedObject;
    
    public GameObject? SelectedObject => selectedObject;
    public event Action<GameObject?>? SelectionChanged;
    
    public SelectionManager()
    {
        selectionPass = new SelectionRenderPass();
    }
    
    public SelectionRenderPass GetSelectionPass() => selectionPass;
    
    public void SelectObjectAtPosition(IScene scene, int screenX, int screenY, IRenderer renderer)
    {
        var newSelection = selectionPass.GetObjectAtPosition(scene, screenX, screenY, renderer);
        
        if (newSelection != selectedObject)
        {
            selectedObject = newSelection;
            SelectionChanged?.Invoke(selectedObject);
            
            if (selectedObject != null)
            {
                Logger.Info($"Selected object: {selectedObject.Name} (ID: {selectedObject.ID})");
            }
            else
            {
                Logger.Info("Selection cleared");
            }
        }
    }
    
    public void ClearSelection()
    {
        if (selectedObject != null)
        {
            selectedObject = null;
            SelectionChanged?.Invoke(null);
            Logger.Info("Selection cleared");
        }
    }
}