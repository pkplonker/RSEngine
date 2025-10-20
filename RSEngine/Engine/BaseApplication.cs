using System.Numerics;
using Engine;
using Engine.Logging;
using Silk.NET.Core;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using StbImageSharp;

namespace Engine
{
    public abstract class BaseApplication
    {
        public const int WINDOW_SIZE_X = 3840;
        public const int WINDOW_SIZE_Y = 2160;
        
        protected IWindow? window;
        protected IRenderer? renderer;
        protected static Vector2 LastMousePosition;
        protected IInputController inputController;

        protected abstract string WindowName { get; }

        protected BaseApplication()
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
            options.Title = WindowName;
            options.WindowState = GetInitialWindowState();
            window = Window.Create(options);
            window.Load += OnLoad;
            window.Update += OnUpdate;
            window.Render += OnRender;
            window.Resize += OnWindowResize;
            window.Closing += OnClose;

            window.VSync = false;
        }

        protected virtual WindowState GetInitialWindowState()
        {
            return WindowState.Normal;
        }

        protected RawImage LoadIcon(string? filePath)
        {
            byte[] imageData = File.ReadAllBytes(filePath);
            var imageResult = ImageResult.FromMemory(imageData, ColorComponents.RedGreenBlueAlpha);
            return new RawImage(imageResult.Width, imageResult.Height, imageResult.Data);
        }

        protected virtual void OnClose()
        {
            renderer?.Close();
            window.IsClosing = true;
        }

        public virtual void Start()
        {
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

                    OnShutdown();
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

                OnApplicationExit();
            }
        }

        protected virtual void OnShutdown()
        {
            // Override in derived classes for custom shutdown logic
        }

        protected virtual void OnApplicationExit()
        {
            // Override in derived classes for final cleanup
        }

        protected virtual void OnWindowResize(Vector2D<int> size)
        {
            renderer?.Resize(size);
        }

        protected virtual void OnLoad()
        {
            if (window != null)
            {
                renderer?.Load(window);
                ResourceManager.Instance.Init(renderer.Gl, ProjectManager.ActiveProject?.Directory);
                
                var inputContext = window.CreateInput();
                inputController = new InputController(inputContext);
                
                SetupInput();
                SetupRenderer();
                SetupIcon();
                
                OnApplicationLoaded();
            }
            else
            {
                throw new NullReferenceException($"{nameof(window)} cannot be null");
            }
        }

        protected virtual void SetupInput()
        {
            inputController.SubscribeToKeyEvent((key, inputState) =>
            {
                if (inputState == IInputController.InputState.Pressed && key == IInputController.Key.Escape)
                {
                    HandleEscapeKey();
                    return true;
                }

                return HandleCustomKeyInput(key, inputState);
            });
        }

        protected virtual void HandleEscapeKey()
        {
            window?.Close();
        }

        protected virtual bool HandleCustomKeyInput(IInputController.Key key, IInputController.InputState inputState)
        {
            return false;
        }

        protected virtual void SetupRenderer()
        {
            // Override in derived classes for custom renderer setup
        }

        protected virtual void SetupIcon()
        {
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

        protected virtual void OnApplicationLoaded()
        {
            // Override in derived classes for custom initialization after load
        }

        protected virtual void OnRender(double deltaTime)
        {
            renderer?.RenderUpdate();
        }

        protected virtual void OnUpdate(double deltaTime)
        {
            Time.Update((float)window.Time);
            SceneController.ActiveScene?.Update();
            OnCustomUpdate(deltaTime);
            LateUpdate();
            PerformanceTracker.ReportAverages();
        }

        protected virtual void OnCustomUpdate(double deltaTime)
        {
            // Override in derived classes for custom update logic
        }

        protected virtual void LateUpdate()
        {
            inputController.Update();
        }
        
    }
}