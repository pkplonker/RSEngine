using System.Numerics;

namespace Engine;

public abstract class Camera : ICamera, IComponent
{
	public Camera(GameObject go)
	{
		this.GameObject = go;
	}
	public GameObject GameObject { get; set; }
	private float aspectRatio = 1.6f;
	private bool isDirty = true;

	public float AspectRatio
	{
		get => aspectRatio;
		set
		{
			if (value != aspectRatio)
			{
				aspectRatio = value;
				isDirty = true;
			}
		}
	}

	private Matrix4x4 projectionMatrix;
	private float nearPlaneDistance = 0.1f;
	private float farPlaneDistance = 200f;
	private Matrix4x4 viewMatrix;

	public Matrix4x4 GetView() => viewMatrix;

	public Matrix4x4 GetProjection()
	{
		if (isDirty)
		{
			RecalculateMatrices();
			isDirty = false;
		}

		return projectionMatrix;
	}

	protected void RecalculateMatrices()
	{
		projectionMatrix = CalculateProjectionMatrix();
		viewMatrix = CalculateViewMatrix();
	}

	protected virtual Matrix4x4 CalculateViewMatrix()
	{
		var position = GameObject.Transform.Position;
		var front = GameObject.Transform.Forward;
		var up = GameObject.Transform.Up;
		return Matrix4x4.CreateLookAt(position, position + front, up);
	}

	protected abstract Matrix4x4 CalculateProjectionMatrix();

	public float NearPlaneDistance
	{
		get => nearPlaneDistance;
		set
		{
			if (value != nearPlaneDistance)
			{
				nearPlaneDistance = value;
				isDirty = true;
			}
		}
	}

	public float FarPlaneDistance
	{
		get => farPlaneDistance;
		set
		{
			if (value != farPlaneDistance)
			{
				farPlaneDistance = value;
				isDirty = true;
			}
		}
	}

	public virtual void Update() { }
}