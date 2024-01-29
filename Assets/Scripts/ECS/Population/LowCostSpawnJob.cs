#nullable enable
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace MarkovCraft
{
    [GenerateTestsForBurstCompatibility]
    public struct LowCostSpawnJob : IJobParallelFor
    {
        public Entity Prototype;
        public EntityCommandBuffer.ParallelWriter Ecb;

        [ReadOnly]
        public NativeArray<int3> PositionData; // Unity coordinates

        [ReadOnly]
        public int EmptyMeshIndex;

        public void Execute(int index)
        {
            var e = Ecb.Instantiate(index, Prototype);

            var pos = PositionData[index]; // Unity coordinates
            
            // Prototype has all correct components up front, can use SetComponent
            Ecb.SetComponent(index, e, new LocalToWorld {
                    Value = float4x4.TRS(
                        new(pos.x, pos.y, pos.z),
                        quaternion.identity,
                        new(1F, 1F, 1F)
                    ) });

            // Use empty mesh on start
            Ecb.SetComponent(index, e, MaterialMeshInfo.FromRenderMeshArrayIndices(0, EmptyMeshIndex));

            Ecb.SetComponent(index, e, new LowCostBlockInstanceComponent { Position = pos, DataIndex = index });
            
        }
    }
}