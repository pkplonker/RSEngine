namespace Engine;

public interface IComponent
{
	[Serializable(false)]
	public GameObject GameObject { get; set; }

	public void Update();
}