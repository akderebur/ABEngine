using System;
using System.Collections.Generic;

namespace ABEngine.ABERuntime
{
	public static class SceneManager
	{
		private static int _sceneID;
		private static Dictionary<int, string> sceneMap;

		static SceneManager()
		{
			
		}

		public static int GetSceneID()
		{
			return _sceneID;
		}

		public static void LoadScene(int sceneID)
		{
			_sceneID = sceneID;
			Game.ReloadGame(true);
		}

        public static void LoadNextScene()
        {
			LoadScene(_sceneID + 1);
        }

        public static void LoadPreviousScene()
        {
            LoadScene(_sceneID - 1);
        }
    }
}

