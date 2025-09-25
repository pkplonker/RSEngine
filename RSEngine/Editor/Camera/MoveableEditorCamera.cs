using System.Numerics;
using Editor;
using Engine.Logging;

namespace Engine;

public class MoveableEditorCamera : EditorCamera
{
	private bool subscribed = false;
	private IInputController inputController;
	private float currentSpeed = EditorSettings.GetSetting("Key Speed", settingsCategory, true, 10f);
	private const string settingsCategory = "Editor Camera";
	public MoveableEditorCamera(Vector3 position, float aspectRatio) : base(position, aspectRatio) { }

	private bool HandleKeyPress(IInputController.Key key, IInputController.InputState inputState)
	{
		if (inputState == IInputController.InputState.Held || inputState == IInputController.InputState.Pressed)
		{
			if (key == IInputController.Key.W)
			{
				Transform.Position += Transform.Forward * currentSpeed * Time.DeltaTime;
				return true;
			}

			if (key == IInputController.Key.A)
			{
				Transform.Position += Transform.Right * currentSpeed * Time.DeltaTime;
				return true;
			}

			if (key == IInputController.Key.S)
			{
				Transform.Position += Transform.Back * currentSpeed * Time.DeltaTime;
				return true;
			}

			if (key == IInputController.Key.D)
			{
				Transform.Position += Transform.Left * currentSpeed * Time.DeltaTime;
				return true;
			}
		}

		if (key == IInputController.Key.ShiftLeft)
		{
			float speed = EditorSettings.GetSetting("Key Speed", settingsCategory, true, 10f);
			float speedMultiplier = EditorSettings.GetSetting("Speed Multiplier", settingsCategory, true, 5f);
			currentSpeed = inputState == IInputController.InputState.Held ? speed * speedMultiplier : speed;
			return true;
		}

		return false;
	}
	

	private bool HandleMouseMove(Vector2 arg)
	{
		if (inputController.IsMouseHeld(IInputController.MouseButton.Right))
		{
			float mouseSensitivityX = EditorSettings.GetSetting("Mouse Sensitivity X", settingsCategory, true, 0.3f);
			float mouseSensitivityY = EditorSettings.GetSetting("Mouse Sensitivity Y", settingsCategory, true, 0.5f);
			var mouseDelta = arg;
			Transform.Rotate(-mouseDelta.X * mouseSensitivityX * Time.DeltaTime,
				mouseDelta.Y * mouseSensitivityY * Time.DeltaTime);
			return true;
		}

		return false;
	}

	public override void SetActive(bool active, IInputController inputController)
	{
		this.inputController ??= inputController;
		if (active && !subscribed)
		{
			inputController.SubscribeToKeyEvent(HandleKeyPress);
			inputController.SubscribeToMouseMoveEvent(HandleMouseMove);
			subscribed = true;
		}
		else
		{
			subscribed = false;
			inputController.UnsubscribeToKeyEvent(HandleKeyPress);
			inputController.UnsubscribeToMouseMoveEvent(HandleMouseMove);
		}
	}
}