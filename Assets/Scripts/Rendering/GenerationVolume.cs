#nullable enable
using UnityEngine;
using Unity.Mathematics;

namespace MarkovBlocks
{
    public class GenerationVolume : MonoBehaviour
    {
        [SerializeField] public int3 GenerationPosition;
        [SerializeField] public int3 GenerationSize;

        [SerializeField] public int Iteration;
        [SerializeField] public int GenerationSeed;

        [SerializeField] public bool Valid = true;

        public float padding = 1F;

        public void UpdateVolume(int3 pos, int3 size)
        {
            GenerationPosition = pos;
            GenerationSize = size;

            UpdateVolume();
        }

        public Vector3 GetVolumeSize()
        {
            return new(GenerationSize.x + padding * 2F, GenerationSize.y + padding, GenerationSize.z + padding * 2F);
        }

        public Vector3 GetVolumePosition()
        {
            return new(GenerationPosition.x + GenerationSize.x / 2F,
                    GenerationPosition.y + (GenerationSize.y + padding) / 2F,
                    GenerationPosition.z + GenerationSize.z / 2F);
        }

        private void UpdateVolume()
        {
            transform.localScale = GetVolumeSize();
            transform.position = GetVolumePosition();

        }
    }
}