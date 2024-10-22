#nullable enable
using System.IO;
using System.Threading;
using UnityEngine;

using CraftSharp;
using System.Linq;

namespace MarkovCraft
{
    public class MarkovGlobal : MonoBehaviour
    {
        public const int WINDOWED_APP_WIDTH = 1600, WINDOWED_APP_HEIGHT = 900;
        private static readonly char SP = Path.DirectorySeparatorChar;

        public const string MARKOV_CRAFT_BUILTIN_FILE_NAME = "MarkovCraftBuiltin";
        public const int    MARKOV_CRAFT_BUILTIN_VERSION = 2;
        public const string VANILLAFIX_FILE_NAME = "VanillaFix";
        public const int    VANILLAFIX_VERSION = 1;

        public const string SELECTED_RESPACKS_FILE_NAME = "selected_respacks.txt";
        public const string VANILLA_RESPACK_SYMBOL = "$VANILLA";

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

        public static string[] LoadSelectedResPacks()
        {
            var txtPath = PathHelper.GetPackDirectoryNamed(SELECTED_RESPACKS_FILE_NAME);

            if (File.Exists(txtPath))
            {
                return File.ReadAllLines(txtPath).Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            }
            else
            {
                // Packs will be listed in order for loading [Base -> Overrides]
                return new string[] { VANILLA_RESPACK_SYMBOL, "vanilla_fix" };
            }
        }

        public static void SaveSelectedResPacks(string[] packs)
        {
            var txtPath = PathHelper.GetPackDirectoryNamed(SELECTED_RESPACKS_FILE_NAME);

            File.WriteAllLines(txtPath, packs);
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