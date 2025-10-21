using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Engine.Logging;

namespace Engine;

public class SelectionManager
{
    private SelectionRenderPass selectionPass;
    public IRenderable? SelectedObject { get; private set; }
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
        
        if (newSelection != SelectedObject)
        {
            SelectedObject = newSelection;
            SelectionChanged?.Invoke(SelectedObject);
        }

        return newSelection;
    }
    
    public void ClearSelection()
    {
        if (SelectedObject != null)
        {
            SelectedObject = null;
            SelectionChanged?.Invoke(null);
            Logger.Info("Selection cleared");
        }
    }
}