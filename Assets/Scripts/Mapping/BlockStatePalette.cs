using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEngine;
using Unity.Mathematics;

namespace MarkovBlocks.Mapping
{
    public class BlockStatePalette
    {
        public static readonly BlockStatePalette INSTANCE = new();

        public BlockState FromId(int stateId)
        {
            return statesTable.GetValueOrDefault(stateId, BlockState.AIR_STATE);
        }

        public ResourceLocation GetBlock(int stateId)
        {
            return blocksTable.GetValueOrDefault(stateId, BlockState.AIR_ID);
        }

        public HashSet<int> GetStatesOfBlock(ResourceLocation blockId)
        {
            return stateListTable.GetValueOrDefault(blockId, new HashSet<int>());
        }

        private readonly Dictionary<ResourceLocation, HashSet<int>> stateListTable = new Dictionary<ResourceLocation, HashSet<int>>();
        public Dictionary<ResourceLocation, HashSet<int>> StateListTable { get { return stateListTable; } }

        private readonly Dictionary<int, ResourceLocation> blocksTable = new Dictionary<int, ResourceLocation>();
        public Dictionary<int, ResourceLocation> BlocksTable { get { return blocksTable; } }

        private readonly Dictionary<int, BlockState> statesTable = new Dictionary<int, BlockState>();
        public Dictionary<int, BlockState> StatesTable { get { return statesTable; } }

        public readonly Dictionary<ResourceLocation, RenderType> RenderTypeTable = new();

        public IEnumerator PrepareData(string dataVersion, DataLoadFlag flag, LoadStateInfo loadStateInfo)
        {
            // Clean up first...
            statesTable.Clear();
            blocksTable.Clear();
            stateListTable.Clear();
            RenderTypeTable.Clear();

            HashSet<int> knownStates = new HashSet<int>();

            string statesPath = PathHelper.GetExtraDataFile($"blocks-{dataVersion}.json");
            string listsPath  = PathHelper.GetExtraDataFile("block_lists.json");
            string colorsPath = PathHelper.GetExtraDataFile("block_colors.json");
            string renderTypePath = PathHelper.GetExtraDataFile("block_render_type.json");

            if (!File.Exists(statesPath) || !File.Exists(listsPath) || !File.Exists(colorsPath) || !File.Exists(renderTypePath))
            {
                loadStateInfo.infoText = "Block data not complete!";
                flag.Finished = true;
                flag.Failed = true;
                yield break;
            }

            // First read special block lists...
            var lists = new Dictionary<string, HashSet<ResourceLocation>>();
            lists.Add("no_occlusion", new());
            lists.Add("no_collision", new());
            lists.Add("water_blocks", new());
            lists.Add("always_fulls", new());
            lists.Add("empty_blocks", new());

            Json.JSONData spLists = Json.ParseJson(File.ReadAllText(listsPath, Encoding.UTF8));
            loadStateInfo.infoText = $"Reading special lists from {listsPath}";

            int count = 0, yieldCount = 200;

            foreach (var pair in lists)
            {
                if (spLists.Properties.ContainsKey(pair.Key))
                {
                    foreach (var block in spLists.Properties[pair.Key].DataArray)
                    {
                        pair.Value.Add(ResourceLocation.fromString(block.StringValue));
                        count++;
                        if (count % yieldCount == 0)
                            yield return null;
                    }
                }
            }

            // References for later use
            ResourceLocation lavaId   = new("lava");
            var noOcclusion = lists["no_occlusion"];
            var noCollision = lists["no_collision"];
            var waterBlocks = lists["water_blocks"];
            var alwaysFulls = lists["always_fulls"];
            var emptyBlocks = lists["empty_blocks"];

            // Then read block states...
            Json.JSONData palette = Json.ParseJson(File.ReadAllText(statesPath, Encoding.UTF8));
            Debug.Log("Reading block states from " + statesPath);
            count = 0;
            foreach (KeyValuePair<string, Json.JSONData> item in palette.Properties)
            {
                ResourceLocation blockId = ResourceLocation.fromString(item.Key);

                if (stateListTable.ContainsKey(blockId))
                    throw new InvalidDataException($"Duplicate block id {blockId}!");
                
                stateListTable[blockId] = new HashSet<int>();

                foreach (Json.JSONData state in item.Value.Properties["states"].DataArray)
                {
                    int stateId = int.Parse(state.Properties["id"].StringValue);

                    if (knownStates.Contains(stateId))
                        throw new InvalidDataException($"Duplicate state id {stateId}!?");

                    knownStates.Add(stateId);
                    blocksTable[stateId] = blockId;
                    stateListTable[blockId].Add(stateId);

                    if (state.Properties.ContainsKey("properties"))
                    {
                        // This block state contains block properties
                        var props = new Dictionary<string, string>();

                        var inWater = waterBlocks.Contains(blockId);

                        foreach (var prop in state.Properties["properties"].Properties)
                        {
                            props.Add(prop.Key, prop.Value.StringValue);

                            // Special proc for waterlogged property...
                            if (prop.Key == "waterlogged" && prop.Value.StringValue == "true")
                                inWater = true;

                        }

                        statesTable[stateId] = new BlockState(blockId, props)
                        {
                            NoOcclusion = noOcclusion.Contains(blockId),
                            NoCollision = noCollision.Contains(blockId),
                            InWater = inWater,
                            InLava  = blockId == lavaId,
                            LikeAir = emptyBlocks.Contains(blockId),
                            FullSolid = (!noOcclusion.Contains(blockId)) && alwaysFulls.Contains(blockId)
                        };
                    }
                    else
                    {
                        statesTable[stateId] = new BlockState(blockId)
                        {
                            NoOcclusion = noOcclusion.Contains(blockId),
                            NoCollision = noCollision.Contains(blockId),
                            InWater = waterBlocks.Contains(blockId),
                            InLava  = blockId == lavaId,
                            LikeAir = emptyBlocks.Contains(blockId),
                            FullSolid = (!noOcclusion.Contains(blockId)) && alwaysFulls.Contains(blockId)
                        };
                    }

                    // Count per state so that loading time can be more evenly distributed
                    count++;
                    if (count % 10 == 0)
                    {
                        loadStateInfo.infoText = $"Loading states of block {item.Key}";
                        yield return null;
                    }

                }
            }

            Debug.Log($"{statesTable.Count} block states loaded.");

            // Load block color rules...
            // [Code removed]
            
            // Load and apply block render types...
            loadStateInfo.infoText = $"Loading block render types";
            yield return null;

            try
            {
                var renderTypeText = File.ReadAllText(renderTypePath);
                var renderTypes = Json.ParseJson(renderTypeText);

                var allBlockIds = stateListTable.Keys.ToHashSet();

                foreach (var pair in renderTypes.Properties)
                {
                    var blockId = ResourceLocation.fromString(pair.Key);

                    if (allBlockIds.Contains(blockId))
                    {
                        var type = pair.Value.StringValue.ToLower() switch
                        {
                            "solid"         => RenderType.SOLID,
                            "cutout"        => RenderType.CUTOUT,
                            "cutout_mipped" => RenderType.CUTOUT_MIPPED,
                            "translucent"   => RenderType.TRANSLUCENT,

                            _               => RenderType.SOLID
                        };

                        RenderTypeTable.Add(blockId, type);

                        allBlockIds.Remove(blockId);
                    }

                }

                foreach (var blockId in allBlockIds) // Other blocks which doesn't its render type specifically stated
                {
                    RenderTypeTable.Add(blockId, RenderType.SOLID); // Default to solid
                }

            }
            catch (IOException e)
            {
                Debug.LogWarning($"Failed to load block render types: {e.Message}");
                loadStateInfo.infoText = $"Failed to load block render types: {e.Message}";
                flag.Failed = true;
            }

            Debug.Log($"Render type of {RenderTypeTable.Count} blocks loaded.");

            flag.Finished = true;

        }

    }
}