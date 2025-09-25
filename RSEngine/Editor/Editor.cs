using Engine;
namespace Editor
{
	class Editor
	{
		[STAThread]
		private static void Main()
		{
			var application = EditorApplication.GetApplication();
			application.Start();
		}
	}
}