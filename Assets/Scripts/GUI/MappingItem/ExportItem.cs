#nullable enable
using UnityEngine;

using CraftSharp;
using CraftSharp.Resource;
using static Unity.Collections.AllocatorManager;

namespace MarkovCraft
{
    public class ExportItem : MappingItem
    {
        [HideInInspector] public int currentStateId = 0;
        [SerializeField] public GameObject? previewObject;

        private void UpdatePreview(int stateId)
        {
            if (stateId == 0) // Hide away
            {
                previewObject!.SetActive(false);
            }
            else // Show up and display specified state
            {
                previewObject!.SetActive(true);
                var blockId = BlockStatePalette.INSTANCE.GetGroupIdByNumId(stateId);
                UpdatePreviewObject(stateId, blockId);

                previewObject!.SetActive(true);
            }
        }

        private void UpdatePreviewObject(int stateId, ResourceLocation blockId)
        {
            if (stateId == currentStateId)
                return; // No need to update
            
            // Update block mesh
            if (previewObject)
            {
                var geometry = ResourcePackManager.Instance.StateModelTable[stateId].Geometries[0];
                var visualBuffer = new VertexBuffer(geometry.GetVertexCount(BlockStatePreview.PREVIEW_CULLFLAG));
                uint vertOffset = 0;

                var material = GameScene.Instance.MaterialManager!.GetAtlasMaterial(BlockStatePalette.INSTANCE.RenderTypeTable[blockId]);
                var blockTint = ColorConvert.GetFloat3(BlockStatePalette.INSTANCE.GetBlockColor(stateId, GameScene.DummyWorld, BlockLoc.Zero));

                geometry.Build(
                        visualBuffer, ref vertOffset, BlockStatePreview.ITEM_CENTER, BlockStatePreview.PREVIEW_CULLFLAG,
                        0, 0F, BlockStatePreview.DUMMY_BLOCK_VERT_LIGHT, blockTint);

                previewObject.GetComponent<MeshFilter>().sharedMesh = BlockStatePreview.BuildMesh(visualBuffer);
                previewObject.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            currentStateId = stateId;
        }

        public override void InitializeData(char character, int defoRgb, int rgb, string blockState, 
                MappingItemColorPicker colorPicker, MappingItemBlockPicker blockPicker, BlockStatePreview blockStatePreview)
        {
            base.InitializeData(character, defoRgb, rgb, blockState, colorPicker, blockPicker, blockStatePreview);

            // Initialize block state preview
            UpdatePreview(BlockStatePalette.INSTANCE.GetStateIdFromString(blockState, 0));
        }

        public override void SetBlockState(string blockState)
        {
            if (BlockStateInput!.interactable) // The blockstate input is not locked
            {
                BlockStateInput!.SetTextWithoutNotify(blockState); // Avoid updating block preview
                // Update block state preview
                UpdatePreview(BlockStatePalette.INSTANCE.GetStateIdFromString(blockState, 0));
            }
        }

        public override void TagAsSpecial(string blockState)
        {
            base.TagAsSpecial(blockState);
            // Update block state preview
            UpdatePreview(BlockStatePalette.INSTANCE.GetStateIdFromString(blockState, 0));
        }

        protected override void OnSelectBlockStateInput(string blockState)
        {
            if (BlockStatePalette.INSTANCE.TryGetStateIdFromString(blockState, out int stateId)) // Update and show preview
            {
                blockStatePreview!.UpdatePreview(stateId);
                UpdatePreview(stateId);
            }
            else // Hide preview
            {
                blockStatePreview!.UpdatePreview(0);
                UpdatePreview(0);
            }
        }

        protected override void OnUpdateBlockStateInput(string blockState)
        {
            if (BlockStatePalette.INSTANCE.TryGetStateIdFromString(blockState, out int stateId)) // Update and show preview
            {
                blockStatePreview!.UpdatePreview(stateId);
                UpdatePreview(stateId);
            }
            else // Hide preview
            {
                blockStatePreview!.UpdateHint(blockState);
                UpdatePreview(0);
            }
        }
    }
}