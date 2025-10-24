using System.Collections.ObjectModel;
using System.Numerics;
using Editor.Controls;
using Engine;
using Engine.Logging;
using Silk.NET.Core;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace Editor
{
    public class EditorApplication : BaseApplication
    {
        private const string WINDOW_NAME = "Luna Engine - Stuart Heath - WIP";
        
        private static EditorApplication application;
        private EditorImGuiController? imGuiController;
        private IEditorCamera? editorCamera;
        private FileWatcher fileWatcher;
        private SelectionManager selectionManager;
        private ObservableCollection<IScene> activeScenes = new();
        private GizmoController gizmoController;
        
        protected override string WindowName => WINDOW_NAME;

        private EditorApplication() : base()
        {
        }

        protected override WindowState GetInitialWindowState()
        {
            return WindowState.Maximized;
        }

        protected override void OnClose()
        {
            void Close()
            {
                Logger.Flush();
                imGuiController?.Close();
                renderer?.Close();
                window.IsClosing = true;
            }

            window.IsClosing = false;
            DecisionBox.Show("Save Project?", () =>
            {
                ProjectManager.Save();
                EditorImGuiController.SaveScene();
                ResourceManager.Instance.Save();
                Close();
            }, () => Close(), showCancel: true);
        }

        public override void Start()
        {
            EditorSettings.LoadSettings();
            base.Start();
        }

        protected override void OnShutdown()
        {
            if (imGuiController != null)
            {
                imGuiController.Dispose();
                imGuiController = null;
            }
        }

        protected override void OnApplicationExit()
        {
            EditorSettings.SaveSettings();
        }

        protected override void OnWindowResize(Vector2D<int> size)
        {
            base.OnWindowResize(size);
            imGuiController?.Resize(size);
        }

        protected override void HandleEscapeKey()
        {
            OnClose();
        }

        protected override void SetupRenderer()
        {
            IconLoader.Init(renderer.Gl);
            selectionManager = new SelectionManager(activeScenes);
            SetupRenderPasses();
            renderer.OverlayRegistry.RegisterOverlay(new GridOverlay());
        }

        protected override void OnApplicationLoaded()
        {
            var inputContext = window.CreateInput();
    
            editorCamera = new MoveableEditorCamera(new Vector3(0, 4,9), 16f / 9f, selectionManager, new Vector3(-20, 0, 0));
            imGuiController = new EditorImGuiController(renderer.Gl, window, inputContext, renderer, editorCamera,
                inputController, selectionManager);
            var gizmoScene = new GizmoScene();
            gizmoScene.ActiveCamera = editorCamera;
            gizmoController = new GizmoController(inputController,selectionManager, gizmoScene, gizmoScene.ActiveCamera);

            activeScenes.Add(gizmoScene);
    
            SceneController.OnActiveSceneChanged += (newScene, oldScene) =>
            {
                renderer.RemoveScene(oldScene);
                var size = imGuiController.CurrentSize;
                
                if (size.X <= 0 || size.Y <= 0)
                {
                    size = new Vector2(window.Size.X, window.Size.Y);
                }
                
                renderer.AddScene(newScene, new Vector2D<uint>((uint)size.X, (uint)size.Y), out _, true);
                if (newScene != null)
                {
                    newScene.ActiveCamera = editorCamera;
                    renderer.SetRenderTargetSize(newScene, new Vector2D<float>(size.X, size.Y));
                }
                activeScenes.Remove(oldScene);
                activeScenes.Add(newScene);
                activeScenes.Add(gizmoScene);
        
                renderer.ShareRenderTargets(newScene, gizmoScene);
            };

#if DEBUG
            ProjectManager.LoadTestProject();
#endif

            try
            {
                var path = ProjectManager.ActiveProject?.Directory;
                if (!string.IsNullOrEmpty(path))
                {
                    fileWatcher = new FileWatcher(ProjectManager.ActiveProject?.Directory);
                }
            }
            catch (Exception e)
            {
                Logger.Warning($"Failed to init filewatcher {e}");
            }
        }

        private void SetupRenderPasses()
        {
            renderer.RenderPassRegistry.RegisterRenderPass(selectionManager.GetSelectionPass());
            renderer.RenderPassRegistry.RegisterRenderPass(new WireframeDebugPass());
            selectionManager.SelectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(IRenderable? selectedObject)
        {
            
        }

        protected override void OnRender(double deltaTime)
        {
            base.OnRender(deltaTime);
            imGuiController?.Render();
        }

        protected override void OnCustomUpdate(double deltaTime)
        {
            imGuiController?.ImGuiControllerUpdate((float)deltaTime, activeScenes);
            PlayModeManager.Instance.Update((float)deltaTime);
        }

      

        public static EditorApplication GetApplication() => application ??= new EditorApplication();
    }
}