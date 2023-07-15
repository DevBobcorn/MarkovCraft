#nullable enable
using Unity.Entities;
using Unity.Mathematics;

namespace MarkovCraft
{
    public struct RegularBlockInstanceComponent : IComponentData
    {
        public float LifeTime;
        public float Timer;
        public int3 Position;
    }
}
