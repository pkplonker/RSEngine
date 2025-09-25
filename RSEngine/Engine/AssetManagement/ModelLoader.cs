using Silk.NET.Assimp;
using Silk.NET.OpenGL;
using System;
using System.Numerics;
using Engine.Logging;
using File = Silk.NET.Assimp.File;

namespace Engine;

public class ModelLoader
{
	public static unsafe Mesh? LoadModel(GL gl, string path, Guid metadataGuid)
	{
		var assimp = Silk.NET.Assimp.Assimp.GetApi();
		if (!System.IO.File.Exists(path))
		{
			Logger.Warning($"Failed to find file {path}");
			return null;
		}

		var scene = assimp.ImportFile(path, (uint) PostProcessSteps.Triangulate);

		if (scene == null || scene->MFlags == Silk.NET.Assimp.Assimp.SceneFlagsIncomplete || scene->MRootNode == null)
		{
			var error = assimp.GetErrorStringS();
			throw new Exception(error);
		}

		var mesh = scene->MMeshes[0];

		List<Vertex> vertices = new List<Vertex>();
		List<uint> indices = new List<uint>();

		for (uint i = 0; i < mesh->MNumVertices; i++)
		{
			Vertex vertex = new Vertex();

			vertex.Position = mesh->MVertices[i];

			if (mesh->MNormals != null)
				vertex.Normal = mesh->MNormals[i];
			if (mesh->MTangents != null)
				vertex.Tangent = mesh->MTangents[i];
			if (mesh->MBitangents != null)
				vertex.Bitangent = mesh->MBitangents[i];

			if (mesh->MTextureCoords[0] != null)
			{
				Vector3 texcoord3 = mesh->MTextureCoords[0][i];
				vertex.TexCoords = new Vector2(texcoord3.X, texcoord3.Y);
			}

			vertices.Add(vertex);
		}

		for (uint i = 0; i < mesh->MNumFaces; i++)
		{
			Face face = mesh->MFaces[i];
			for (uint j = 0; j < face.MNumIndices; j++)
				indices.Add(face.MIndices[j]);
		}

		return new Mesh(gl, BuildVertices(vertices), BuildIndices(indices), metadataGuid);
	}

	private static float[] BuildVertices(List<Vertex> vertexCollection)
	{
		var vertices = new List<float>();

		foreach (var vertex in vertexCollection)
		{
			vertices.Add(vertex.Position.X);
			vertices.Add(vertex.Position.Y);
			vertices.Add(vertex.Position.Z);
			vertices.Add(vertex.Normal.X);
			vertices.Add(vertex.Normal.Y);
			vertices.Add(vertex.Normal.Z);
			vertices.Add(vertex.Tangent.X);
			vertices.Add(vertex.Tangent.Y);
			vertices.Add(vertex.Tangent.Z);
			vertices.Add(vertex.TexCoords.X);
			vertices.Add(vertex.TexCoords.Y);
			vertices.Add(vertex.Bitangent.X);
			vertices.Add(vertex.Bitangent.Y);
			vertices.Add(vertex.Bitangent.Z);
		}

		return vertices.ToArray();
	}

	private static uint[] BuildIndices(List<uint> indices) => indices.ToArray();
}