using System.Numerics;

namespace Engine;

public interface ICamera
{
	public float AspectRatio { get; set; }

	Matrix4x4 GetView();
	Matrix4x4 GetProjection();
	public bool Main { get; set; }
	public static Matrix4x4 CreatePerspectiveFieldOfViewGL(float fov, float aspect, float zNear, float zFar)
	{
		float yScale = 1.0f / MathF.Tan(fov / 2.0f);
		float xScale = yScale / aspect;

		return new Matrix4x4(
			xScale, 0, 0, 0,
			0, yScale, 0, 0,
			0, 0, -(zFar + zNear) / (zFar - zNear), -1,
			0, 0, -(2 * zFar * zNear) / (zFar - zNear), 0
		);
	}
}