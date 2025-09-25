namespace Editor;

public class Memento
{
	private readonly Action perform = () => { };
	private readonly Action undo = () => { };
	public string name { get; private set; }

	public Memento(Action perform, Action undo, string name = "")
	{
		this.perform = perform;
		this.undo = undo;
		this.name = name;
	}

	public void Perform() => perform.Invoke();

	public void Undo() => undo.Invoke();
}