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
	unsafe void DrawElements(Silk.NET.OpenGL.PrimitiveType primativeType, uint indicesLength, DrawElementsType elementsTyp);
	void UseShader(IShader? shader);
	void UseMaterial(IMaterial material, RenderPassData data, Matrix4x4 modelMatrix);
	IRenderTarget? GetSceneRenderTarget(IScene? scene);
	void RemoveScene(IScene? oldScene);
	public Vector2D<int> WindowSize { get; set; }
}