#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;

using CraftSharp;
using CraftSharp.Resource;



namespace MarkovCraft
{
    public class GenerationResult : MonoBehaviour
    {
        [HideInInspector] public bool Valid = true;
        [SerializeField] public float Margin = 0.5F;
        [SerializeField] private GameObject? volumeColliderHolder;
        [SerializeField] private string blockMeshLayerName = "Block";

        private readonly Dictionary<int3, GameObject> renderHolders = new();
        private readonly Dictionary<int3, HashSet<int>> renderHolderEntries = new();

        private bool completed = false;
        public bool Completed => completed;

        // Main information
        public int SizeX { get; private set; } = 0;
        public int SizeY { get; private set; } = 0;
        public int SizeZ { get; private set; } = 0;
        public CustomMappingItem[] ResultPalette { get; private set; } = { };

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

        // Set data from MarkovJunior generation
        public void SetData(Dictionary<char, CustomMappingItem> fullPalette, byte[] state, char[] legend, int sizeX, int sizeY, int sizeZ, int stepCount, string confModel, int seed)
        {
            // Generation data
            ConfiguredModelName = confModel;
            GenerationSeed = seed;
            FinalStepCount = stepCount;

            // Get a minimum set of characters in the result
            byte[] minimumCharIndices = state.ToHashSet().ToArray();
            // Character index => Result palette index(/Minimum character index)
            var charIndex2ResultIndex = new Dictionary<byte, int>();
            for (int mi = 0;mi < minimumCharIndices.Length;mi++)
            {
                charIndex2ResultIndex[minimumCharIndices[mi]] = mi;
            }
            // Remap block data
            var blockData = state.Select(ci => charIndex2ResultIndex[ci]).ToArray();
            // Calculate which indices should be air
            if (sizeZ != 1 && charIndex2ResultIndex.ContainsKey(0)) // For 3d results, legend[0] is air
            {
                AirIndices.Add(charIndex2ResultIndex[0]);
            }
            // Calculate result palette
            var resultPalette = minimumCharIndices.Select(ci => fullPalette[legend[ci]].AsCopy()).ToArray();

            // Call regular set data method
            SetData(resultPalette, blockData, sizeX, sizeY, sizeZ);
        }

        // Set data from vox model
        public void SetData(int[] state, int[]? rgbPalette, int sizeX, int sizeY, int sizeZ)
        {
            // Generation data
            ConfiguredModelName = "Vox Import";
            GenerationSeed = 0;
            FinalStepCount = 0;

            // Get a minimum set of indices in the result, add index 0 (air) if not present
            int[] minimumVoxIndices = state.ToHashSet().ToArray();
            // Vox index => Result palette index(/Minimum vox palette index)
            var voxIndex2ResultIndex = new Dictionary<int, int>();
            for (int mi = 0;mi < minimumVoxIndices.Length;mi++)
            {
                voxIndex2ResultIndex[minimumVoxIndices[mi]] = mi;
            }
            Debug.Log($"Vox remapping: {string.Join(", ", voxIndex2ResultIndex.Select(x => $"[{x.Key}, {x.Value}]"))}");
            // Remap block data
            var blockData = state.Select(ci => voxIndex2ResultIndex[ci]).ToArray();
            // Index -1 in loaded vox data is air
            AirIndices.Add(voxIndex2ResultIndex[-1]);
            // Calculate result palette
            var resultPalette = rgbPalette == null ?
                    minimumVoxIndices.Select(vi =>
                            new CustomMappingItem { Color = Color.cyan }).ToArray() :
                    minimumVoxIndices.Select(vi =>
                            new CustomMappingItem { Color = vi == -1 ? Color.black :
                                    ColorConvert.GetOpaqueColor32(rgbPalette[vi]) }).ToArray();

            // Call regular set data method
            SetData(resultPalette, blockData, sizeX, sizeY, sizeZ);
        }

        private void SetData(CustomMappingItem[] resultPalette, int[] blockData, int FX, int FY, int FZ)
        {
            // Update result final size
            SizeX = FX;
            SizeY = FY;
            SizeZ = FZ;
            // Update result block data
            ResultPalette = resultPalette;
            BlockData = blockData;
            
            completed = true;

            RequestRebuildResultMesh();
        }

        public void UpdateBlockData(int[] blockData, int FX, int FY, int FZ)
        {
            UpdateVolume(UnityPosition, FX, FY, FZ);

            // Update result final size
            SizeX = FX;
            SizeY = FY;
            SizeZ = FZ;
            // Update result block data
            BlockData = blockData;
            
            completed = true;

            renderHolderEntries.Clear();
            var renderHoldersArr = renderHolders.Values.ToArray();
            for (int i = 0;i < renderHoldersArr.Length;i++)
            {
                Destroy(renderHoldersArr[i]);
            }
            renderHolders.Clear();

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
            if (volumeColliderHolder != null)
                volumeColliderHolder.transform.localScale = GetVolumeSize();
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

            var materialManager = GameScene.Instance.GetMaterialManager();
            var statePalette = BlockStatePalette.INSTANCE;
            var packManager = ResourcePackManager.Instance;
            var stateModelTable = packManager.StateModelTable;

            int invalidBlockStateIndex = -1;

            // Cache mapped data
            var stateIdPalette = ResultPalette.Select(x =>
                    statePalette.GetStateIdCandidatesFromString(x.BlockState).
                            DefaultIfEmpty(invalidBlockStateIndex).First()).ToArray();
            
            var renderTypeTable = statePalette.RenderTypeTable;

            var renderTypePalette = stateIdPalette.Select(stateId => {
                        if (stateId == invalidBlockStateIndex)
                            return GameScene.DEFAULT_MATERIAL_INDEX;
                        
                        var stateBlockId = statePalette.GetGroupIdByNumId(stateId);
                        if (renderTypeTable.ContainsKey(stateBlockId))
                            return GameScene.GetMaterialIndex( renderTypeTable[stateBlockId] );

                        return GameScene.DEFAULT_MATERIAL_INDEX;
                    }).ToArray();
            
            var nonOpaquePalette = stateIdPalette.Select((stateId, idx) => {
                        if (AirIndices.Contains(idx))
                            return true;
                        if (stateId == invalidBlockStateIndex)
                            return false; // Default pure color cubes, consider them as full opaque blocks
                        
                        return !statePalette.GetByNumId(stateId).MeshFaceOcclusionSolid;
                    }).ToArray();
            
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

                int count = GameScene.BLOCK_RENDER_TYPES.Length, renderTypeMask = 0;
                var visualBuffer = new VertexBuffer[count];
                var nonAirIndicesInChunk = new HashSet<int>();

                int blockMeshLayer = LayerMask.NameToLayer(blockMeshLayerName);

                var buildTask = Task.Run(() => {
                    var vertexCount = new int[count];
                    for (int i = 0;i < count;i++)
                        vertexCount[i] = 0;

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

                        if (z == SizeZ - 1 || nonOpaquePalette[BlockData[x + y * SizeX + (z + 1) * SizeX * SizeY]]) // Unity +Y (Up)    | Markov +Z
                            cullFlags |= 0b000001;
                        if (z ==         0 || nonOpaquePalette[BlockData[x + y * SizeX + (z - 1) * SizeX * SizeY]]) // Unity -Y (Down)  | Markov -Z
                            cullFlags |= 0b000010;
                        if (x == SizeX - 1 || nonOpaquePalette[BlockData[(x + 1) + y * SizeX + z * SizeX * SizeY]]) // Unity +X (South) | Markov +X
                            cullFlags |= 0b000100;
                        if (x ==         0 || nonOpaquePalette[BlockData[(x - 1) + y * SizeX + z * SizeX * SizeY]]) // Unity -X (North) | Markov -X
                            cullFlags |= 0b001000;
                        if (y == SizeY - 1 || nonOpaquePalette[BlockData[x + (y + 1) * SizeX + z * SizeX * SizeY]]) // Unity +Z (East)  | Markov +Y
                            cullFlags |= 0b010000;
                        if (y ==         0 || nonOpaquePalette[BlockData[x + (y - 1) * SizeX + z * SizeX * SizeY]]) // Unity -Z (East)  | Markov +Y
                            cullFlags |= 0b100000;

                        int stateId = stateIdPalette[value];
                        int renderTypeIndex = renderTypePalette[value];

                        if (stateId == invalidBlockStateIndex)
                        {
                            if (cullFlags != 0b000000)// If at least one face is visible
                            {
                                vertexCount[renderTypeIndex] += CubeGeometry.GetVertexCount(cullFlags);
                                
                                renderTypeMask |= (1 << renderTypeIndex);
                            }
                        }
                        else
                        {
                            if (cullFlags != 0b000000)// If at least one face is visible
                            {
                                vertexCount[renderTypeIndex] += stateModelTable[stateId].Geometries[0].GetVertexCount(cullFlags);

                                renderTypeMask |= (1 << renderTypeIndex);
                            }
                        }
                    }

                    for (int i = 0;i < count;i++)
                        visualBuffer[i] = new(vertexCount[i]);
                    
                    var vertexOffset = new uint[count];
                    for (int i = 0;i < count;i++)
                        vertexOffset[i] = 0;

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

                        if (z == SizeZ - 1 || nonOpaquePalette[BlockData[x + y * SizeX + (z + 1) * SizeX * SizeY]]) // Unity +Y (Up)    | Markov +Z
                            cullFlags |= 0b000001;
                        if (z ==         0 || nonOpaquePalette[BlockData[x + y * SizeX + (z - 1) * SizeX * SizeY]]) // Unity -Y (Down)  | Markov -Z
                            cullFlags |= 0b000010;
                        if (x == SizeX - 1 || nonOpaquePalette[BlockData[(x + 1) + y * SizeX + z * SizeX * SizeY]]) // Unity +X (South) | Markov +X
                            cullFlags |= 0b000100;
                        if (x ==         0 || nonOpaquePalette[BlockData[(x - 1) + y * SizeX + z * SizeX * SizeY]]) // Unity -X (North) | Markov -X
                            cullFlags |= 0b001000;
                        if (y == SizeY - 1 || nonOpaquePalette[BlockData[x + (y + 1) * SizeX + z * SizeX * SizeY]]) // Unity +Z (East)  | Markov +Y
                            cullFlags |= 0b010000;
                        if (y ==         0 || nonOpaquePalette[BlockData[x + (y - 1) * SizeX + z * SizeX * SizeY]]) // Unity -Z (East)  | Markov +Y
                            cullFlags |= 0b100000;

                        int stateId = stateIdPalette[value];
                        int renderTypeIndex = renderTypePalette[value];

                        if (stateId == invalidBlockStateIndex)
                        {
                            if (cullFlags != 0b000000)// If at least one face is visible
                            {
                                var cubeTint = ResultPalette[value].Color;
                                CubeGeometry.Build(visualBuffer[renderTypeIndex], ref vertexOffset[renderTypeIndex], new(ix, iz, iy), ResourcePackManager.BLANK_TEXTURE, cullFlags,
                                        new(cubeTint.r / 255F, cubeTint.g / 255F, cubeTint.b / 255F));
                            }
                        }
                        else
                        {
                            if (cullFlags != 0b000000)// If at least one face is visible
                            {
                                var blockTint = statePalette.GetBlockColor(stateId, GameScene.DummyWorld, BlockLoc.Zero, statePalette.GetByNumId(stateId));
                                stateModelTable[stateId].Geometries[0].Build(visualBuffer[renderTypeIndex], ref vertexOffset[renderTypeIndex],
                                        new(ix, iz, iy), cullFlags, 0, 0F, BlockStatePreview.DUMMY_BLOCK_VERT_LIGHT, blockTint);
                            }
                        }
                    }
                });

                while (!buildTask.IsCompleted)
                    yield return null;

                if (renderTypeMask != 0) // Mesh is not empty
                {
                    if (renderHolders.ContainsKey(coord))
                    {
                        renderHolder = renderHolders[coord];
                    }
                    else
                    {
                        renderHolder = new($"[{cx} {cy} {cz}]") { layer = blockMeshLayer };
                        // Set as own child
                        renderHolder.transform.SetParent(transform, false);
                        renderHolder.transform.localPosition = new Vector3(cox - SizeX / 2F, coz - SizeZ / 2F, coy - SizeY / 2F);
                        // Add necesary components
                        renderHolder.AddComponent<MeshFilter>();
                        renderHolder.AddComponent<MeshRenderer>();
                        renderHolder.AddComponent<MeshCollider>();

                        renderHolders.Add(coord, renderHolder);
                        renderHolderEntries.Add(coord, nonAirIndicesInChunk);
                    }

                    var (mesh, materialArr) = BuildMesh(visualBuffer, renderTypeMask);

                    renderHolder.GetComponent<MeshFilter>().sharedMesh = mesh;
                    renderHolder.GetComponent<MeshCollider>().sharedMesh = mesh;
                    renderHolder.GetComponent<MeshRenderer>().sharedMaterials = materialArr;

                    yield return null;
                }
                else
                {
                    if (renderHolders.ContainsKey(coord))
                    {
                        Destroy(renderHolders[coord]);
                        renderHolders.Remove(coord);
                    }
                }
            }
        
            rebuildingMesh = false;
        }

        public static (Mesh, Material[]) BuildMesh(VertexBuffer[] visualBuffer, int layerMask)
        {
            // Count layers, vertices and face indices
            int layerCount = 0, totalVertCount = 0;
            for (int layer = 0;layer < visualBuffer.Length;layer++)
            {
                if ((layerMask & (1 << layer)) != 0)
                {
                    layerCount++;
                    totalVertCount += visualBuffer[layer].vert.Length;
                }
            }

            int triIdxCount = (totalVertCount / 2) * 3;

            var meshDataArr = Mesh.AllocateWritableMeshData(1);
            var materialArr  = new Material[layerCount];
            var meshData = meshDataArr[0];
            meshData.subMeshCount = layerCount;

            var vertAttrs = new NativeArray<VertexAttributeDescriptor>(4, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            vertAttrs[0] = new(VertexAttribute.Position,  dimension: 3, stream: 0);
            vertAttrs[1] = new(VertexAttribute.TexCoord0, dimension: 3, stream: 1);
            vertAttrs[2] = new(VertexAttribute.TexCoord3, dimension: 4, stream: 2);
            vertAttrs[3] = new(VertexAttribute.Color,     dimension: 4, stream: 3);

            // Set mesh params
            meshData.SetVertexBufferParams(totalVertCount, vertAttrs);
            vertAttrs.Dispose();

            // Prepare source data arrays
            var allVerts = new float3[totalVertCount];
            var allUVs   = new float3[totalVertCount];
            var allAnims = new float4[totalVertCount];
            var allTints = new float4[totalVertCount];

            int vertOffset = 0;
            for (int layer = 0;layer < visualBuffer.Length;layer++)
            {
                if ((layerMask & (1 << layer)) != 0)
                {
                    visualBuffer[layer].vert.CopyTo(allVerts, vertOffset);
                    visualBuffer[layer].txuv.CopyTo(allUVs,   vertOffset);
                    visualBuffer[layer].uvan.CopyTo(allAnims, vertOffset);
                    visualBuffer[layer].tint.CopyTo(allTints, vertOffset);

                    vertOffset += visualBuffer[layer].vert.Length;
                }
            }

            // Copy the source arrays to mesh data
            var positions  = meshData.GetVertexData<float3>(0);
            positions.CopyFrom(allVerts);
            var texCoords  = meshData.GetVertexData<float3>(1);
            texCoords.CopyFrom(allUVs);
            var texAnims   = meshData.GetVertexData<float4>(2);
            texAnims.CopyFrom(allAnims);
            var vertColors = meshData.GetVertexData<float4>(3);
            vertColors.CopyFrom(allTints);

            meshData.SetIndexBufferParams(triIdxCount, IndexFormat.UInt32);

            // Set face data
            var triIndices = meshData.GetIndexData<uint>();
            uint vi = 0; int ti = 0;
            for (;vi < totalVertCount;vi += 4U, ti += 6)
            {
                triIndices[ti]     = vi;
                triIndices[ti + 1] = vi + 3U;
                triIndices[ti + 2] = vi + 2U;
                triIndices[ti + 3] = vi;
                triIndices[ti + 4] = vi + 1U;
                triIndices[ti + 5] = vi + 3U;
            }

            var materialManager = GameScene.Instance.GetMaterialManager();

            // Select materials used by the mesh and split submeshes
            int subMeshIndex = 0;
            vertOffset = 0;
            for (int layer = 0;layer < visualBuffer.Length;layer++)
            {
                if ((layerMask & (1 << layer)) != 0)
                {
                    materialArr[subMeshIndex] = materialManager.GetAtlasMaterial(GameScene.BLOCK_RENDER_TYPES[layer]);
                    int vertCount = visualBuffer[layer].vert.Length;
                    meshData.SetSubMesh(subMeshIndex, new((vertOffset / 2) * 3, (vertCount / 2) * 3){ vertexCount = vertCount });
                    vertOffset += vertCount;
                    subMeshIndex++;
                }
            }

            meshData.subMeshCount = layerCount;

            var mesh = new Mesh();

            Mesh.ApplyAndDisposeWritableMeshData(meshDataArr, mesh);

            mesh.RecalculateBounds();
            // Recalculate mesh normals
            mesh.RecalculateNormals();

            return (mesh, materialArr);
        }

        public void EnableBlockColliders()
        {
            // Disable volume collider
            volumeColliderHolder!.SetActive(false);
        }

        public (int, int, int, float3, CustomMappingItem?) GetBlockPosInVolume(float3 position)
        {
            float3 pos = position - UnityPosition;

            int x = Mathf.FloorToInt(pos.x); // Unity X => Markov X
            int z = Mathf.FloorToInt(pos.y); // Unity Y => Markov Z
            int y = Mathf.FloorToInt(pos.z); // Unity Z => Markov Y

            var unityPos = UnityPosition + new float3(x, z, y);
            CustomMappingItem? c = null;

            if (x >= 0 && y >= 0 && z >= 0 && x < SizeX && y < SizeY && z < SizeZ)
                c = ResultPalette[BlockData[x + y * SizeX + z * SizeX * SizeY]];

            return (x, y, z, unityPos, c);
        }
        
        public void DisableBlockColliders()
        {
            // Enable volume collider
            volumeColliderHolder!.SetActive(true);
        }
    }
}