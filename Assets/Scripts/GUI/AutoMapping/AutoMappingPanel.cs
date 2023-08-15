#nullable enable
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

using CraftSharp;

namespace MarkovCraft
{
    [RequireComponent(typeof (CanvasGroup))]
    public class AutoMappingPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform? groups;
        [SerializeField] private GameObject? groupPrefab;
        [SerializeField] private Toggle? skipAssignedBlocksToggle;

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

            string localized(string key)
            {
                return GameScene.GetL10nString(key);
            };

            // Batch create groups
            CreateColoredGroup(
                    localized("auto_mapper.block_group.wool"), "wool");
            CreateColoredGroupContainingUncoloredVariant(
                    localized("auto_mapper.block_group.terracotta"), "terracotta");
            CreateColoredGroupContainingUncoloredVariant(
                    localized("auto_mapper.block_group.shulker_box"), "shulker_box");
            CreateColoredGroup(
                    localized("auto_mapper.block_group.concrete"), "concrete");
            CreateColoredGroup(
                    localized("auto_mapper.block_group.concrete_powder"), "concrete_powder");

            // Initialize block groups
            foreach (var group in GetComponentsInChildren<BlockGroup>())
            {
                group.Initialize();
            }
        }

        private static readonly string[] COLORS = {
            "white",       "orange",      "magenta",     "light_blue",
            "yellow",      "lime",        "pink",        "gray",
            "light_gray",  "cyan",        "purple",      "blue",
            "brown",       "green",       "red",         "black"
        };

        public void OnSkipAssignedBlocksChanged(bool skip)
        {
            SkipAssignedBlocks = skip;
        }

        private void CreateColoredGroupContainingUncoloredVariant(string groupName, string blockBaseName)
        {
            CreateGroup(groupName, COLORS.Select(x =>
                    new BlockGroupItemInfo { BlockId = $"{x}_{blockBaseName}" })
                            .Append(new BlockGroupItemInfo { BlockId = blockBaseName }).ToArray());
        }

        private void CreateColoredGroup(string groupName, string blockBaseName)
        {
            CreateGroup(groupName, COLORS.Select(x =>
                    new BlockGroupItemInfo { BlockId = $"{x}_{blockBaseName}" }).ToArray());
        }

        private void CreateGroup(string groupName, BlockGroupItemInfo[] items)
        {
            var groupObj = Instantiate(groupPrefab);
            groupObj!.transform.SetParent(groups, false);

            var group = groupObj.GetComponent<BlockGroup>();
            group.SetData(groupName, items);
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