using Engine;

namespace GameCore;

class Entry
{
	[STAThread]
	private static void Main()
	{
		var application = new GameApplication();
		application.Start();
	}
}

