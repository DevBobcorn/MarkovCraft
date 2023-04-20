using System.Collections.Generic;
using UnityEngine;

using MarkovCraft.Mapping;

namespace MarkovCraft
{
    public class ResourcePackManager
    {
        // Identifier -> Texture file path
        public readonly Dictionary<ResourceLocation, string> TextureFileTable = new();

        // Identidier -> Block json model file path
        public readonly Dictionary<ResourceLocation, string> BlockModelFileTable = new();

        // Identidier -> BlockState json model file path
        public readonly Dictionary<ResourceLocation, string> BlockStateFileTable = new();

        // Identifier -> Block model
        public readonly Dictionary<ResourceLocation, JsonModel> BlockModelTable = new();

        // Block state numeral id -> Block state geometries (One single block state may have a list of models to use randomly)
        public readonly Dictionary<int, BlockStateModel> StateModelTable = new();

        public readonly BlockModelLoader BlockModelLoader;
        public readonly BlockStateModelLoader StateModelLoader;

        public int GeneratedItemModelPrecision { get; set; } = 16;
        public int GeneratedItemModelThickness { get; set; } =  1;

        private readonly List<ResourcePack> packs = new List<ResourcePack>();

        public ResourcePackManager()
        {
            // Block model loaders
            BlockModelLoader = new BlockModelLoader(this);
            StateModelLoader = new BlockStateModelLoader(this);
        }

        public void AddPack(ResourcePack pack)
        {
            packs.Add(pack);
        }

        public void ClearPacks()
        {
            packs.Clear();
            TextureFileTable.Clear();
            BlockModelTable.Clear();
            StateModelTable.Clear();
        }

        public void LoadPacks(DataLoadFlag flag, LoadStateInfo loadStateInfo)
        {
            System.Diagnostics.Stopwatch sw = new();
            sw.Start();

            // Gather all textures and model files
            foreach (var pack in packs)
            {
                if (pack.IsValid)
                    pack.GatherResources(this, loadStateInfo);
                
            }

            var atlasFlag = new DataLoadFlag();

            // Load texture atlas (on main thread)...
            Loom.QueueOnMainThread(() => {
                Test.Instance.StartCoroutine(AtlasManager.Generate(this, loadStateInfo, atlasFlag));
            });
            
            while (!atlasFlag.Finished) { /* Wait */ }

            // Load block models...
            foreach (var blockModelId in BlockModelFileTable.Keys)
            {
                // This model loader will load this model, its parent model(if not yet loaded),
                // and then add them to the manager's model dictionary
                BlockModelLoader.LoadBlockModel(blockModelId);
            }

            // Load item models...
            // [Code removed]

            BuildStateGeometries(loadStateInfo);
            // [Code removed]

            // Perform integrity check...
            var statesTable = BlockStatePalette.INSTANCE.StatesTable;

            foreach (var stateItem in statesTable)
            {
                if (!StateModelTable.ContainsKey(stateItem.Key))
                {
                    Debug.LogWarning($"Model for {stateItem.Value}(state Id {stateItem.Key}) not loaded!");
                }
            }

            loadStateInfo.InfoText = string.Empty;

            Debug.Log($"Resource packs loaded in {sw.ElapsedMilliseconds} ms.");
            Debug.Log($"Built {StateModelTable.Count} block state geometry lists.");

            flag.Finished = true;
        }

        public void BuildStateGeometries(LoadStateInfo loadStateInfo)
        {
            loadStateInfo.InfoText = $"Building block state geometries";

            // Load all blockstate files and build their block meshes...
            foreach (var blockPair in BlockStatePalette.INSTANCE.StateListTable)
            {
                var blockId = blockPair.Key;
                
                if (BlockStateFileTable.ContainsKey(blockId)) // Load the state model definition of this block
                {
                    var renderType =
                        BlockStatePalette.INSTANCE.RenderTypeTable.GetValueOrDefault(blockId, RenderType.SOLID);

                    StateModelLoader.LoadBlockStateModel(this, blockId, BlockStateFileTable[blockId], renderType);
                }
                else
                    Debug.LogWarning($"Block state model definition not assigned for {blockId}!");
                
            }

        }

    }
}