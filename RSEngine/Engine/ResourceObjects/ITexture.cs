using Silk.NET.OpenGL;

namespace Engine;

public interface ITexture : IResource, IInspectable
{
	uint Width { get; }
	uint Height { get; }
	string Path { get; set; }
	void Bind(TextureUnit textureSlot = TextureUnit.Texture0);
	void Dispose();
}