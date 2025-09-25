using System.Numerics;
using Engine;

namespace Game
{
	public class Application
	{
		public const int WINDOW_SIZE_X = 3840;
		public const int WINDOW_SIZE_Y = 2160;
		private const string WINDOW_NAME = "Luna Engine";

		private IWindow? window;
		private Renderer? renderer;
		private static Application application;
		private Camera camera;
		private IInputContext input;

		private Application()
		{
			SetupWindow();
		}

		private void SetupWindow()
		{
			var options = WindowOptions.Default;
			options.Size = new Vector2D<int>(WINDOW_SIZE_X, WINDOW_SIZE_Y);
			options.Title = WINDOW_NAME;
			window = Window.Create(options);

			window.Load += OnLoad;
			window.Update += OnUpdate;
			window.Render += OnRender;
			window.Resize += OnWindowResize;
			window.Closing += OnClose;
			input = window.CreateInput();
			camera = new Camera(Vector3.UnitZ * 6, Vector3.UnitZ * -1, Vector3.UnitY, (float)WINDOW_SIZE_X/(float)WINDOW_SIZE_Y);

		}

		private void OnClose()
		{
			renderer.Close();
		}

		public void Start()
		{
			window.Run();
			
			window.Dispose();
		}

		private void OnWindowResize(Vector2D<int> size)
		{
			renderer.Resize(size);
		}

		private void OnLoad()
		{
			IInputContext input = window.CreateInput();
			foreach (var keyboard in input.Keyboards)
			{
				keyboard.KeyDown += KeyDown;
			}

			renderer.Load(window);
		}

		private void OnRender(double deltaTime)
		{
			renderer.Update(deltaTime, camera.GetView(), camera.GetProjection());
		}

		private void OnUpdate(double deltaTime)
		{
		}

		private void KeyDown(IKeyboard keyboard, Key key, int arg3)
		{
			if (key == Key.Escape)
			{
				window.Close();
			}
		}

		public static Application GetApplication() => application ??= new Application();
	}
}