#nullable enable
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

using MinecraftClient;

namespace MarkovCraft
{
    [RequireComponent(typeof (CanvasGroup))]
    public class AutoMappingPanel : MonoBehaviour
    {
        [SerializeField] private RectTransform? groups;
        [SerializeField] private GameObject? groupPrefab;
        [SerializeField] private Toggle? skipAssignedBlocksToggle;

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
            var groupObj = GameObject.Instantiate(groupPrefab);
            groupObj!.transform.SetParent(groups, false);

            var group = groupObj.GetComponent<BlockGroup>();
            group.SetData(groupName, items);
        }

        public Dictionary<ResourceLocation, Color32> GetSelectedBlocks()
        {
            var result = new Dictionary<ResourceLocation, Color32>();

            foreach (var group in GetComponentsInChildren<BlockGroup>())
            {
                group.AppendSelected(ref result);
            }

            return result;
        }

        public void Show()
        {
            Initialize();

            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1F;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        public void Hide()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0F;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }
}