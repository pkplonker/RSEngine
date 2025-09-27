using System.Numerics;
using Engine;
using Silk.NET.Core;
using Silk.NET.Maths;

namespace GameCore
{
    public class GameApplication : BaseApplication
    {
        private const string WINDOW_NAME = "Luna Game - Stuart Heath - WIP";
        private static GameApplication application;

        protected override string WindowName => WINDOW_NAME;

        public GameApplication() : base()
        {
        }

        protected override void OnApplicationLoaded()
        {
            SceneController.ActiveScene = new Scene();

#if DEBUG
            ProjectManager.LoadTestProject();
#endif
            renderer.AddScene(SceneController.ActiveScene, new Vector2D<uint>(0, 0), out _, false);
        }

        public static GameApplication GetApplication() => application ??= new GameApplication();
    }
}