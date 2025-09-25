using System.Numerics;
using Engine;
using ImGuiNET;
using Silk.NET.Maths;

namespace Editor.Controls;

public class ObjectPreviewPanel : IPanel
{
	private Scene scene;
	private GameObject? ms;
	private GameObject? MaterialSphere => ms ??= GameObjectFactory.CreatePrimitive(scene, PrimitiveType.Sphere);

	private Vector2 previousSize;
	private readonly IInputController inputController;
	private readonly EditorViewport editorViewport;
	private Vector2 currentSize;
	private bool subscribed = false;
	private GameObject? activeGameObject = null;
	private const string settingsCategory = "Object Preview";

	public ObjectPreviewPanel(PropertiesPanel properties, IInputController inputController, IRenderer renderer)
	{
		this.inputController = inputController;
		properties.SelectionChanged += InspectorOnSelectionChanged;
		scene = new Scene("Object Preview Scene");
		editorViewport = new EditorViewport();
		editorViewport.IsActive += OnIsActive;
		scene.ActiveCamera = new EditorCamera(Vector3.UnitZ * 6, 1024 / (float) 1024);
		renderer.AddScene(scene, new Vector2D<uint>((uint) 0, (uint) 0), out var rt, true);
	}

	private void OnIsActive(bool isActive)
	{
		if (isActive && !subscribed)
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

	private bool HandleMouseMove(Vector2 arg)
	{
		if (inputController.IsMouseHeld(IInputController.MouseButton.Right))
		{
			float mouseSensitivityX = EditorSettings.GetSetting("Viewport Mouse Sensitivity X", settingsCategory, true, 0.3f);
			float mouseSensitivityY = EditorSettings.GetSetting("Viewport Mouse Sensitivity Y", settingsCategory, true, 0.5f);
			var mouseDelta = arg;
			activeGameObject?.Transform.Rotate(-mouseDelta.X * mouseSensitivityX * Time.DeltaTime,
				mouseDelta.Y * mouseSensitivityY * Time.DeltaTime);
			return true;
		}

		return false;
	}

	private bool HandleKeyPress(IInputController.Key arg1, IInputController.InputState arg2)
	{
		return false;
	}

	private void InspectorOnSelectionChanged(IInspectable? obj)
	{
		scene.Clear();
		activeGameObject = null;
		switch (obj)
		{
			case GameObject gameObject:
				var dummyGo = new GameObject();
				dummyGo.Transform.SetParent(scene);
				var mf = gameObject.GetComponent<MeshFilter>();
				var mr = gameObject.GetComponent<MeshRenderer>();
				var dmf = dummyGo.AddComponent<MeshFilter>();
				var dmr = dummyGo.AddComponent<MeshRenderer>();
				if (mf != null)
				{
					mf.Clone(dmf);
				}

				if (mr != null)
				{
					mr.Clone(dmr);
				}

				activeGameObject = dummyGo;
				break;
			case Material material:
				MaterialSphere.Transform.SetParent(scene);
				MaterialSphere.GetComponent<MeshRenderer>().MaterialGuid = material.GUID;
				activeGameObject = MaterialSphere;
				break;
			default:
				// Handle default case
				break;
		}
	}

	public string PanelName { get; set; } = "Preview";

	public void Draw(IRenderer renderer)
	{
		editorViewport.Update(PanelName, scene.ActiveCamera as IEditorCamera, scene, inputController, renderer,
			ref currentSize);
	}
}