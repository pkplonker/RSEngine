namespace Engine;

public interface IMesh : IResource, IInspectable
{
	unsafe void SetupMesh();
	void Render(Renderer renderer, RenderPassData data);
}