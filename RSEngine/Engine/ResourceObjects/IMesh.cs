namespace Engine;

public interface IMesh : IResource, IInspectable
{
	unsafe void SetupMesh();
	void Render(IRenderer renderer, RenderPassData data);
}