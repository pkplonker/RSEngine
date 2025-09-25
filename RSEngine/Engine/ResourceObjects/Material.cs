using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine;

[Inspectable]
[Serializable]
public class Material : IMaterial
{
	public enum TextureType
	{
		Albedo,
		Normal,
		Metallic,
		Roughness,
		AO,
	}

	[Inspectable(false)]
	[Serializable(true)]
	public Guid GUID { get; private set; } = Guid.NewGuid();

	[Inspectable]
	[ResourceGuid(typeof(Shader))]
	public Guid ShaderGUID { get; set; }
	
	[Inspectable]
	[ResourceGuid(typeof(Texture))]
	public Guid AlbedoGUID { get; set; }
	
	[Inspectable]
	public Vector4 Color { get; set; } =  new Vector4(1.0f, 0.0f, 0.0f, 1.0f);

	public Material(Guid shaderGuid)
	{
		this.ShaderGUID = shaderGuid;
	}

	public Material() { }

	public void SetShader(Guid shader)
	{
		this.ShaderGUID = shader;
	}

	public void Use(Renderer renderer, RenderPassData data, Matrix4x4 modelMatrix)
	{
		if (renderer == null) return;
		if (!ResourceManager.Instance.TryGetResourceByGuid<Shader>(ShaderGUID, out var shader))
		{
			return;
		}

		if (shader == null) return;
		renderer.UseShader(shader);
		if (AlbedoGUID != Guid.Empty)
		{
			BindTexture(AlbedoGUID, TextureType.Albedo, shader);	
		}

		shader.SetUniform("uView", data.View);
		shader.SetUniform("uProjection", data.Projection);
		shader.SetUniform("uModel", modelMatrix);
		shader.SetUniform("color", Color);
	}

	private void BindTexture(Guid textureGuid, TextureType textureType, Shader shader)
	{
		ResourceManager.Instance.TryGetResourceByGuid<Texture>(textureGuid, out var texture);
		int textureUnit = -1;
		if (texture != null)
		{
			textureUnit = (int) textureType;
			texture?.Bind(TextureUnit.Texture0 + textureUnit);
		}

		shader.SetUniform($"u{Enum.GetName(textureType)}", textureUnit);
	}
}