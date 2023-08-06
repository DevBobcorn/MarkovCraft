#nullable enable
using UnityEngine;

using MinecraftClient.Mapping;
using MinecraftClient.Resource;

namespace MarkovCraft
{
    public class ExportItem : MappingItem
    {
        [HideInInspector] public int currentStateId = -1;
        [SerializeField] public GameObject? previewObject;

        private void UpdatePreview(int stateId)
        {
            if (stateId == BlockStateHelper.INVALID_BLOCKSTATE) // Hide away
            {
                previewObject!.SetActive(false);
            }
            else // Show up and display specified state
            {
                previewObject!.SetActive(true);
                var newState = BlockStatePalette.INSTANCE.StatesTable[stateId];
                UpdatePreviewObject(stateId, newState);

                previewObject!.SetActive(true);
            }
        }

        private void UpdatePreviewObject(int stateId, BlockState newState)
        {
            if (stateId == currentStateId)
                return; // No need to update
            
            // Update block mesh
            if (previewObject != null)
            {
                var visualBuffer = new VertexBuffer();

                var blockTint = BlockStatePalette.INSTANCE.GetBlockColor(stateId, GameScene.DummyWorld, Location.Zero, newState);
                ResourcePackManager.Instance.StateModelTable[stateId].Geometries[0].Build(ref visualBuffer,
                        BlockStatePreview.ITEM_CENTER, BlockStatePreview.PREVIEW_CULLFLAG, blockTint);

                previewObject.GetComponent<MeshFilter>().sharedMesh = BlockStatePreview.BuildMesh(visualBuffer);
            }

            currentStateId = stateId;
        }

        public override void InitializeData(char character, int defoRgb, int rgb, string blockState, 
                MappingItemColorPicker colorPicker, BlockStatePreview blockStatePreview)
        {
            base.InitializeData(character, defoRgb, rgb, blockState, colorPicker, blockStatePreview);

            // Initialize block state preview
            var stateId = BlockStateHelper.GetStateIdFromString(blockState);
            UpdatePreview(stateId);
        }

        public override void SetBlockState(string blockState)
        {
            if (BlockStateInput!.interactable) // The blockstate input is not locked
            {
                BlockStateInput!.SetTextWithoutNotify(blockState); // Avoid updating block preview
                OnUpdateBlockStateInput(blockState);
            }
        }

        public override void TagAsSpecial(string blockState)
        {
            base.TagAsSpecial(blockState);
            // Update block state preview
            var stateId = BlockStateHelper.GetStateIdFromString(blockState);
            UpdatePreview(stateId);
        }

        protected override void OnSelectBlockStateInput(string blockState)
        {
            var stateId = BlockStateHelper.GetStateIdFromString(blockState);
            // Update and show preview
            blockStatePreview?.UpdatePreview(stateId);
            UpdatePreview(stateId);
        }

        protected override void OnUpdateBlockStateInput(string blockState)
        {
            var stateId = BlockStateHelper.GetStateIdFromString(blockState);

            if (stateId != BlockStateHelper.INVALID_BLOCKSTATE) // Update and show preview
            {
                blockStatePreview?.UpdatePreview(stateId);
                UpdatePreview(stateId);
            }
            else // Hide preview
            {
                blockStatePreview?.UpdateHint(blockState);
                UpdatePreview(BlockStateHelper.INVALID_BLOCKSTATE);
            }
        }
    }
}