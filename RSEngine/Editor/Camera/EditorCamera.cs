using System.Numerics;

namespace Engine;

public class EditorCamera : IEditorCamera
{
    public ITransform Transform { get; set; }

    protected float zoom = 45f;
    protected readonly Vector3 startPosition;
    protected readonly float startAspectRatio;
    public float AspectRatio { get; set; }

    public EditorCamera(Vector3 position, float aspectRatio, Vector3 eulerRotation = default)
    {
        startPosition = position;
        startAspectRatio = aspectRatio;
        startRotation = eulerRotation;
        Reset();
    }

    private Vector3 startRotation;

    public void Reset()
    {
        Transform = new Transform(null)
        {
            Position = startPosition,
            Rotation = Quaternion.CreateFromYawPitchRoll(
                MathExtensions.DegreesToRadians(startRotation.Y),
                MathExtensions.DegreesToRadians(startRotation.X),
                MathExtensions.DegreesToRadians(startRotation.Z)
            )
        };
        AspectRatio = startAspectRatio;
    }

    public void ModifyZoom(float zoomAmount)
    {
        zoom = Math.Clamp(zoom - zoomAmount, 1.0f, 45f);
    }

    public void ModifyDirection(float xOffset, float yOffset)
    {
        Transform.Rotate(xOffset, yOffset);
    }

    public Matrix4x4 GetView()
    {
        Vector3 position = Transform.Position;
        Vector3 front = Transform.Forward;
        Vector3 up = Transform.Up;
        return Matrix4x4.CreateLookAt(position, position + front, up);
    }

    public Matrix4x4 GetProjection() =>
        ICamera.CreatePerspectiveFieldOfViewGL(MathExtensions.DegreesToRadians(zoom), AspectRatio, 0.1f, 100.0f);

    public bool Main { get; set; } = false;



    public virtual void SetActive(bool active, IInputController input)
    {
    }
}