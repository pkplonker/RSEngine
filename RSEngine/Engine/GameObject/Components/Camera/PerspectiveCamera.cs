using System.Numerics;

namespace Engine;

[ComponentName("Perspective Camera")]
public class PerspectiveCamera : Camera
{
    [Range(ReadOnly = true)]
    public float FieldOfView { get; set; } = MathExtensions.DegreesToRadians(45f);

    [Serializable(false)]
    [Range(1,180, Tooltip = "FOV in Degrees")]
    public float FieldOfViewDegrees
    {
        get => MathExtensions.RadiansToDegrees(FieldOfView);
        set => FieldOfView = MathExtensions.DegreesToRadians(value);
    }

    public PerspectiveCamera(GameObject go) : base(go)
    {
    }

    protected override Matrix4x4 CalculateProjectionMatrix() =>
        ICamera.CreatePerspectiveFieldOfViewGL(FieldOfView, AspectRatio, NearPlaneDistance, FarPlaneDistance);
}