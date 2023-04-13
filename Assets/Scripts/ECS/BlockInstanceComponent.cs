#nullable enable
using Unity.Entities;
using Unity.Mathematics;

namespace MarkovBlocks
{
    public struct BlockInstanceComponent : IComponentData
    {
        public float LifeTime;
        public float TimeLeft;
        public int3 Position;

    }
}
