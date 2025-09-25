using System.Numerics;
using Editor;
using Engine;
using Engine.Logging;
using Silk.NET.Core;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.Windowing;
using StbImageSharp;

namespace GameCore
{
	public class GameApplication
	{
		public const int WINDOW_SIZE_X = 3840;
		public const int WINDOW_SIZE_Y = 2160;
		private const string WINDOW_NAME = "Luna Game - Stuart Heath - WIP";
		private IWindow? window;
		private IRenderer? renderer;
		private static GameApplication application;

		private static Vector2 LastMousePosition;
		private IKeyboard? primaryKeyboard;
		private IInputController inputController;

		public GameApplication()
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
			renderer.Close();
		}

		public void Start()
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
			}
		}

		private void OnWindowResize(Vector2D<int> size)
		{
			renderer?.Resize(size);
		}

		private void OnLoad()
		{
			SceneController.ActiveScene = new Scene();

			if (window != null)
			{
				renderer?.Load(window);
				ResourceManager.Instance.Init(renderer.Gl, ProjectManager.ActiveProject?.Directory);
				var inputContext = window.CreateInput();
				inputController = new InputController(inputContext);
				inputController.SubscribeToKeyEvent((key, inputState) =>
				{
					if (inputState == IInputController.InputState.Pressed && key == IInputController.Key.Escape)
					{
						window.Close();
						return true;
					}

					return false;
				});

				var cameraGo = new GameObject();
				cameraGo.Transform.Translate(Vector3.UnitZ * 6);
				var cam = cameraGo.AddComponent<PerspectiveCamera>();
				cam.AspectRatio = WINDOW_SIZE_X / (float) WINDOW_SIZE_Y;
				cameraGo.Transform.SetParent(SceneController.ActiveScene);
				SceneController.ActiveScene.AddGameObject(cameraGo);
				SceneController.ActiveScene.ActiveCamera = cam;

				renderer.AddScene(SceneController.ActiveScene, new Vector2D<uint>(0, 0), out _, false);
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
		}

		private void OnRender(double deltaTime)
		{
			renderer?.RenderUpdate();
		}

		private void OnUpdate(double deltaTime)
		{
			Time.Update((float) window.Time);
			SceneController.ActiveScene?.Update();
			LateUpdate();
			PerformanceTracker.ReportAverages();
		}

		private void LateUpdate()
		{
			inputController.Update();
		}
	}
}