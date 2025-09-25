using System.Numerics;
using Engine.Logging;
using Silk.NET.OpenGL;

namespace Engine;

[ComponentName("Mesh Renderer")]
public class MeshRenderer : Component, IRenderableComponent
{
	[ResourceGuid(typeof(Material))]
	public Guid MaterialGuid { get; set; }

	public MeshRenderer(GameObject gameObject) : base(gameObject)
	{
		this.GameObject = gameObject;
	}

	public void Render(Renderer renderer, RenderPassData data)
	{
		ResourceManager.Instance.TryGetResourceByGuid<Material>(MaterialGuid, out var material);

		renderer.UseMaterial(material, data, GameObject.Transform.ModelMatrix);

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

	public void Update() { }

	public void Clone(MeshRenderer? dmr)
	{
		dmr.MaterialGuid = MaterialGuid;
	}
}