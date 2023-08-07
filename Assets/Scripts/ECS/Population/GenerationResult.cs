#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Mathematics;

using MinecraftClient.Mapping;
using MinecraftClient.Resource;

namespace MarkovCraft
{
    public class GenerationResult : MonoBehaviour
    {
        private readonly int BLOCK_COLLIDER_COUNT_MAX = 1000000;

        [HideInInspector] public bool Valid = true;
        [SerializeField] public float Margin = 0.5F;
        [SerializeField] private GameObject? volumeColliderHolder;
        [SerializeField] private string blockColliderLayerName = "BlockCollider";
        [SerializeField] private Material? blockMaterial;
        private GameObject? blockColliderHolder;
        private bool blockColliderAvailable = false;

        private readonly Dictionary<int3, GameObject> renderHolders = new();
        private readonly Dictionary<int3, HashSet<int>> renderHolderEntries = new();

        private bool completed = false;
        public bool Completed => completed;

        // Main information
        public int SizeX { get; private set; } = 0;
        public int SizeY { get; private set; } = 0;
        public int SizeZ { get; private set; } = 0;
        public bool Is2D { get; private set; } = false;
        public CustomMappingItem[] ResultPalette { get; private set; } = { };
        private bool[] AirGrid = { };

        public readonly HashSet<int> AirIndices = new();
        public int[] BlockData { get; private set; } = { };
        public int FinalStepCount { get; private set; } = 0;
        [HideInInspector] public int GenerationSeed;
        // Additional information
        public string ConfiguredModelName { get; private set; } = string.Empty;
        // Unity     X  Y  Z
        // Markov    X  Z  Y
        public int3 UnityPosition { get; private set; } = int3.zero;
        public int ResultId { get; set; } = 0;
        private bool rebuildingMesh = false;

        public void SetData(Dictionary<char, CustomMappingItem> fullPalette, byte[] state, char[] legend, int FX, int FY, int FZ, int stepCount, string confModel, int seed)
        {
            if (completed)
            {
                throw new InvalidOperationException("Result data already presents");
            }

            // Update result final size
            SizeX = FX;
            SizeY = FY;
            SizeZ = FZ;
            Is2D = SizeZ == 1;
            // Get a minimum set of characters in the result
            byte[] minimumCharIndices = state.ToHashSet().ToArray();
            // Character index => Result palette index(/Minimum character index)
            var charIndex2ResultIndex = new Dictionary<byte, int>();
            for (int mi = 0;mi < minimumCharIndices.Length;mi++)
            {
                charIndex2ResultIndex[minimumCharIndices[mi]] = mi;
            }
            // Calculate which indices should be air
            if (!Is2D && charIndex2ResultIndex.ContainsKey(0)) // For 3d results, legend[0] is air
            {
                AirIndices.Add(charIndex2ResultIndex[0]);
            }
            // Calculate result palette
            ResultPalette = minimumCharIndices.Select(ci => fullPalette[legend[ci]].AsCopy()).ToArray();
            // Remap block data
            BlockData = state.Select(ci => charIndex2ResultIndex[ci]).ToArray();
            // Other data
            FinalStepCount = stepCount;
            ConfiguredModelName = confModel;
            GenerationSeed = seed;

            AirGrid = new bool[BlockData.Length];
            
            for (int i = 0;i < BlockData.Length;i++)
                AirGrid[i] = AirIndices.Contains(BlockData[i]);
            
            completed = true;

            RequestRebuildResultMesh();
        }

        public (int sizeX, int sizeY, int sizeZ, int[] blockData, int[] colors, HashSet<int> airIndices) GetPreviewData()
        {
            return (SizeX, SizeY, SizeZ, BlockData, ResultPalette.Select(
                    x => ColorConvert.GetOpaqueRGB(x.Color)).ToArray(), AirIndices );
        }

        public void UpdateVolume(int3 unityPos, int sizeX, int sizeY, int sizeZ)
        {
            UnityPosition = unityPos;
            SizeX = sizeX;
            SizeY = sizeY;
            SizeZ = sizeZ;
            UpdateVolumeCollider();
        }

        public Vector3 GetVolumeSize()
        {
            // Swap z and y size for unity
            return new(SizeX + Margin * 2F, SizeZ + Margin * 2F, SizeY + Margin * 2F);
        }

        public Vector3 GetVolumePosition()
        {
            // Swap z and y size for unity
            return new(UnityPosition.x + SizeX / 2F,
                    UnityPosition.y + SizeZ / 2F,
                    UnityPosition.z + SizeY / 2F);
        }

        private void UpdateVolumeCollider()
        {
            volumeColliderHolder!.transform.localScale = GetVolumeSize();
            transform.position = GetVolumePosition();
        }

        public void RequestRebuildResultMesh(HashSet<int>? updatedEntries = null)
        {
            if (updatedEntries is not null && updatedEntries.Count == 0)
            {
                // It's a update request but nothing is updated
                return;
            }

            if (completed && !rebuildingMesh)
            {
                rebuildingMesh = true;

                StartCoroutine(RebuildResultMesh(updatedEntries));
            }
        }

        private IEnumerator RebuildResultMesh(HashSet<int>? updatedEntries)
        {
            int chunkX = Mathf.CeilToInt(SizeX / 16F);
            int chunkY = Mathf.CeilToInt(SizeY / 16F);
            int chunkZ = Mathf.CeilToInt(SizeZ / 16F);

            var statesTable = BlockStatePalette.INSTANCE.StatesTable;
            var packManager = ResourcePackManager.Instance;
            var stateModelTable = packManager.StateModelTable;

            // Cache mapped data
            var stateIdPalette = ResultPalette.Select(x =>
                    BlockStateHelper.GetStateIdFromString(x.BlockState)).ToArray();
            
            bool updateFromExistingMesh = updatedEntries is not null;

            for (int cx = 0;cx < chunkX;cx++) for (int cy = 0;cy < chunkY;cy++) for (int cz = 0;cz < chunkZ;cz++)
            {
                GameObject renderHolder;
                int3 coord = new(cx, cy, cz);

                if (updateFromExistingMesh && renderHolderEntries.ContainsKey(coord))
                {
                    if (renderHolderEntries[coord].Intersect(updatedEntries).Count() == 0)
                    {
                        // This chunk doesn't contain any updated entries
                        // No need to rebuild its mesh, skip.
                        continue;
                    }
                }

                // Chunk origin
                int cox = cx << 4;
                int coy = cy << 4;
                int coz = cz << 4;

                var visualBuffer = new VertexBuffer();
                var nonAirIndicesInChunk = new HashSet<int>();

                var buildTask = Task.Run(() => {
                    for (int ix = 0;ix < 16;ix++) for (int iy = 0;iy < 16;iy++) for (int iz = 0;iz < 16;iz++)
                    {
                        int x = ix + cox;
                        int y = iy + coy;
                        int z = iz + coz;

                        if (x >= SizeX || y >= SizeY || z >= SizeZ) continue;
                        int pos = x + y * SizeX + z * SizeX * SizeY;

                        int value = BlockData[pos];
                        if (AirIndices.Contains(value)) continue;

                        nonAirIndicesInChunk.Add(value);

                        int cullFlags = 0b000000;

                        if (z == SizeZ - 1 || AirGrid[x + y * SizeX + (z + 1) * SizeX * SizeY]) // Unity +Y (Up)    | Markov +Z
                            cullFlags |= 0b000001;
                        if (z ==         0 || AirGrid[x + y * SizeX + (z - 1) * SizeX * SizeY]) // Unity -Y (Down)  | Markov -Z
                            cullFlags |= 0b000010;
                        if (x == SizeX - 1 || AirGrid[(x + 1) + y * SizeX + z * SizeX * SizeY]) // Unity +X (South) | Markov +X
                            cullFlags |= 0b000100;
                        if (x ==         0 || AirGrid[(x - 1) + y * SizeX + z * SizeX * SizeY]) // Unity -X (North) | Markov -X
                            cullFlags |= 0b001000;
                        if (y == SizeX - 1 || AirGrid[x + (y + 1) * SizeX + z * SizeX * SizeY]) // Unity +Z (East)  | Markov +Y
                            cullFlags |= 0b010000;
                        if (y ==         0 || AirGrid[x + (y - 1) * SizeX + z * SizeX * SizeY]) // Unity -Z (East)  | Markov +Y
                            cullFlags |= 0b100000;

                        int stateId = stateIdPalette[value];

                        if (stateId == BlockStateHelper.INVALID_BLOCKSTATE)
                        {
                            var cubeTint = ResultPalette[value].Color;

                            CubeGeometry.Build(ref visualBuffer, ResourcePackManager.BLANK_TEXTURE, ix, iz, iy, cullFlags,
                                    new(cubeTint.r / 255F, cubeTint.g / 255F, cubeTint.b / 255F));
                        }
                        else
                        {

                            if (cullFlags != 0b000000)// If at least one face is visible
                            {
                                var blockTint = BlockStatePalette.INSTANCE.GetBlockColor(stateId, GameScene.DummyWorld, Location.Zero, statesTable[stateId]);
                                stateModelTable[stateId].Geometries[0].Build(ref visualBuffer, new(ix, iz, iy), cullFlags, blockTint);
                            }
                        }
                    }
                });

                while (!buildTask.IsCompleted)
                    yield return null;

                if (visualBuffer.vert.Length > 0) // Mesh is not empty
                {
                    if (renderHolders.ContainsKey(coord))
                    {
                        renderHolder = renderHolders[coord];
                    }
                    else
                    {
                        renderHolder = new($"[{cx} {cy} {cz}]");
                        // Set as own child
                        renderHolder.transform.SetParent(transform, false);
                        renderHolder.transform.localPosition = new Vector3(cox - SizeX / 2F, coz - SizeZ / 2F, coy - SizeY / 2F);
                        // Add necesary components
                        renderHolder.AddComponent<MeshFilter>();
                        renderHolder.AddComponent<MeshRenderer>();

                        renderHolders.Add(coord, renderHolder);
                        renderHolderEntries.Add(coord, nonAirIndicesInChunk);
                    }

                    renderHolder.GetComponent<MeshRenderer>().sharedMaterial = blockMaterial;
                    renderHolder.GetComponent<MeshFilter>().sharedMesh = BlockStatePreview.BuildMesh(visualBuffer);

                    yield return null;
                }
            }
        
            rebuildingMesh = false;
        }

        public IEnumerator EnableBlockColliders()
        {
            // Disable volume collider
            volumeColliderHolder!.SetActive(false);

            yield return null;

            if (!blockColliderAvailable) // Collider Holder not present yet
            {
                // Neither empty nor too many
                if (BlockData.Length > 0 && BlockData.Length < BLOCK_COLLIDER_COUNT_MAX)
                {
                    blockColliderHolder = new GameObject("Block Colliders");
                    blockColliderHolder.transform.SetParent(transform, false);
                    blockColliderHolder.layer = LayerMask.NameToLayer(blockColliderLayerName);

                    // Add a 0.5F to xyz, not a margin (because box collider pivot is at its center)
                    // Swap z and y size for unity
                    float3 offset = new(-SizeX / 2F + 0.5F, -SizeZ / 2F + 0.5F, -SizeY / 2F + 0.5F);

                    var colliderPositions = BlockDataBuilder.GetColliderData(BlockData, AirIndices, SizeX, SizeY, SizeZ);
                    for (int i = 0;i < colliderPositions.Length;i++)
                    {
                        if (blockColliderHolder == null)
                        {
                            // Gone, give it up
                            yield break;
                        }

                        var b = blockColliderHolder.AddComponent<BoxCollider>();
                        b.center = colliderPositions[i] + offset;

                        if (i % 100 == 0) // Take a break
                        {
                            yield return null;
                        }
                    }

                    blockColliderAvailable = true;
                }
            }
            else
            {
                if (blockColliderHolder != null)
                {
                    blockColliderHolder!.SetActive(true);
                }
            }
        }

        public (int, int, int, CustomMappingItem) GetColliderPosInVolume(BoxCollider collider)
        {
            float3 offset = new(SizeX / 2F - 0.5F, SizeZ / 2F - 0.5F, SizeY / 2F - 0.5F);
            float3 pos = ((float3) collider.center) + offset;

            int x = Mathf.RoundToInt(pos.x); // Unity X => Markov X
            int z = Mathf.RoundToInt(pos.y); // Unity Y => Markov Z
            int y = Mathf.RoundToInt(pos.z); // Unity Z => Markov Y

            var c = ResultPalette[BlockData[x + y * SizeX + z * SizeX * SizeY]];

            return (x, y, z, c);
        }

        public void DisableBlockColliders()
        {
            // Enable volume collider
            volumeColliderHolder!.SetActive(true);

            if (blockColliderAvailable && blockColliderHolder != null) // Collider Holder is present
            {
                blockColliderHolder.SetActive(false);
            }
        }
    }
}