using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Engine.Logging;

namespace Engine;

public class SelectionManager
{
    private SelectionRenderPass selectionPass;
    private IRenderable? selectedObject;
    private readonly ObservableCollection<IScene> activeScenes;

    public event Action<IRenderable?>? SelectionChanged;
    
    public SelectionManager(ObservableCollection<IScene> activeScenes)
    {
        selectionPass = new SelectionRenderPass();
        activeScenes.CollectionChanged += OnActiveScenesChanged;
        this.activeScenes = activeScenes;
    }

    private void OnActiveScenesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        
    }

    public SelectionRenderPass GetSelectionPass() => selectionPass;
    
    public IRenderable SelectObjectAtPosition(int screenX, int screenY, IRenderer renderer)
    {
        var newSelection = selectionPass.GetObjectAtPosition(activeScenes, screenX, screenY, renderer);
        
        if (newSelection != selectedObject)
        {
            selectedObject = newSelection;
            SelectionChanged?.Invoke(selectedObject);
        }

        return newSelection;
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