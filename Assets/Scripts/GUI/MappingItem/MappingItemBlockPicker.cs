#nullable enable
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using CraftSharp;
using CraftSharp.Resource;

namespace MarkovCraft
{
    public class MappingItemBlockPicker : MonoBehaviour
    {
        // Block list
        [SerializeField] public GameObject? BlockListItemPrefab;
        [SerializeField] public RectTransform? GridTransform;

        private readonly Dictionary<ResourceLocation, BlockListItem> blockListItems = new();

        private MappingItem? activeItem = null;
        private BlockState selectedBlockState = BlockState.AIR_STATE;

        private void SelectBlock(ResourceLocation blockId)
        {
            var defaultStateId = BlockStateHelper.GetDefaultStateId(blockId);

            if (defaultStateId != BlockStateHelper.INVALID_BLOCKSTATE)
            {
                SelectBlockState(BlockStatePalette.INSTANCE.StatesTable[defaultStateId]);
            }
            else
            {
                SelectBlockState(BlockState.AIR_STATE);
            }
        }

        private void SelectBlockState(BlockState blockState)
        {
            if (selectedBlockState == blockState)
            {
                // Target already selected
                return;
            }

            // Select our target
            selectedBlockState = blockState;
        }

        private void Open()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1F;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        private void Close()
        {
            var canvasGroup = GetComponent<CanvasGroup>();
            canvasGroup.alpha = 0F;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            // Clear 3d block preview items
            blockListItems.Clear();
            foreach (Transform block in GridTransform!)
            {
                Destroy(block.gameObject);
            }
        }

        public void OpenAndInitialize(MappingItem item, BlockState initialBlockState)
        {
            if (activeItem != null) // Opened by an item while editing another one
            {
                // Apply current blockstate to active item
                ApplyToItem(activeItem);
            }

            // Update active item
            activeItem = item;

            // Initialize block list
            blockListItems.Clear();
            foreach (Transform block in GridTransform!)
            {
                Destroy(block.gameObject);
            }

            int index = 0;

            foreach (var pair in BlockStatePalette.INSTANCE.DefaultStateTable)
            {
                var blockId = pair.Key;
                var defaultStateId = pair.Value;

                var blockListItemObj = Instantiate(BlockListItemPrefab)!;
                blockListItemObj.transform.SetParent(GridTransform, false);

                var blockListItem = blockListItemObj.GetComponent<BlockListItem>();

                blockListItem.SetBlockId(blockId, defaultStateId);
                blockListItem.SetClickEvent(() => SelectBlock(blockId));
                
                blockListItems.Add(blockId, blockListItem);

                index++;
            }

            // Update selected blockstate
            if (blockListItems.ContainsKey(initialBlockState.BlockId))
            {
                SelectBlockState(initialBlockState);
                blockListItems[initialBlockState.BlockId].VisualSelect();
            }
            else if (index > 0) // If the model list is not empty
            {
                // Select default blockstate of first block in the list
                var pair = blockListItems.First();
                SelectBlock(pair.Key);
                pair.Value.VisualSelect();
            }

            Open();
        }

        public void CloseAndDiscard()
        {
            Close();
        }

        private void ApplyToItem(MappingItem item)
        {
            item.SetBlockState(selectedBlockState.ToString());
        }

        public void CloseAndApply()
        {
            if (activeItem != null) // Active item is available
            {
                // Apply current color to active item
                ApplyToItem(activeItem);
            }

            Close();
        }
    }
}