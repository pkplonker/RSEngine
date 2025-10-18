using System.Numerics;

namespace Engine;

public interface ITransform : ITransformNode
{
	Vector3 Position { get; set; }
	IEnumerable<Guid> ChildrenGuids { get; }
	Vector3 Forward { get; }
	Vector3 Back { get; }
	Vector3 Up { get; }
	Vector3 Down { get; }
	Vector3 Left { get; }
	Vector3 Right { get; }
	
	Vector3 Scale { get; set; }
	Quaternion Rotation { get; set; }
	Matrix4x4 ModelMatrix { get; }
	
	void RotateByEuler(Vector3 euler);
	void Rotate(float xOffset, float yOffset);
	void Translate(Vector3 translation);
	
	
	
}