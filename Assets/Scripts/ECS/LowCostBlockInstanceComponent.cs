#nullable enable
using Unity.Entities;
using Unity.Mathematics;

namespace MarkovCraft
{
    public struct LowCostBlockInstanceComponent : IComponentData
    {
        public float LifeTime;
        public float Timer;
        public int Identifier;
        public int3 Position; // Unity coordinates
    }
}
