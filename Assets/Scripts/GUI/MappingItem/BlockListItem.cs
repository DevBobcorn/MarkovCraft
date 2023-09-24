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

        public void SetBlockId(ResourceLocation blockId, int defaultStateId)
        {
            this.blockId = blockId;
            
            // Update preview object
            if (blockId != ResourceLocation.INVALID && previewObject != null)
            {
                var defaultState = BlockStatePalette.INSTANCE.StatesTable[defaultStateId];
                var visualBuffer = new VertexBuffer();

                var material = GameScene.Instance.MaterialManager!.GetAtlasMaterial(BlockStatePalette.INSTANCE.RenderTypeTable[blockId]);
                var blockTint = BlockStatePalette.INSTANCE.GetBlockColor(defaultStateId, GameScene.DummyWorld, Location.Zero, defaultState);
                ResourcePackManager.Instance.StateModelTable[defaultStateId].Geometries[0].Build(ref visualBuffer, BlockStatePreview.ITEM_CENTER,
                        BlockStatePreview.PREVIEW_CULLFLAG, BlockStatePreview.DUMMY_AMBIENT_OCCLUSSION, BlockStatePreview.DUMMY_BLOCK_VERT_LIGHT, blockTint);

                previewObject.GetComponent<MeshFilter>().sharedMesh = BlockStatePreview.BuildMesh(visualBuffer);
                previewObject.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
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