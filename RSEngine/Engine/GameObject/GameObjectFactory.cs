using Engine.Logging;

namespace Engine;

public enum PrimitiveType
{
	Empty,
	EmptyMesh,
	Cube,
	Sphere,
	Plane
}

public static class GameObjectFactory
{
	private static readonly char[] separator = {'(', ')'};
	private const string SPHERE_NAME = "New Sphere";
	private const string CUBE_NAME = "New Cube";
	private const string PLANE_NAME = "New Plane";

	public static GameObject CreateCamera(IScene activeScene, bool isPerspective = true)
	{
		var go = CreatePrimitive(activeScene);
		if (isPerspective)
			go.AddComponent<PerspectiveCamera>();
		else go.AddComponent<OrthographicCamera>();
		go.Transform.SetParent(activeScene);
		return go;
	}

	public static GameObject? CreatePrimitive(IScene activeScene, PrimitiveType type = PrimitiveType.Empty)
	{
		var go = new GameObject();

		try
		{
			if (type != PrimitiveType.Empty)
			{
				switch (type)
				{
					case PrimitiveType.Cube:
						CreateMesh(go,
							ResourceManager.Instance.GetResourceByName(ResourceManager.DEFAULT_MATERIAL) ?? null,
							ResourceManager.Instance.GetResourceByName(ResourceManager.DEFAULT_CUBE) ?? null,
							ResourceManager.Instance.GetResourceByName(ResourceManager.DEFAULT_SHADER) ?? null);
						go.Name = CUBE_NAME;
						break;
					case PrimitiveType.Sphere:
						CreateMesh(go,
							ResourceManager.Instance.GetResourceByName(ResourceManager.DEFAULT_MATERIAL) ?? null,
							ResourceManager.Instance.GetResourceByName(ResourceManager.DEFAULT_SPHERE) ?? null,
							ResourceManager.Instance.GetResourceByName(ResourceManager.DEFAULT_SHADER) ?? null);

						go.Name = SPHERE_NAME;
						break;
					case PrimitiveType.Plane:
						CreateMesh(go,
							ResourceManager.Instance.GetResourceByName(ResourceManager.DEFAULT_MATERIAL) ?? null,
							ResourceManager.Instance.GetResourceByName(ResourceManager.DEFAULT_PLANE) ?? null,
							ResourceManager.Instance.GetResourceByName(ResourceManager.DEFAULT_SHADER) ?? null);

						go.Name = PLANE_NAME;
						break;
				}
			}
		}
		catch (Exception e)
		{
			Logger.Error($"Failed to create primitive mesh {e}");
			return null;
		}

		go.Transform.SetParent(activeScene);

		SetName(activeScene, go);
		return go;
	}

	private static void SetName(IScene activeScene, GameObject go)
	{
		var defaultName = go.Name;

		var maxNumber = 0;
		foreach (var parts in activeScene.ChildrenAsGameObjectsRecursive
			         .Where(x => x.Name.StartsWith(defaultName))
			         .Select(x => x.Name)
			         .Select(name => name.Split(separator, StringSplitOptions.RemoveEmptyEntries)))
		{
			if (parts.Length == 2 && int.TryParse(parts[1], out var number))
			{
				maxNumber = Math.Max(maxNumber, number);
			}
		}

		go.Name = $"{defaultName} ({maxNumber + 1})";
	}

	private static void CreateMesh(GameObject go, IMetadata? materialMetadata, IMetadata? meshMetaData,
		IMetadata? shaderMetadata)
	{
		if (materialMetadata == null || meshMetaData == null || shaderMetadata == null)
		{
			throw new ArgumentException("Invalid guid loading primitive mesh");
		}

		var mf = go.AddComponent<MeshFilter>();
		var mr = go.AddComponent<MeshRenderer>();

		if (ResourceManager.Instance.TryGetResourceByGuid(materialMetadata.GUID,
			    out Material? material))
		{
			if (material != null)
			{
				mr.MaterialGuid = material.GUID;
				material.ShaderGUID = shaderMetadata.GUID;
				ObjectSerializer.Serialize(material, materialMetadata.Path.MakeProjectAbsolute());
			}
		}
		else
		{
			throw new ArgumentException("Shader guid invalid");
		}

		if (ResourceManager.Instance.TryGetResourceByGuid(meshMetaData.GUID,
			    out Mesh? mesh))
		{
			if (mesh != null)
			{
				mf.AddMesh(mesh.GUID);
			}
			else
			{
				throw new ArgumentException("Mesh GUID invalid");
			}
		}
	}

	public static GameObject CreateMesh(IScene activeScene) =>
		CreatePrimitive(activeScene, PrimitiveType.EmptyMesh);
}