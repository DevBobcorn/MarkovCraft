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
        public NativeArray<int3> PositionData; // x, y, z

        [ReadOnly]
        public int EmptyMeshIndex;

        private static readonly float4 WHITE = new(1F);

        public void Execute(int index)
        {
            var e = Ecb.Instantiate(index, Prototype);

            // Prototype has all correct components up front, can use SetComponent
            var pos = PositionData[index]; // Unity coordinates

            // Take lowest 10 bits of xyz to make an identifier for this cell (Maximum 1024*1024*1024)
            // This identifier can later be used to update this cell
            int identifier = ((pos.x & 0x3FF) << 20) | ((pos.y & 0x3FF) << 10) | (pos.z & 0x3FF);

            Ecb.SetComponent(index, e, new LocalToWorld {
                    Value = float4x4.TRS(
                        new(pos.x, pos.y, pos.z),
                        quaternion.identity,
                        new(1F, 1F, 1F)
                    ) });
            
            Ecb.SetComponent(index, e, new InstanceBlockColorComponent { Value = WHITE });

            // Use empty mesh on start
            //Ecb.SetComponent(index, e, MaterialMeshInfo.FromRenderMeshArrayIndices(0, EmptyMeshIndex));
            // or Use cube mesh on start
            Ecb.SetComponent(index, e, MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));

            Ecb.SetComponent(index, e, new LowCostBlockInstanceComponent { Timer = 0F, LifeTime = 0F, Position = pos, Identifier = identifier });
            
        }

        public static float4 ComputeColor(int rgb)
        {
            return new(((rgb & 0xFF0000) >> 16) / 255F, ((rgb & 0xFF00) >> 8) / 255F, (rgb & 0xFF) / 255F, 1F);
        }
    }
}