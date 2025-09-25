using System.Numerics;

namespace Engine;

public interface ICamera
{
	public float AspectRatio { get; set; }

	Matrix4x4 GetView();
	Matrix4x4 GetProjection();
}