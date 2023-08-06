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

using MinecraftClient;
using MinecraftClient.Resource;
using MinecraftClient.Mapping;

namespace MarkovCraft
{
    public abstract class GameScene : MonoBehaviour
    {
        private static readonly char SP = Path.DirectorySeparatorChar;

        // Dummy world
        public class World : AbstractWorld { }
        public static readonly World DummyWorld = new();
        // Palettes and resources
        protected Mesh[] blockMeshes = { };
        protected BlockGeometry?[] blockGeometries = { };
        protected float3[] blockTints = { };
        protected int blockMeshCount = 0;

        // Unity config and asset files
        [SerializeField] protected VersionHolder? VersionHolder;
        [SerializeField] protected LocalizedStringTable? L10nTable;
        protected Dictionary<string, string> L10nBlockNameTable = new();
        protected string loadedDataVersionName = "MC 0.0";
        protected int loadedDataVersionInt = 0;

        [SerializeField] public Material? BlockMaterial;
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

        public int GetDataVersionInt()
        {
            return loadedDataVersionInt;
        }

        public static string GetL10nString(string key, params object[] p)
        {
            var str = Instance.L10nTable?.GetTable().GetEntry(key);
            if (str is null) return $"<{key}>";
            return string.Format(str.Value, p);
        }

        public static string GetL10nBlockName(ResourceLocation blockId) =>
                Instance.L10nBlockNameTable.GetValueOrDefault($"block.{blockId.Namespace}.{blockId.Path}", $"block.{blockId.Namespace}.{blockId.Path}");


        public virtual void Hide3dGUI() { }

        protected void GenerateBlockMeshes(Dictionary<int, int> stateId2Mesh) // StateId => Mesh index
        {
            var statePalette = BlockStatePalette.INSTANCE;
            var buffers = new VertexBuffer[blockMeshCount];

            blockGeometries = new BlockGeometry[blockMeshCount];
            blockTints = new float3[blockMeshCount];
            
            for (int i = 0;i < buffers.Length;i++)
                buffers[i] = new();

            // #0 is default cube mesh
            CubeGeometry.Build(ref buffers[0], ResourcePackManager.BLANK_TEXTURE, 0, 0, 0, 0b111111, new float3(1F));

            var modelTable = ResourcePackManager.Instance.StateModelTable;
            
            foreach (var pair in stateId2Mesh) // StateId => Mesh index
            {
                var stateId = pair.Key;

                if (modelTable.ContainsKey(stateId))
                {
                    var blockGeometry = modelTable[stateId].Geometries[0];
                    var blockTint = statePalette.GetBlockColor(stateId, DummyWorld, Location.Zero, statePalette.FromId(stateId));

                    blockGeometry.Build(ref buffers[pair.Value], float3.zero, 0b111111, blockTint);
                    
                    blockGeometries[pair.Value] = blockGeometry;
                    blockTints[pair.Value] = blockTint;
                }
                else
                {
                    Debug.LogWarning($"Model for block state #{stateId} ({statePalette.FromId(stateId)}) is not available. Using cube model instead.");
                    CubeGeometry.Build(ref buffers[pair.Value], ResourcePackManager.BLANK_TEXTURE, 0, 0, 0, 0b111111, new float3(1F));
                }
            }

            // Set result to blockMeshes
            blockMeshes = BlockMeshGenerator.GenerateMeshes(buffers);
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
            packManager.AddPack(new($"vanilla-{resVersion}"));
            packManager.AddPack(new($"vanilla_fix"));
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

            BlockMaterial!.SetTexture("_BaseMap", packManager.GetAtlasArray(RenderType.SOLID));

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