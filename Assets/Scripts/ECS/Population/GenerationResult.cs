#nullable enable
using System.Collections;
using UnityEngine;
using Unity.Mathematics;

namespace MarkovCraft
{
    public class GenerationResult : MonoBehaviour
    {
        private int BLOCK_COLLIDER_COUNT_MAX = 1000000;

        [HideInInspector] public int3 GenerationPosition;
        [HideInInspector] public int3 GenerationSize;
        [HideInInspector] public int Iteration;
        [HideInInspector] public int GenerationSeed;

        [HideInInspector] public bool Valid = true;

        public int FinalStepCount => Data?.stepCount ?? 0;

        [SerializeField] public float Margin = 0.5F;
        [SerializeField] private GameObject? volumeColliderHolder;
        [SerializeField] private string blockColliderLayerName = "BlockCollider";
        private GameObject? blockColliderHolder;
        private bool blockColliderAvailable = false;

        private bool completed = false;
        public bool Completed => completed;
        private (string[] info, byte[] state, char[] legend, int FX, int FY, int FZ, int stepCount)? data = null;
        public (string[] info, byte[] state, char[] legend, int FX, int FY, int FZ, int stepCount)? Data => data;

        public void SetData((string[] info, byte[] state, char[] legend, int FX, int FY, int FZ, int stepCount) data)
        {
            this.data = data;
            completed = true;
        }

        public void UpdateVolume(int3 pos, int3 size)
        {
            GenerationPosition = pos;
            GenerationSize = size;

            UpdateVolumeCollider();
        }

        public Vector3 GetVolumeSize()
        {
            return new(GenerationSize.x + Margin * 2F, GenerationSize.y + Margin * 2F, GenerationSize.z + Margin * 2F);
        }

        public Vector3 GetVolumePosition()
        {
            return new(GenerationPosition.x + GenerationSize.x / 2F,
                    GenerationPosition.y + GenerationSize.y / 2F,
                    GenerationPosition.z + GenerationSize.z / 2F);
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
                if (data is not null && data.Value.state.Length < BLOCK_COLLIDER_COUNT_MAX)
                {
                    blockColliderHolder = new GameObject("Block Colliders");
                    blockColliderHolder.transform.SetParent(transform, false);
                    blockColliderHolder.layer = LayerMask.NameToLayer(blockColliderLayerName);

                    // Add a 0.5F to xyz, not a margin (because box collider pivot is at its center)
                    float3 offset = new float3(-GenerationSize.x / 2F + 0.5F, -GenerationSize.y / 2F + 0.5F, -GenerationSize.z / 2F + 0.5F);

                    (string[] _, byte[] state, char[] _, int FX, int FY, int FZ, int _) = data.Value;

                    var colliderPositions = BlockDataBuilder.GetColliderData(state, FX, FY, FZ);
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

        public (int, int, int, char) GetColliderPosInVolume(BoxCollider collider)
        {
            float3 offset = new float3(GenerationSize.x / 2F - 0.5F, GenerationSize.y / 2F - 0.5F, GenerationSize.z / 2F - 0.5F);
            float3 pos = ((float3) collider.center) + offset;

            int x = Mathf.RoundToInt(pos.x); // Unity X => Markov X
            int z = Mathf.RoundToInt(pos.y); // Unity Y => Markov Z
            int y = Mathf.RoundToInt(pos.z); // Unity Z => Markov Y

            (string[] _, byte[] state, char[] legend, int FX, int FY, int FZ, int _) = data!.Value;
            char c = legend[state[x + y * FX + z * FX * FY]];

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