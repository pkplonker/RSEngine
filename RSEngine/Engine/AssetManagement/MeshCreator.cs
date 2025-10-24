using System.Numerics;
using Silk.NET.OpenGL;

namespace Engine.AssetManagement;

public class MeshCreator
{
    public static object Create(GL gl, float[] vertexArray, uint[] indexArray,Guid guid)
    {
        return new Mesh(gl, vertexArray, indexArray,guid);
    }
}