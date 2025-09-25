using Silk.NET.OpenGL;
using StbImageSharp;
using File = System.IO.File;

namespace Engine;

[Inspectable]
[Serializable]
public class Texture : IDisposable, ITexture
{
	private uint textureHandle;

	[Serializable(false)]
	[Inspectable(false)]
	public uint TextureId => textureHandle;

	private GL gl;
	public uint Width { get; private set; }
	public uint Height { get; private set; }

	public string Path { get; set; }

	[Serializable]
	[Inspectable(false)]
	public Guid GUID { get; set; } = Guid.NewGuid();

	public unsafe Texture(GL gl, string path, Guid guid)
	{
		this.gl = gl;
		Path = path;
		this.GUID = guid;
		textureHandle = this.gl.GenTexture();
		Bind();

		var img = ImageResult.FromMemory(File.ReadAllBytes(path), ColorComponents.RedGreenBlueAlpha);
		fixed (byte* ptr = img.Data)
		{
			Width = (uint) img.Width;
			Height = (uint) img.Height;
			gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint) img.Width,
				(uint) img.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
		}

		SetParameters();
	}

	private void SetParameters()
	{
		gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
		gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);
		gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter,
			(int) GLEnum.LinearMipmapLinear);
		gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Linear);
		gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
		gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8);
		gl.GenerateMipmap(TextureTarget.Texture2D);
	}

	public void Bind(TextureUnit textureSlot = TextureUnit.Texture0)
	{
		gl.ActiveTexture(textureSlot);
		gl.BindTexture(TextureTarget.Texture2D, textureHandle);
	}

	public void Dispose()
	{
		gl.DeleteTexture(textureHandle);
	}
}