using System.Numerics;
using Editor.Controls;
using Engine;
using Engine.Logging;
using Silk.NET.Core;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using StbImageSharp;

namespace Editor
{
    public class EditorApplication
    {
        public const int WINDOW_SIZE_X = 3840;
        public const int WINDOW_SIZE_Y = 2160;
        private const string WINDOW_NAME = "Luna Engine - Stuart Heath - WIP";
        private IWindow? window;
        private IRenderer? renderer;
        private static EditorApplication application;
        private EditorImGuiController? imGuiController;
        private IEditorCamera? editorCamera;
        private static Vector2 LastMousePosition;
        private IInputController inputController;
        private FileWatcher fileWatcher;
        private SelectionManager selectionManager;

        private EditorApplication()
        {
            Setup();
            renderer = new Renderer();
        }

        private void Setup()
        {
            Logger.Start();
            Logger.AddSink(new ConsoleLogSink());

            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(WINDOW_SIZE_X, WINDOW_SIZE_Y);
            options.Title = WINDOW_NAME;
            options.WindowState = WindowState.Maximized;
            window = Window.Create(options);
            window.Load += OnLoad;
            window.Update += OnUpdate;
            window.Render += OnRender;
            window.Resize += OnWindowResize;
            window.Closing += OnClose;

            window.VSync = false;
        }

        private RawImage LoadIcon(string? filePath)
        {
            byte[] imageData = File.ReadAllBytes(filePath);
            var imageResult = ImageResult.FromMemory(imageData, ColorComponents.RedGreenBlueAlpha);
            return new RawImage(imageResult.Width, imageResult.Height, imageResult.Data);
        }

        private void OnClose()
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

        public void Start()
        {
            EditorSettings.LoadSettings();

            if (window != null)
            {
                try
                {
                    try
                    {
                        window.Run();
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("An error occurred during window run: " + ex);
                    }

                    if (imGuiController != null)
                    {
                        imGuiController.Dispose();
                        imGuiController = null;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error("An error occurred during shutdown: " + ex);
                }
                finally
                {
                    try
                    {
                        window?.Dispose();
                    }
                    catch (Exception e)
                    {
                        Logger.Error("Failed to dispose");
                    }
                }

                EditorSettings.SaveSettings();
            }
        }

        private void OnWindowResize(Vector2D<int> size)
        {
            renderer?.Resize(size);
            imGuiController.Resize(size);
        }

        private void OnLoad()
        {
            if (window != null)
            {
                renderer?.Load(window);
                ResourceManager.Instance.Init(renderer.Gl, ProjectManager.ActiveProject?.Directory);
                IconLoader.Init(renderer.Gl);
                selectionManager = new SelectionManager();
                SetupRenderPasses();
                //renderer.OverlayRegistry.RegisterOverlay(new GridOverlay());
                var inputContext = window.CreateInput();
                inputController = new InputController(inputContext);

                inputController.SubscribeToKeyEvent((key, inputState) =>
                {
                    if (inputState == IInputController.InputState.Pressed && key == IInputController.Key.Escape)
                    {
                        OnClose();
                        return true;
                    }

                    return false;
                });

                editorCamera = new MoveableEditorCamera(new Vector3(0,-5,12), 16f / 9f, selectionManager, new Vector3(14,0,0));
                imGuiController = new EditorImGuiController(renderer.Gl, window, inputContext, renderer, editorCamera,
                    inputController, selectionManager);
                // hack
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
                };

#if DEBUG
                ProjectManager.LoadTestProject();
#endif
                try
                {
                    window.SetWindowIcon(
                        new ReadOnlySpan<RawImage>(LoadIcon(@"/resources/core/TransparentLunaSmall.png"
                            .MakeAbsolute())));
                }
                catch (Exception e)
                {
                    Logger.Error(e);
                }
            }
            else
            {
                throw new NullReferenceException($"{nameof(window)} cannot be null");
            }

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

        private void OnSelectionChanged(GameObject? selectedObject)
        {
            if (selectedObject != null)
            {
                Logger.Info($"Selected: {selectedObject.Name}");
            }
            else
            {
                Logger.Info("Selection cleared");
            }
        }

        private void OnRender(double deltaTime)
        {
            renderer?.RenderUpdate();
            imGuiController?.Render();
        }

        private void OnUpdate(double deltaTime)
        {
            Time.Update((float)window.Time);
            SceneController.ActiveScene?.Update();
            imGuiController?.ImGuiControllerUpdate((float)deltaTime);
            LateUpdate();
            PerformanceTracker.ReportAverages();
        }

        private void LateUpdate()
        {
            inputController.Update();
        }

        public static EditorApplication GetApplication() => application ??= new EditorApplication();
    }
}