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
            activeScenes.Add(new GizmoScene());
            SceneController.OnActiveSceneChanged += (newScene, oldScene) =>
            {
                renderer.RemoveScene(oldScene);
                renderer.AddScene(newScene, new Vector2D<uint>(0, 0), out _, true);
                var size = imGuiController.CurrentSize;
                if (newScene != null)
                {
                    newScene.ActiveCamera = editorCamera;
                    renderer.SetRenderTargetSize(SceneController.ActiveScene, new Vector2D<float>(size.X, size.Y));
                }
                activeScenes.Remove(oldScene);
                activeScenes.Add(newScene);
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
            renderer.RenderPassRegistry.RegisterRenderPass(new MainWithWireframePass());
            selectionManager.SelectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(IRenderable? selectedObject)
        {
            if (selectedObject is Component component)
            {
                Logger.Info($"Selected: {component.GameObject.Name}");
            }
            else
            {
                Logger.Info("Selection cleared");
            }
        }

        protected override void OnRender(double deltaTime)
        {
            base.OnRender(deltaTime);
            imGuiController?.Render();
        }

        protected override void OnCustomUpdate(double deltaTime)
        {
            imGuiController?.ImGuiControllerUpdate((float)deltaTime, activeScenes);
        }

        private ObservableCollection<IScene> activeScenes = new();

        public static EditorApplication GetApplication() => application ??= new EditorApplication();
    }
}