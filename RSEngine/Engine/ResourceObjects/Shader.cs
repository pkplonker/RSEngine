using System.Diagnostics;
using System.Numerics;
using Engine.Logging;
using Silk.NET.OpenGL;

namespace Engine;

[Inspectable]
[Serializable]
public class Shader : IDisposable, IShader
{
	private uint handle;
	private GL gl;
	
	[Serializable(false)]
	private Dictionary<string, int> uniformDict = new();

	[Serializable(false)]
	public string ShaderPath { get; private set; }

	[Inspectable(false)]
	[Serializable(true)]
	[ResourceGuid(typeof(Shader))]
	public Guid GUID { get; private set; } = Guid.NewGuid();

	public Shader(GL gl, string shaderPath, Guid metadataGuid)
	{
		GUID = metadataGuid;
		this.ShaderPath = shaderPath;
		this.gl = gl;

		LoadShader(shaderPath, out uint vertex, out uint fragment);

		handle = this.gl.CreateProgram();
		this.gl.AttachShader(handle, vertex);
		this.gl.AttachShader(handle, fragment);
		this.gl.LinkProgram(handle);
		this.gl.GetProgram(handle, GLEnum.LinkStatus, out var status);
		if (status == 0)
		{
			throw new Exception($"Program failed to link with error: {this.gl.GetProgramInfoLog(handle)}");
		}

		this.gl.DetachShader(handle, vertex);
		this.gl.DetachShader(handle, fragment);
		this.gl.DeleteShader(vertex);
		this.gl.DeleteShader(fragment);
	}

	public void Use()
	{
		gl.UseProgram(handle);
	}

	private bool TryGetUniformLocation(string name, out int location)
	{
		if (!uniformDict.ContainsKey(name))
		{
			location = gl.GetUniformLocation(handle, name);
			if (location == -1)
			{
				Debug.WriteLine($"{name} uniform not found on shader.");
				return false;
			}

			uniformDict[name] = location;
		}

		location = uniformDict[name];
		return true;
	}

	public void SetUniform(string name, int value)
	{
		if (!TryGetUniformLocation(name, out var location))
		{
			Logger.Error($"{name} uniform not found on shader.");
		}

		gl.Uniform1(location, value);
	}

	public unsafe void SetUniform(string name, Matrix4x4 value)
	{
		if (!TryGetUniformLocation(name, out var location))
		{
			//Logger.Error($"{name} uniform not found on shader.");
		}

		gl.UniformMatrix4(location, 1, false, (float*) &value);
	}

	public void SetUniform(string name, float value)
	{
		if (!TryGetUniformLocation(name, out var location))
		{
			//Logger.Error($"{name} uniform not found on shader.");
		}

		gl.Uniform1(location, value);
	}

	public void SetUniform(string name, bool value)
	{
		if (!TryGetUniformLocation(name, out var location))
		{
			//Logger.Error($"{name} uniform not found on shader.");
		}

		gl.Uniform1(location, value ? 1 : 0);
	}
	public void SetUniform(string name, Vector4 value)
	{
		if (!TryGetUniformLocation(name, out var location))
		{
			//Logger.Error($"{name} uniform not found on shader.");
		}

		gl.Uniform4(location, value);
	}

	private uint LoadShader(ShaderType type, string src)
	{
		uint handle = gl.CreateShader(type);
		gl.ShaderSource(handle, src);
		gl.CompileShader(handle);
		string infoLog = gl.GetShaderInfoLog(handle);
		if (!string.IsNullOrWhiteSpace(infoLog))
		{
			throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
		}

		return handle;
	}

	private void LoadShader(string path, out uint vertex, out uint fragment)
	{
		if (!File.Exists(path))
		{
			throw new FileNotFoundException($"Shader file not found: {path}");
		}

		var src = File.ReadAllText(path);

		var shaderParts = src.Split(new string[] {"#####"}, StringSplitOptions.RemoveEmptyEntries);
		if (shaderParts.Length != 2)
		{
			throw new InvalidOperationException("Shader source must contain exactly two parts separated by '#####'");
		}

		var vertexSrc = shaderParts[0];
		var fragmentSrc = shaderParts[1];

		vertex = LoadShader(ShaderType.VertexShader, vertexSrc);
		fragment = LoadShader(ShaderType.FragmentShader, fragmentSrc);
	}

	public void Dispose()
	{
		gl.DeleteProgram(handle);
	}
}