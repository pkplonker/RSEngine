namespace Engine;

public abstract class Component : IComponent
{
	[Serializable(false)]
	[Inspectable(false)]
	public GameObject GameObject { get; set; }
	public Component(GameObject gameObject)
	{
		this.GameObject = gameObject;
	}

	public virtual void Update() { }
}