#nullable enable
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using TMPro;

using CraftSharp;
using CraftSharp.Resource;

namespace MarkovCraft
{
    [RequireComponent(typeof (CanvasGroup))]
    public class BlockStatePreview : MonoBehaviour
    {
        public static readonly bool[] DUMMY_AMBIENT_OCCLUSSION = Enumerable.Repeat(false, 27).ToArray();
        public static readonly float[] DUMMY_BLOCK_VERT_LIGHT = Enumerable.Repeat(0F, 8).ToArray();

        public static readonly float3 ITEM_CENTER = new(-0.5F, -0.5F, -0.5F);
        public const int PREVIEW_CULLFLAG = 0b011001;

        [HideInInspector] public int currentStateId = -1;
        [SerializeField] private GameObject? previewObject;
        [SerializeField] private TMP_Text? descText;

        private CanvasGroup? canvasGroup;

        void Start()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            descText = GetComponentInChildren<TMP_Text>();

            canvasGroup.alpha = 0F; // Hide on start
            previewObject!.SetActive(false);

            if (previewObject == null)
            {
                Debug.LogWarning("Preview Object of BlockState Preview not assigned!");
            }
        }

        public static Mesh BuildMesh(VertexBuffer visualBuffer)
        {
            int vertexCount = visualBuffer.vert.Length;
            int triIdxCount = (vertexCount / 2) * 3;

            var meshDataArr = Mesh.AllocateWritableMeshData(1);
            var meshData = meshDataArr[0];

            var vertAttrs = new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            vertAttrs[0] = new(VertexAttribute.Position,  dimension: 3, stream: 0);
            vertAttrs[1] = new(VertexAttribute.TexCoord0, dimension: 3, stream: 1);
            vertAttrs[2] = new(VertexAttribute.TexCoord3, dimension: 4, stream: 2);
            vertAttrs[3] = new(VertexAttribute.Color,     dimension: 4, stream: 3);

            // Set mesh params
            meshData.SetVertexBufferParams(vertexCount, vertAttrs);
            vertAttrs.Dispose();

            meshData.SetIndexBufferParams(triIdxCount, IndexFormat.UInt32);

            // Set vertex data
            // Positions
            var positions = meshData.GetVertexData<float3>(0);
            positions.CopyFrom(visualBuffer.vert);
            // Tex Coordinates
            var texCoords = meshData.GetVertexData<float3>(1);
            texCoords.CopyFrom(visualBuffer.txuv);
            // Animation Info
            var animInfos = meshData.GetVertexData<float4>(2);
            animInfos.CopyFrom(visualBuffer.uvan);
            // Vertex colors
            var vertColors = meshData.GetVertexData<float4>(3);
            vertColors.CopyFrom(visualBuffer.tint);

            // Set face data
            var triIndices = meshData.GetIndexData<uint>();
            uint vi = 0; int ti = 0;
            for (;vi < vertexCount;vi += 4U, ti += 6)
            {
                triIndices[ti]     = vi;
                triIndices[ti + 1] = vi + 3U;
                triIndices[ti + 2] = vi + 2U;
                triIndices[ti + 3] = vi;
                triIndices[ti + 4] = vi + 1U;
                triIndices[ti + 5] = vi + 3U;
            }

            meshData.subMeshCount = 1;

            meshData.SetSubMesh(0, new SubMeshDescriptor(0, triIdxCount)
            {
                vertexCount = vertexCount
            });

            var mesh = new Mesh();

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArr, mesh);

            mesh.RecalculateBounds();
            // Recalculate mesh normals
            mesh.RecalculateNormals();

            return mesh;
        }

        public void UpdateHint(string incompleteState)
        {
            var incompleteBlockId = ResourceLocation.FromString(incompleteState.Split('[')[0]);
            if (string.IsNullOrEmpty(incompleteBlockId.Path))
            {
                canvasGroup!.alpha = 0F;
                previewObject!.SetActive(false);
                return;
            }

            canvasGroup!.alpha = 1F;
            previewObject!.SetActive(true);

            var candidates = BlockStatePalette.GetBlockIdCandidates(incompleteBlockId);

            if (candidates.Length > 0) // Display candidates
            {
                var typedLength = incompleteBlockId.ToString().Length;

                descText!.text = string.Join('\n', candidates.Select(x =>
                        $"<color=yellow>{x.ToString()[0..typedLength]}</color>{x.ToString()[typedLength..]}"));

                var palette = BlockStatePalette.INSTANCE;
                var stateId = palette.DefaultStateTable[candidates[0]];
                UpdatePreviewObject(stateId, palette.StatesTable[stateId]);
                
                previewObject!.SetActive(true);
            }
            else
            {
                descText!.text = GameScene.GetL10nString("blockstate_preview.info.no_candidates");
                previewObject!.SetActive(false);
            }
        }

        public void UpdatePreview(int stateId)
        {
            if (stateId == 0) // Hide away
            {
                canvasGroup!.alpha = 0F;
                previewObject!.SetActive(false);
            }
            else // Show up and display specified state
            {
                canvasGroup!.alpha = 1F;
                previewObject!.SetActive(true);

                var newState = BlockStatePalette.INSTANCE.StatesTable[stateId];
                var blockName = GameScene.GetL10nBlockName(newState.BlockId);

                descText!.text = $"{blockName}\n{newState}";
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
                var blockTint = BlockStatePalette.INSTANCE.GetBlockColor(stateId, GameScene.DummyWorld, BlockLoc.Zero, newState);
                ResourcePackManager.Instance.StateModelTable[stateId].Geometries[0].Build(ref visualBuffer, ITEM_CENTER,
                        PREVIEW_CULLFLAG, DUMMY_AMBIENT_OCCLUSSION, DUMMY_BLOCK_VERT_LIGHT, blockTint);

                previewObject.GetComponent<MeshFilter>().sharedMesh = BuildMesh(visualBuffer);
                previewObject.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            currentStateId = stateId;
        }
    }
}