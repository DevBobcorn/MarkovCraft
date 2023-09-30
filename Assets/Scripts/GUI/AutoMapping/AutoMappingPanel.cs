#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization.Settings;

using CraftSharp;

namespace MarkovCraft
{
    [RequireComponent(typeof (CanvasGroup))]
    public class AutoMappingPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform? groups;
        [SerializeField] private GameObject? groupPrefab;
        [SerializeField] private GameObject? autoMappingEntry;
        public bool SkipAssignedBlocks { get; private set; } = true;

        private bool initialized = false;

        private void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            var path = PathHelper.GetExtraDataFile("block_groups.json");

            if (File.Exists(path)) // Block group definition is present
            {
                // Load block groups from file
                var data = Json.ParseJson(File.ReadAllText(path));

                var langCode = LocalizationSettings.SelectedLocale.Identifier.Code;

                foreach (var pair in data.Properties)
                {
                    var groupData = pair.Value;

                    var blocks = groupData.Properties["blocks"].Properties
                            .Select(x => new BlockGroupItemInfo {
                                    BlockId = x.Key, TextureId = x.Value.StringValue }).ToArray();
                    
                    var defaultSel = false;
                    if (groupData.Properties.ContainsKey("default_selected"))
                    {
                        defaultSel = groupData.Properties["default_selected"].StringValue.ToLower() == "true";
                    }

                    var groupName = pair.Key;
                    if (groupData.Properties.ContainsKey("names") && groupData.Properties["names"].Properties.ContainsKey(langCode))
                    {
                        groupName = groupData.Properties["names"].Properties[langCode].StringValue;
                    }

                    CreateGroup(groupName, blocks, defaultSel);
                }
            }
            else
            {
                // Generate block groups
                static string localized(string key)
                {
                    return GameScene.GetL10nString(key);
                };

                // Batch create groups
                // - Colored blocks
                CreateColoredGroup(
                        localized("auto_mapper.block_group.wool"), "wool");
                CreateColoredGroupContainingUncoloredVariant(
                        localized("auto_mapper.block_group.terracotta"), "terracotta");
                CreateColoredGroup(
                        localized("auto_mapper.block_group.glazed_terracotta"), "glazed_terracotta", false);
                CreateColoredGroup(
                        localized("auto_mapper.block_group.concrete"), "concrete");
                CreateColoredGroup(
                        localized("auto_mapper.block_group.concrete_powder"), "concrete_powder", false);
                CreateColoredGroupContainingUncoloredVariant(
                        localized("auto_mapper.block_group.shulker_box"), "shulker_box", false);
                
                // - Wood blocks
                CreateGroup(localized("auto_mapper.block_group.planks"), WOOD_TYPES.Append("bamboo").Union(HYPHAE_TYPES).Select(x =>
                        new BlockGroupItemInfo { BlockId = $"{x}_planks" }).ToArray(), false);
                CreateGroup(localized("auto_mapper.block_group.wood_and_hyphae"),
                        WOOD_TYPES.Select(x => new BlockGroupItemInfo { BlockId = $"{x}_wood", TextureId = $"block/{x}_log" })
                        .Union(HYPHAE_TYPES.Select(x => new BlockGroupItemInfo { BlockId = $"{x}_hyphae", TextureId = $"block/{x}_stem" }))
                        .Union(WOOD_TYPES.Select(x => new BlockGroupItemInfo { BlockId = $"stripped_{x}_wood", TextureId = $"block/stripped_{x}_log" }))
                        .Union(HYPHAE_TYPES.Select(x => new BlockGroupItemInfo { BlockId = $"stripped_{x}_hyphae", TextureId = $"block/stripped_{x}_stem" }))
                        .ToArray(), false);
                CreateGroup(localized("auto_mapper.block_group.log_and_stem"),
                        WOOD_TYPES.Select(x => new BlockGroupItemInfo { BlockId = $"{x}_log", TextureId = $"block/{x}_log_top" })
                        .Append(new BlockGroupItemInfo { BlockId = $"bamboo_block" })
                        .Union(HYPHAE_TYPES.Select(x => new BlockGroupItemInfo { BlockId = $"{x}_stem", TextureId = $"block/{x}_stem_top" }))
                        .Union(WOOD_TYPES.Select(x => new BlockGroupItemInfo { BlockId = $"stripped_{x}_log", TextureId = $"block/stripped_{x}_log_top" }))
                        .Append(new BlockGroupItemInfo { BlockId = $"stripped_bamboo_block" })
                        .Union(HYPHAE_TYPES.Select(x => new BlockGroupItemInfo { BlockId = $"stripped_{x}_stem", TextureId = $"block/stripped_{x}_stem_top" }))
                        .ToArray(), false);
            }
        }

        private static readonly string[] COLORS = {
            "white",       "orange",      "magenta",     "light_blue",
            "yellow",      "lime",        "pink",        "gray",
            "light_gray",  "cyan",        "purple",      "blue",
            "brown",       "green",       "red",         "black"
        };

        private static readonly string[] WOOD_TYPES = {
            "oak",         "spruce",      "birch",       "jungle",
            "acacia",      "dark_oak",    "mangrove",    "cherry",
            "bamboo"
        };

        private static readonly string[] HYPHAE_TYPES = {
            "crimson",     "warped"
        };

        public void OnSkipAssignedBlocksChanged(bool skip)
        {
            SkipAssignedBlocks = skip;
        }

        private void CreateColoredGroupContainingUncoloredVariant(string groupName, string blockBaseName, bool defaultSelected = true)
        {
            CreateGroup(groupName, COLORS.Select(x =>
                    new BlockGroupItemInfo { BlockId = $"{x}_{blockBaseName}" })
                            .Append(new BlockGroupItemInfo { BlockId = blockBaseName }).ToArray(), defaultSelected);
        }

        private void CreateColoredGroup(string groupName, string blockBaseName, bool defaultSelected = true)
        {
            CreateGroup(groupName, COLORS.Select(x =>
                    new BlockGroupItemInfo { BlockId = $"{x}_{blockBaseName}" }).ToArray(), defaultSelected);
        }

        private void CreateGroup(string groupName, BlockGroupItemInfo[] items, bool defaultSelected = true)
        {
            var groupObj = Instantiate(groupPrefab);
            groupObj!.transform.SetParent(groups, false);

            var group = groupObj.GetComponent<BlockGroup>();
            group.SetData(groupName, items, defaultSelected);
        }

        private Dictionary<ResourceLocation, Color32> GetSelectedBlocks()
        {
            var result = new Dictionary<ResourceLocation, Color32>();

            foreach (var group in GetComponentsInChildren<BlockGroup>())
            {
                group.AppendSelected(ref result);
            }

            return result;
        }

        public void AutoMap(List<MappingItem> mappingItems)
        {
            var selectedBlocks = GetSelectedBlocks();

            if (selectedBlocks is not null && selectedBlocks.Count > 0)
            {
                // Perform auto mapping
                foreach (var item in mappingItems)
                {
                    if (!SkipAssignedBlocks || item.GetBlockState() == string.Empty)
                    {
                        var targetColor = ColorConvert.OpaqueColor32FromHexString(item.GetColorCode());
                        int minDist = int.MaxValue;
                        ResourceLocation pickedBlock = ResourceLocation.INVALID;

                        foreach (var block in selectedBlocks)
                        {
                            int rDist = targetColor.r - block.Value.r;
                            int gDist = targetColor.g - block.Value.g;
                            int bDist = targetColor.b - block.Value.b;
                            int newDist = rDist * rDist + gDist * gDist + bDist * bDist;
                            
                            if (newDist < minDist) // This color is closer to target color, update this entry
                            {
                                minDist = newDist;
                                pickedBlock = block.Key;
                            }
                        }

                        if (pickedBlock != ResourceLocation.INVALID) // A block is picked
                        {
                            item.SetBlockState(pickedBlock.ToString());
                            //Debug.Log($"Mapping {item.GetColorCode()} to {pickedBlock}");
                        }
                    }
                }
            }
        }

        public void Show()
        {
            Initialize();

            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1F;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            // Disable auto map entry
            if (autoMappingEntry != null)
            {
                autoMappingEntry.SetActive(false);
            }
        }

        public void Hide()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0F;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Enable auto map entry
            if (autoMappingEntry != null)
            {
                autoMappingEntry.SetActive(true);
            }
        }
    }
}