using System.Numerics;

namespace Engine;

public interface ITransform
{
	GameObject? GameObject { get; }
	Vector3 Position { get; set; }
	IEnumerable<Guid> ChildrenGuids { get; }
	Vector3 Forward { get; }
	Vector3 Back { get; }
	Vector3 Up { get; }
	Vector3 Down { get; }
	Vector3 Left { get; }
	Vector3 Right { get; }
	bool HasChildren { get; }
	IReadOnlyList<ITransform> GetChildren { get; }
	Guid GUID { get; set; }
	IReadOnlyList<ITransform> ChildrenRecursive { get; }
	IEnumerable<GameObject> ChildrenAsGameObjectsRecursive { get; }
	IEnumerable<GameObject> ChildrenAsGameObjects { get; }
	Vector3 Scale { get; set; }
	Quaternion Rotation { get; set; }
	Matrix4x4 ModelMatrix { get; }
	public HashSet<ITransform> children { get; } 
	void SetParent(ITransform? newParent);
	void RotateByEuler(Vector3 euler);
	void Rotate(float xOffset, float yOffset);
	void Translate(Vector3 translation);
}