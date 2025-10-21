using System.Numerics;
using Engine.Logging;
using Silk.NET.OpenGL;

namespace Engine;

[ComponentName("Mesh Renderer")]
public class MeshRenderer : Component, IRenderableComponent
{
	[ResourceGuid(typeof(Material))]
	public Guid MaterialGuid { get; set; }
	[Inspectable(false)]
	public Matrix4x4 ModelMatrix => GameObject.Transform.ModelMatrix;
	
	[Inspectable(false)]
	public RenderID24 RenderID { get; }

	public MeshRenderer(GameObject gameObject) : base(gameObject)
	{
		this.GameObject = gameObject;
		RenderID = ++IRenderable.CurrentID;
	}

	public void Render(IRenderer renderer, RenderPassData data, CustomShaderArgs customShaderArgs = null)
	{
		if (customShaderArgs != null)
		{
			renderer.UseShader(customShaderArgs.Shader);
			customShaderArgs.Shader.SetUniform("uView", data.View);
			customShaderArgs.Shader.SetUniform("uProjection", data.Projection);
			customShaderArgs.Shader.SetUniform("uModel", ModelMatrix);
        
			customShaderArgs.SetupCustomUniforms?.Invoke();
		}
		else
		{
			ResourceManager.Instance.TryGetResourceByGuid<Material>(MaterialGuid, out var material);
			renderer.UseMaterial(material, data, ModelMatrix);
		}

		var mf = GameObject.GetComponent<MeshFilter>();
		if (mf != null)
		{
			foreach (var guid in mf.meshes)
			{
				if (ResourceManager.Instance.TryGetResourceByGuid<Mesh>(guid, out var mesh))
				{
					mesh?.Render(renderer, data);
				}
			}
		}
	}


	public override void Update() { }

	public void Clone(MeshRenderer? dmr)=> dmr.MaterialGuid = MaterialGuid;
	
}