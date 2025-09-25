using System.Numerics;

namespace Engine;

[ComponentName("Perspective Camera")]
public class PerspectiveCamera : Camera
{
	public float FieldOfView { get; set; } = MathExtensions.DegreesToRadians(60f);
	public PerspectiveCamera(GameObject go) : base(go) { }

	protected override Matrix4x4 CalculateProjectionMatrix() =>
		Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearPlaneDistance, FarPlaneDistance);
}