#nullable enable
using UnityEngine;
using Unity.Mathematics;

namespace MarkovCraft
{
    public class GenerationResult : MonoBehaviour
    {
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

        public void EnableBlockColliders()
        {
            // Disable volume collider
            volumeColliderHolder!.SetActive(false);

            if (blockColliderHolder == null) // Collider Holder not present yet
            {
                if (data is not null)
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
                        var b = blockColliderHolder.AddComponent<BoxCollider>();
                        b.center = colliderPositions[i] + offset;
                    }
                }
            }
            else
            {
                blockColliderHolder.SetActive(true);
            }
        }

        public void DisableBlockColliders()
        {
            // Enable volume collider
            volumeColliderHolder!.SetActive(true);

            if (blockColliderHolder != null) // Collider Holder is present
            {
                blockColliderHolder.SetActive(false);
            }
        }
    }
}