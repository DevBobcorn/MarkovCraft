#nullable enable
using System.Linq;
using UnityEngine;

using CraftSharp;
using CraftSharp.Resource;

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
                var material = GameScene.Instance.MaterialManager!.GetAtlasMaterial(BlockStatePalette.INSTANCE.RenderTypeTable[newState.BlockId]);
                var blockTint = BlockStatePalette.INSTANCE.GetBlockColor(stateId, GameScene.DummyWorld, Location.Zero, newState);
                ResourcePackManager.Instance.StateModelTable[stateId].Geometries[0].Build(
                        ref visualBuffer, BlockStatePreview.ITEM_CENTER, BlockStatePreview.PREVIEW_CULLFLAG,
                        BlockStatePreview.DUMMY_AMBIENT_OCCLUSSION, BlockStatePreview.DUMMY_BLOCK_VERT_LIGHT, blockTint);

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
            var stateId = BlockStateHelper.GetStateIdFromString(blockState);
            UpdatePreview(stateId);
        }

        public override void SetBlockState(string blockState)
        {
            if (BlockStateInput!.interactable) // The blockstate input is not locked
            {
                BlockStateInput!.SetTextWithoutNotify(blockState); // Avoid updating block preview

                var stateId = BlockStateHelper.GetStateIdFromString(blockState);
                
                if (stateId != BlockStateHelper.INVALID_BLOCKSTATE) // Update and show preview
                {
                    UpdatePreview(stateId);
                }
                else // Hide preview
                {
                    UpdatePreview(BlockStateHelper.INVALID_BLOCKSTATE);
                }
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