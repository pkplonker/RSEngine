using Engine;

namespace Editor;

public static class UndoManager
{
	private static Stack<Memento> performedStack = new Stack<Memento>();
	private static Stack<Memento> redoStack = new Stack<Memento>();

	public static void RecordAndPerform(Memento memento)
	{
		memento.Perform();
		performedStack.Push(memento);
		redoStack.Clear();
	}

	public static void Undo()
	{
		if (performedStack.Count == 0)
			return;

		var memento = performedStack.Pop();
		memento.Undo();
		redoStack.Push(memento);
	}

	public static void Redo()
	{
		if (redoStack.Count == 0)
			return;

		var memento = redoStack.Pop();
		memento.Perform();
		performedStack.Push(memento);
	}

	public static IEnumerable<string> GetRedoActions() => new List<string>(redoStack.Select(x => x.name));
	public static IEnumerable<string> GetUndoActions() => new List<string>(performedStack.Select(x => x.name));

	public static void Clear()
	{
		redoStack.Clear();
		performedStack.Clear();
	}

	

}