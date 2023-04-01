#nullable enable
using Unity.Entities;
using Unity.Mathematics;

namespace MarkovBlocks
{
    public struct MagicComponent : IComponentData
    {
        public float LifeTime;
        public float TimeLeft;
        public int3 Position;

    }
}
