#nullable enable
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using TMPro;

using MarkovCraft.Mapping;

namespace MarkovCraft
{
    [RequireComponent(typeof (CanvasGroup))]
    public class BlockStatePreview : MonoBehaviour
    {
        public const int PREVIEW_CULLFLAG = 0b101001;

        [HideInInspector] public int currentStateId = -1;
        [SerializeField] public GameObject? previewObject;
        [SerializeField] public CanvasGroup? imageCanvasGroup;

        private Test? game;

        private CanvasGroup? canvasGroup;
        private TMP_Text? descText;

        void Start()
        {
            game = Test.Instance;

            canvasGroup = GetComponent<CanvasGroup>();
            descText = GetComponentInChildren<TMP_Text>();

            canvasGroup.alpha = 0F; // Hide on start

            if (previewObject == null)
                Debug.LogWarning("Preview Object of BlockState Preview not assigned!");

        }

        public static Mesh BuildMesh(VertexBuffer visualBuffer)
        {
            int vertexCount = visualBuffer.vert.Length;
            int triIdxCount = (vertexCount / 2) * 3;

            var meshDataArr = Mesh.AllocateWritableMeshData(1);
            var meshData = meshDataArr[0];

            var vertAttrs = new NativeArray<VertexAttributeDescriptor>(3, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            vertAttrs[0] = new(VertexAttribute.Position,  dimension: 3, stream: 0);
            vertAttrs[1] = new(VertexAttribute.TexCoord0, dimension: 3, stream: 1);
            vertAttrs[2] = new(VertexAttribute.Color,     dimension: 3, stream: 2);

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
            // Vertex colors
            var vertColors = meshData.GetVertexData<float3>(2);
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
            var incompleteBlockId = ResourceLocation.fromString(incompleteState.Split('[')[0]);
            if (string.IsNullOrEmpty(incompleteBlockId.Path))
            {
                canvasGroup!.alpha = 0F;
                return;
            }

            canvasGroup!.alpha = 1F;

            var candidates = BlockStateHelper.GetBlockIdCandidates(incompleteBlockId);

            if (candidates.Length > 0) // Display candidates
            {
                var typedLength = incompleteBlockId.ToString().Length;

                descText!.text = string.Join('\n', candidates.Select(x =>
                        $"<color=yellow>{x.ToString()[0..typedLength]}</color>{x.ToString()[typedLength..]}"));

                var palette = BlockStatePalette.INSTANCE;
                var stateId = palette.StateListTable[candidates[0]].First();
                UpdatePreviewObject(stateId, palette.StatesTable[stateId]);
                
                imageCanvasGroup!.alpha = 1F;
            }
            else
            {
                imageCanvasGroup!.alpha = 0F;
                descText!.text = "<No Candidates>";
            }
        }

        public void UpdatePreview(int stateId)
        {
            if (stateId == BlockStateHelper.INVALID_BLOCKSTATE) // Hide away
            {
                canvasGroup!.alpha = 0F;

            }
            else // Show up and display specified state
            {
                canvasGroup!.alpha = 1F;

                var newState = BlockStatePalette.INSTANCE.StatesTable[stateId];
                var blockName = "Block";

                descText!.text = $"[{stateId}] {blockName}\n{newState}";
                UpdatePreviewObject(stateId, newState);

                imageCanvasGroup!.alpha = 1F;
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

                var blockTint = BlockStatePalette.INSTANCE.GetBlockColor(stateId, game!.DummyWorld, Location.Zero, newState);
                game!.PackManager.StateModelTable[stateId].Geometries[0].Build(ref visualBuffer, float3.zero, PREVIEW_CULLFLAG, blockTint);

                previewObject.GetComponent<MeshFilter>().sharedMesh = BuildMesh(visualBuffer);
            }

            currentStateId = stateId;
        }
    }
}