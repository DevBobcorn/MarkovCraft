#nullable enable
using Unity.Entities;
using Unity.Mathematics;

namespace MarkovCraft
{
    public struct LowCostBlockInstanceComponent : IComponentData
    {
        public int DataIndex;
        public int3 Position; // Unity coordinates
    }
}
