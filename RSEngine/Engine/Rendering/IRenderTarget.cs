using Silk.NET.Maths;
using Silk.NET.OpenGL;

namespace Engine;

/// Defines a render target that can be bound and resized
public interface IRenderTarget
{
    Vector2D<int> ViewportSize { get; set; }
    void Bind(GL gl);
    void ResizeViewport(GL gl, uint sizeX, uint sizeY);
    void ResizeWindow(GL gl, uint sizeX, uint sizeY);
}