using System.Numerics;
using Engine.Logging;
using Silk.NET.GLFW;
using Silk.NET.Input;
using MouseButton = Silk.NET.Input.MouseButton;

namespace Engine;

public class InputController : IInputController
{
	private readonly IInputContext input;
	private Vector2 lastMousePosition;
	private Vector2 currentMousePosition;
	
	private HashSet<IInputController.Key> currentKeyPresses = new();
	private HashSet<IInputController.MouseButton> currentMousePresses = new();
	
	private Stack<Func<IInputController.Key, IInputController.InputState, bool>> keyHandlers = new();
	private Stack<Func<IInputController.MouseButton, IInputController.InputState, bool>> mouseButtonHandlers = new();
	private Stack<Func<Vector2, bool>> mouseMoveHandlers = new();
	public event Action<float, float> MouseScroll;
	public event Action<float, float> MouseMove;

	public InputController(IInputContext input)
	{
		this.input = input;

		foreach (var keyboard in input.Keyboards)
		{
			keyboard.KeyDown += KeyDown;
			keyboard.KeyUp += KeyUp;
		}

		foreach (var mouse in input.Mice)
		{
			mouse.MouseMove += OnMouseMove;
			mouse.Scroll += OnMouseWheel;
			mouse.MouseDown += MouseButtonDown;
			mouse.MouseUp += MouseButtonUp;
			lastMousePosition = new Vector2(mouse.Position.X, mouse.Position.Y);
		}
	}

	public void SubscribeToKeyEvent(Func<IInputController.Key, IInputController.InputState, bool> handler) =>
		keyHandlers.Push(handler);

	public void UnsubscribeToKeyEvent(Func<IInputController.Key, IInputController.InputState, bool> handler)
	{
		var handlers = keyHandlers.ToList();
		handlers.Remove(handler);
		keyHandlers = new Stack<Func<IInputController.Key, IInputController.InputState, bool>>(handlers);
	}

	public void SubscribeToMouseButtonEvent(
		Func<IInputController.MouseButton, IInputController.InputState, bool> handler) =>
		mouseButtonHandlers.Push(handler);

	public void UnsubscribeToMouseButtonEvent(
		Func<IInputController.MouseButton, IInputController.InputState, bool> handler)
	{
		var handlers = mouseButtonHandlers.ToList();
		handlers.Remove(handler);
		mouseButtonHandlers =
			new Stack<Func<IInputController.MouseButton, IInputController.InputState, bool>>(handlers);
	}

	public void SubscribeToMouseMoveEvent(Func<Vector2, bool> handler) =>
		mouseMoveHandlers.Push(handler);

	public void UnsubscribeToMouseMoveEvent(Func<Vector2, bool> handler)
	{
		var handlers = mouseMoveHandlers.ToList();
		handlers.Remove(handler);
		mouseMoveHandlers =
			new Stack<Func<Vector2, bool>>(handlers);
	}

	private void KeyDown(IKeyboard device, Silk.NET.Input.Key key, int arg3)
	{
		currentKeyPresses.Add((IInputController.Key) key);
		foreach (var handler in keyHandlers)
		{
			if (handler((IInputController.Key) key, IInputController.InputState.Pressed))
				return;
		}
	}

	private void KeyUp(IKeyboard device, Silk.NET.Input.Key key, int arg3)
	{
		currentKeyPresses.Remove((IInputController.Key) key);
		foreach (var handler in keyHandlers)
		{
			if (handler((IInputController.Key) key, IInputController.InputState.Released))
				return;
		}
	}

	private void MouseButtonDown(IMouse mouse, MouseButton button)
	{
		currentMousePresses.Add((IInputController.MouseButton) button);
		foreach (var handler in mouseButtonHandlers)
		{
			if (handler((IInputController.MouseButton) button, IInputController.InputState.Pressed))
				return;
		}
	}

	private void MouseButtonUp(IMouse mouse, MouseButton button)
	{
		currentMousePresses.Remove((IInputController.MouseButton) button);
		foreach (var handler in mouseButtonHandlers)
		{
			if (handler((IInputController.MouseButton) button, IInputController.InputState.Released))
				return;
		}
	}

	private void OnMouseWheel(IMouse device, ScrollWheel args)
	{
		MouseScroll?.Invoke(args.X, args.Y);
	}

	private void OnMouseMove(IMouse device, Vector2 args)
	{
		Vector2 newPosition = new Vector2(args.X, args.Y);
		Vector2 delta = newPosition - lastMousePosition;

		foreach (var handler in mouseMoveHandlers)
		{
			if (handler(delta))
				break;
		}

		currentMousePosition = newPosition;
		MouseMove?.Invoke(args.X, args.Y);
		lastMousePosition = currentMousePosition;
	}

	public void Update()
	{
		foreach (var key in currentKeyPresses)
		{
			foreach (var handler in keyHandlers)
			{
				if (handler(key, IInputController.InputState.Held))
					break;
			}
		}
	}

	public bool IsKeyHeld(IInputController.Key key) => currentKeyPresses.Contains(key);
	public bool IsKeyPressed(IInputController.Key key) => currentKeyPresses.Contains(key);

	public bool IsMousePressed(IInputController.MouseButton button) => currentMousePresses.Contains(button);
	public bool IsMouseHeld(IInputController.MouseButton button) => currentMousePresses.Contains(button);

	public Vector2 GetMousePosition() => currentMousePosition;
}