using System.Numerics;

namespace Engine;

public interface IShader : IResource, IInspectable
{
	string ShaderPath { get; }
	void Use();
	void SetUniform(string name, int value);
	unsafe void SetUniform(string name, Matrix4x4 value);
	void SetUniform(string name, float value);
	void SetUniform(string name, bool value);
}

