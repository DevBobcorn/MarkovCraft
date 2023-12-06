#nullable enable
using System.IO;
using System.Threading;
using UnityEngine;

using CraftSharp;

namespace MarkovCraft
{
    public class MarkovGlobal : MonoBehaviour
    {
        public const int WINDOWED_APP_WIDTH = 1600, WINDOWED_APP_HEIGHT = 900;
        private static readonly char SP = Path.DirectorySeparatorChar;

        private static Thread? unityThread;
        public static Thread UnityThread => unityThread!;

        // Runs before a scene gets loaded
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeApp()
        {
            Loom.Initialize();

            unityThread = Thread.CurrentThread;

            var global = new GameObject("Markov Global");
            global.AddComponent<MarkovGlobal>();
            DontDestroyOnLoad(global);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F11)) // Toggle full screen
            {
                if (Screen.fullScreen)
                {
                    Screen.SetResolution(WINDOWED_APP_WIDTH, WINDOWED_APP_HEIGHT, false);
                    Screen.fullScreen = false;
                }
                else
                {
                    var maxRes = Screen.resolutions[Screen.resolutions.Length - 1];
                    Screen.SetResolution(maxRes.width, maxRes.height, true);
                    Screen.fullScreen = true;
                }
            }
        }

        public static string GetDataDirectory()
        {
            return PathHelper.GetExtraDataDirectory();
        }

        public static string GetDataFile(string fileName)
        {
            return PathHelper.GetExtraDataFile(fileName);
        }

        public static string GetRecordingFile(string fileName)
        {
            return PathHelper.GetRootDirectory() + $"{SP}Recordings{SP}{fileName}";
        }

        public static string GetDefaultExportPath()
        {
            return PathHelper.GetRootDirectory() + $"{SP}Exported";
        }
    }
}