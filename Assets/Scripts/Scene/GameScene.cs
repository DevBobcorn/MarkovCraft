#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using Unity.Mathematics;

using CraftSharp;
using CraftSharp.Resource;

namespace MarkovCraft
{
    public abstract class GameScene : MonoBehaviour
    {
        private static readonly char SP = Path.DirectorySeparatorChar;

        public static readonly RenderType[] BLOCK_RENDER_TYPES =
        {
            RenderType.SOLID,
            RenderType.CUTOUT,
            RenderType.CUTOUT_MIPPED,
            RenderType.TRANSLUCENT
        };

        public static int GetMaterialIndex(RenderType renderType)
        {
            return renderType switch
            {
                RenderType.SOLID         => 0,
                RenderType.CUTOUT        => 1,
                RenderType.CUTOUT_MIPPED => 2,
                RenderType.TRANSLUCENT   => 3,
                _   => DEFAULT_MATERIAL_INDEX
            };
        }

        public const int DEFAULT_MATERIAL_INDEX = 0;

        // Dummy world
        public static readonly World DummyWorld = new();
        // Palettes and resources
        protected Mesh[] blockMeshes = { };
        protected int nextBlockMeshIndex = 0;

        // Unity config and asset files
        [SerializeField] protected VersionHolder? VersionHolder;
        [SerializeField] protected LocalizedStringTable? L10nTable;
        protected Dictionary<string, string> L10nBlockNameTable = new();
        protected string loadedDataVersionName = "MC 0.0";
        protected int loadedDataVersionInt = 0;

        [SerializeField] public MaterialManager? MaterialManager;
        [HideInInspector] public bool Loading = false;

        private static GameScene? instance;
        public static GameScene Instance
        {
            get {
                if (instance == null)
                    instance = Component.FindObjectOfType<GameScene>();

                return instance!;
            }
        }

        public MaterialManager GetMaterialManager()
        {
            return MaterialManager!;
        }

        public int GetDataVersionInt()
        {
            return loadedDataVersionInt;
        }

        public static bool CheckFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;
            
            return true;
        }

        public static string GetL10nString(string key, params object[] p)
        {
            var str = Instance.L10nTable?.GetTable().GetEntry(key);
            if (str is null) return $"<{key}>";
            return string.Format(str.Value, p);
        }

        public static string GetL10nBlockName(ResourceLocation blockId) =>
                Instance.L10nBlockNameTable.GetValueOrDefault($"block.{blockId.Namespace}.{blockId.Path}", $"block.{blockId.Namespace}.{blockId.Path}");


        public virtual void ShowSpecialGUI() { }

        public virtual void HideSpecialGUI() { }

        protected void GenerateBlockMeshes(Dictionary<int, int> stateId2Mesh, bool appendEmptyMesh = false) // StateId => Mesh index
        {
            var statePalette = BlockStatePalette.INSTANCE;
            var buffers = new VertexBuffer[nextBlockMeshIndex];

            var blockGeometries = new BlockGeometry[nextBlockMeshIndex];
            var blockTints = new float3[nextBlockMeshIndex];
            
            for (int i = 0;i < buffers.Length;i++)
                buffers[i] = new();

            // #0 is default cube mesh
            CubeGeometry.Build(ref buffers[0], float3.zero, ResourcePackManager.BLANK_TEXTURE, 0b111111, new float3(1F));

            var modelTable = ResourcePackManager.Instance.StateModelTable;
            
            foreach (var pair in stateId2Mesh) // StateId => Mesh index
            {
                var stateId = pair.Key;

                if (modelTable.ContainsKey(stateId))
                {
                    var blockGeometry = modelTable[stateId].Geometries[0];
                    var blockTint = statePalette.GetBlockColor(stateId, DummyWorld, BlockLoc.Zero, statePalette.FromId(stateId));

                    blockGeometry.Build(ref buffers[pair.Value], float3.zero, 0b111111, BlockStatePreview.DUMMY_AMBIENT_OCCLUSSION,
                            BlockStatePreview.DUMMY_BLOCK_VERT_LIGHT, blockTint);
                    
                    blockGeometries[pair.Value] = blockGeometry;
                    blockTints[pair.Value] = blockTint;
                }
                else
                {
                    Debug.LogWarning($"Model for block state #{stateId} ({statePalette.FromId(stateId)}) is not available. Using cube model instead.");
                    CubeGeometry.Build(ref buffers[pair.Value], float3.zero, ResourcePackManager.BLANK_TEXTURE, 0b111111, new float3(1F));
                }
            }

            // Set result to blockMeshes, and append an empty mesh
            blockMeshes = BlockMeshGenerator.GenerateMeshes(buffers);

            // Append an empty mesh. This empty mesh will be used as mesh for air block
            if (appendEmptyMesh)
            {
                blockMeshes = blockMeshes.Append(new Mesh()).ToArray();
            }
        }

        protected IEnumerator LoadMCBlockData(Action? prepare = null, Action<string>? update = null, Action? callback = null)
        {
            Loading = true;
            
            prepare?.Invoke();

            var ver = VersionHolder!.Versions[VersionHolder.SelectedVersion];
            var dataVersion = ver.DataVersion;
            var resVersion = ver.ResourceVersion;
            loadedDataVersionName = ver.Name;
            loadedDataVersionInt = ver.DataVersionInt;

            Debug.Log($"Loading data version {loadedDataVersionName} ({loadedDataVersionInt})");

            // Wait for splash animation to complete...
            yield return new WaitForSecondsRealtime(0.5F);

            // Generate default data or check update
            var extraDataDir = PathHelper.GetExtraDataDirectory();
            yield return StartCoroutine(BuiltinResourceHelper.ReadyBuiltinResource(
                    MarkovGlobal.MARKOV_CRAFT_BUILTIN_FILE_NAME, MarkovGlobal.MARKOV_CRAFT_BUILTIN_VERSION, extraDataDir,
                    (status) => Loom.QueueOnMainThread(() => update?.Invoke(status)),
                    () => { }, (succeed) => { }));

            // First load all possible Block States...
            var loadFlag = new DataLoadFlag();
            Task.Run(() => BlockStatePalette.INSTANCE.PrepareData(dataVersion, loadFlag));
            while (!loadFlag.Finished) yield return null;
            
            // Then load all Items...
            // [Code removed]

            // Get resource pack manager...
            var packManager = ResourcePackManager.Instance;

            // Load resource packs...
            packManager.ClearPacks();
            // Collect packs
            var selected = MarkovGlobal.LoadSelectedResPacks();
            foreach (var packName in selected)
            {
                packManager.AddPack(new ResourcePack(packName == MarkovGlobal.
                        VANILLA_RESPACK_SYMBOL ? $"vanilla-{resVersion}" : packName));
            }
            // Load valid packs...
            loadFlag.Finished = false;
            Task.Run(() => packManager.LoadPacks(loadFlag,
                    (status) => Loom.QueueOnMainThread(() => update?.Invoke(status))));
            while (!loadFlag.Finished) yield return null;

            Loading = false;

            if (loadFlag.Failed)
            {
                Debug.LogWarning("Block data loading failed");
                yield break;
            }

            MaterialManager!.ClearInitializeFlag();
            MaterialManager.EnsureInitialized();

            yield return null;

            var mcLang = LocalizationSettings.SelectedLocale.Identifier.Code.ToLower() switch
            {
                "zh-hans" => "zh_cn",

                _         => "en_us"
            };

            var langPath = PathHelper.GetPackDirectoryNamed(
                    $"vanilla-{resVersion}{SP}assets{SP}minecraft{SP}lang{SP}{mcLang}.json");

            // Load translated block names
            L10nBlockNameTable.Clear();

            if (File.Exists(langPath)) // Json file present, just load it
            {
                foreach (var entry in Json.ParseJson(File.ReadAllText(langPath)).Properties.Where(x => x.Key.StartsWith("block.")))
                    L10nBlockNameTable.Add(entry.Key, entry.Value.StringValue);
            }
            else // Not present yet, try downloading it
            {
                yield return StartCoroutine(ResourceDownloader.DownloadLanguageJson(resVersion, mcLang,
                    (status) => Loom.QueueOnMainThread(() => update?.Invoke(status)),
                    () => { }, (succeed) => {
                        if (succeed) // Downloaded successfully, load it now
                            foreach (var entry in Json.ParseJson(File.ReadAllText(langPath)).Properties.Where(x => x.Key.StartsWith("block.")))
                                L10nBlockNameTable.Add(entry.Key, entry.Value.StringValue);
                        else
                            Debug.LogWarning($"Language file not available at {langPath}, block names not loaded.");
                    }));
            }

            callback?.Invoke();
        }

        public abstract void ReturnToMenu();
    }
}