using System.Numerics;

namespace Engine;

[Inspectable]
[Serializable]
public class Transform : ITransform
{
	[Inspectable(false)]
	[Serializable(false)]
	public GameObject? GameObject { get; private set; }

	private bool isDirty = true;
	private Vector3 position = new(0f);

	[Inspectable(false)]
	[Serializable(false)]
	public HashSet<ITransform> children { get; } = new();

	[Serializable(true)]
	public IEnumerable<Guid> ChildrenGuids => children.Select(x => x.GUID);

	private ITransform? parent = null;

	[Inspectable(false)]
	public Vector3 Forward => Vector3.Transform(-Vector3.UnitZ, Rotation);

	[Inspectable(false)]
	public Vector3 Back => Vector3.Transform(Vector3.UnitZ, Rotation);

	[Inspectable(false)]
	public Vector3 Up => Vector3.Transform(Vector3.UnitY, Rotation);

	[Inspectable(false)]
	public Vector3 Down => Vector3.Transform(-Vector3.UnitY, Rotation);

	[Inspectable(false)]
	public Vector3 Left => Vector3.Transform(Vector3.UnitX, Rotation);

	[Inspectable(false)]
	public Vector3 Right => Vector3.Transform(-Vector3.UnitX, Rotation);

	[Inspectable(false)]
	public bool HasChildren => children.Any();

	public IReadOnlyList<ITransform> GetChildren => children.ToList();
	public Guid GUID { get; set; } = System.Guid.NewGuid();

	public IReadOnlyList<ITransform> ChildrenRecursive => children
		.SelectMany(child => child.ChildrenRecursive)
		.Concat(children).ToList();

	public IEnumerable<GameObject> ChildrenAsGameObjectsRecursive => children
		.SelectMany(child => child.ChildrenAsGameObjectsRecursive)
		.Concat(children.Select(child => child.GameObject))
		.WhereNotNull()
		.Distinct()
		.ToList() ?? Enumerable.Empty<GameObject>();

	public IEnumerable<GameObject> ChildrenAsGameObjects => children
		.Select(x => x.GameObject)
		.WhereNotNull();

	public Transform(GameObject? go)
	{
		this.GameObject = go;
	}

	public void SetParent(ITransform? newParent)
	{
		parent?.children.Remove(this);

		parent = newParent;

		if (newParent != null)
		{
			newParent.children.Add(this);
		}
	}

	public Vector3 Position
	{
		get => position;
		set
		{
			position = value;
			isDirty = true;
		}
	}

	private Vector3 scale = new Vector3(1f);

	public Vector3 Scale
	{
		get => scale;
		set
		{
			scale = value;
			isDirty = true;
		}
	}

	private Quaternion rotation = Quaternion.Identity;

	public Quaternion Rotation
	{
		get => rotation;
		set
		{
			rotation = value;
			isDirty = true;
		}
	}

	public void RotateByEuler(Vector3 euler)
	{
		Quaternion deltaRotation = Quaternion.CreateFromYawPitchRoll(euler.Y, euler.X, euler.Z);

		Rotation = Quaternion.Normalize(Rotation * deltaRotation);
	}

	private Matrix4x4 modelMatrix;

	[Inspectable(false)]
	public Matrix4x4 ModelMatrix
	{
		get
		{
			if (isDirty)
			{
				modelMatrix = Matrix4x4.Identity * Matrix4x4.CreateFromQuaternion(Rotation) *
				              Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateTranslation(Position);
				isDirty = false;
			}

			return modelMatrix;
		}
	}

	public void Rotate(float xOffset, float yOffset)
	{
		Quaternion yaw = Quaternion.CreateFromAxisAngle(Vector3.UnitY, xOffset);
		Quaternion pitch = Quaternion.CreateFromAxisAngle(Vector3.UnitX, yOffset);

		Rotation = Quaternion.Normalize(yaw * Rotation * pitch);
	}

	public void Translate(Vector3 translation)
	{
		position += translation;
	}
}