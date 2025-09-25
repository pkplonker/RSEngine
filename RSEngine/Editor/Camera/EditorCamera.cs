using System.Numerics;

namespace Engine;

public class EditorCamera : IEditorCamera
{
	public ITransform Transform { get; set; }

	protected float zoom = 45f;
	protected readonly Vector3 startPosition;
	protected readonly float startAspectRatio;
	public float AspectRatio { get; set; }

	public EditorCamera(Vector3 position, float aspectRatio)
	{
		startPosition = position;
		startAspectRatio = aspectRatio;
		Reset();
	}

	public void Reset()
	{
		Transform = new Transform(null)
		{
			Position = startPosition,
			Rotation = Quaternion.Identity
		};
		AspectRatio = startAspectRatio;
	}

	public void ModifyZoom(float zoomAmount)
	{
		zoom = Math.Clamp(zoom - zoomAmount, 1.0f, 45f);
	}

	public void ModifyDirection(float xOffset, float yOffset)
	{
		Transform.Rotate(xOffset, yOffset);
	}

	public Matrix4x4 GetView()
	{
		Vector3 position = Transform.Position;
		Vector3 front = Transform.Forward;
		Vector3 up = Transform.Up;
		return Matrix4x4.CreateLookAt(position, position + front, up);
	}

	public Matrix4x4 GetProjection() =>
		Matrix4x4.CreatePerspectiveFieldOfView(MathExtensions.DegreesToRadians(zoom), AspectRatio, 0.1f, 100.0f);

	public virtual void SetActive(bool active, IInputController input) { }
}