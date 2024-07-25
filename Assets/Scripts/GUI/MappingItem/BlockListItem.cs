#nullable enable
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

using CraftSharp;
using CraftSharp.Resource;

namespace MarkovCraft
{
    public class BlockListItem : MonoBehaviour
    {
        [SerializeField] public GameObject? previewObject;

        private ResourceLocation blockId = ResourceLocation.INVALID;
        public ResourceLocation BlockId => blockId;
        // Save identifier string and localized name for search matching
        private string blockIdString = string.Empty;
        private string localizedNameLower = string.Empty;

        public void SetBlockId(ResourceLocation blockId, int defaultStateId)
        {
            this.blockId = blockId;
            blockIdString = blockId.ToString();
            
            // Update preview object
            if (blockId != ResourceLocation.INVALID && previewObject != null)
            {
                localizedNameLower = GameScene.GetL10nBlockName(blockId).ToLower();
                var defaultState = BlockStatePalette.INSTANCE.StatesTable[defaultStateId];

                var geometry = ResourcePackManager.Instance.StateModelTable[defaultStateId].Geometries[0];
                var visualBuffer = new VertexBuffer(geometry.GetVertexCount(BlockStatePreview.PREVIEW_CULLFLAG));
                uint vertOffset = 0;

                var material = GameScene.Instance.MaterialManager!.GetAtlasMaterial(BlockStatePalette.INSTANCE.RenderTypeTable[blockId]);
                var blockTint = BlockStatePalette.INSTANCE.GetBlockColor(defaultStateId, GameScene.DummyWorld, BlockLoc.Zero, defaultState);

                geometry.Build(visualBuffer, ref vertOffset, BlockStatePreview.ITEM_CENTER, BlockStatePreview.PREVIEW_CULLFLAG,
                        0, BlockStatePreview.DUMMY_BLOCK_VERT_LIGHT, blockTint);

                previewObject.GetComponent<MeshFilter>().sharedMesh = BlockStatePreview.BuildMesh(visualBuffer);
                previewObject.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
        }

        public bool MatchesSearch(string search)
        {
            if (blockIdString.Contains(search)) // Block identifier matches
            {
                return true;
            }

            // Check if localized block name matches
            return localizedNameLower.Contains(search);
        }

        public void VisualSelect()
        {
            GetComponentInChildren<Button>().Select();
        }

        public void SetClickEvent(UnityAction action)
        {
            GetComponentInChildren<Button>().onClick.AddListener(action);
        }
    }
}