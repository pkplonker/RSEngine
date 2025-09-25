using System.Numerics;

namespace Engine;

public struct RenderPassData
{
	public readonly Matrix4x4 View;
	public readonly Matrix4x4 Projection;

	public RenderPassData(Matrix4x4 view, Matrix4x4 projection)
	{
		View = view;
		Projection = projection;
	}
}