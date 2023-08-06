#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;

namespace MarkovCraft
{
    public class GenerationResult : MonoBehaviour
    {
        private readonly int BLOCK_COLLIDER_COUNT_MAX = 1000000;

        [HideInInspector] public bool Valid = true;
        [SerializeField] public float Margin = 0.5F;
        [SerializeField] private GameObject? volumeColliderHolder;
        [SerializeField] private string blockColliderLayerName = "BlockCollider";
        private GameObject? blockColliderHolder;
        private bool blockColliderAvailable = false;

        private bool completed = false;
        public bool Completed => completed;

        // Main information
        public int SizeX { get; private set; } = 0;
        public int SizeY { get; private set; } = 0;
        public int SizeZ { get; private set; } = 0;
        public bool Is2D { get; private set; } = false;
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

        public void SetData(Dictionary<char, CustomMappingItem> fullPalette, byte[] state, char[] legend, int FX, int FY, int FZ, int stepCount, string confModel, int seed)
        {
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

            completed = true;
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