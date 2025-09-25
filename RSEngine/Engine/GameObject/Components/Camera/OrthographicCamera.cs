using System.Numerics;

namespace Engine;

[ComponentName("Orthographic Camera")]
public class OrthographicCamera : Camera
{
	public float Width { get; set; } = 10f;
	public float Height => Width / AspectRatio;
	
	public OrthographicCamera(GameObject go) : base(go) { }

	protected override Matrix4x4 CalculateProjectionMatrix()
	{
		float halfWidth = Width / 2f;
		float halfHeight = Height / 2f;
		return Matrix4x4.CreateOrthographicOffCenter(-halfWidth, halfWidth, -halfHeight, halfHeight, NearPlaneDistance,
			FarPlaneDistance);
	}
}