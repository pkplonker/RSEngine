using Silk.NET.OpenGL;
using System;

namespace Engine
{
	public class VertexArrayObject<TVertexType, TIndexType> : IDisposable
		where TVertexType : unmanaged
		where TIndexType : unmanaged
	{
		private uint handle;
		private GL gl;

		public VertexArrayObject(GL gl, BufferObject<TVertexType> vbo, BufferObject<TIndexType> ebo)
		{
			this.gl = gl;

			handle = this.gl.GenVertexArray();
			Bind();
			vbo.Bind();
			ebo.Bind();
		}

		public unsafe void VertexAttributePointer(uint index, int count, VertexAttribPointerType type, uint vertexSize,
			int offSet)
		{
			gl.VertexAttribPointer(index, count, type, false, vertexSize * (uint) sizeof(TVertexType),
				(void*) (offSet * sizeof(TVertexType)));
			gl.EnableVertexAttribArray(index);
		}

		public void Bind()
		{
			gl.BindVertexArray(handle);
		}

		public void Dispose()
		{
			gl.DeleteVertexArray(handle);
		}

		public void UnBind()
		{
			gl.BindVertexArray(0);
		}
	}
}