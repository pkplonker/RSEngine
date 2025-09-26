using System.Numerics;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Engine;

public interface IRenderer : IRenderStats
{
	GL Gl { get; }

	void AddScene(IScene? scene, Vector2D<uint> size, out IRenderTarget? renderTarget, bool toFrameBuffer);
	void RenderUpdate();
	void Resize(Vector2D<int> size);
	void Load(IWindow window);
	void SetRenderTargetSize(IScene scene, Vector2D<float> size);
	void Close();
	IRenderTarget? GetSceneRenderTarget(IScene? scene, RenderTargetType type = RenderTargetType.Main);
	void RemoveScene(IScene? oldScene);
	public Vector2D<int> WindowSize { get; set; }
	public RenderPassRegistry RenderPassRegistry { get; }
	public SceneOverlayRegistry OverlayRegistry { get; }

	void UseShader(IShader shader);
	void UseMaterial(IMaterial material, RenderPassData data, Matrix4x4 transformModelMatrix);
	void DrawElements(Silk.NET.OpenGL.PrimitiveType triangles, uint indicesLength, DrawElementsType unsignedInt);
	void EnsureRenderTarget(IScene scene, RenderTargetType selectedRenderTargetType);
}