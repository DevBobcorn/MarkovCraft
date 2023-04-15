#nullable enable
using UnityEngine;
using Unity.Mathematics;

namespace MarkovBlocks
{
    public class GenerationResult : MonoBehaviour
    {
        [HideInInspector] public int3 GenerationPosition;
        [HideInInspector] public int3 GenerationSize;
        [HideInInspector] public int Iteration;
        [HideInInspector] public int GenerationSeed;

        [HideInInspector] public bool Valid = true;

        [SerializeField] public float Margin = 0.5F;

        private bool completed = false;
        public bool Completed => completed;
        private (string[] info, byte[] state, char[] legend, int FX, int FY, int FZ)? data = null;
        public (string[] info, byte[] state, char[] legend, int FX, int FY, int FZ)? Data => data;

        public void SetData((string[] info, byte[] state, char[] legend, int FX, int FY, int FZ) data)
        {
            this.data = data;

            completed = true;
        }

        public void UpdateVolume(int3 pos, int3 size)
        {
            GenerationPosition = pos;
            GenerationSize = size;

            UpdateVolume();
        }

        public Vector3 GetVolumeSize()
        {
            return new(GenerationSize.x + Margin * 2F, GenerationSize.y + Margin, GenerationSize.z + Margin * 2F);
        }

        public Vector3 GetVolumePosition()
        {
            return new(GenerationPosition.x + GenerationSize.x / 2F,
                    GenerationPosition.y + (GenerationSize.y + Margin) / 2F,
                    GenerationPosition.z + GenerationSize.z / 2F);
        }

        private void UpdateVolume()
        {
            transform.localScale = GetVolumeSize();
            transform.position = GetVolumePosition();

        }
    }
}