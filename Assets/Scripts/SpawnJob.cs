#nullable enable
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;

namespace MarkovBlocks
{
    [GenerateTestsForBurstCompatibility]
    public struct SpawnJob : IJobParallelFor
    {
        public Entity Prototype;
        public EntityCommandBuffer.ParallelWriter Ecb;

        [ReadOnly]
        public NativeArray<int3> PositionData; // x, y, z

        [ReadOnly]
        public NativeArray<int2> MeshData; // mesh index, color

        [ReadOnly]
        public float LifeTime;

        public void Execute(int index)
        {
            var e = Ecb.Instantiate(index, Prototype);

            // Prototype has all correct components up front, can use SetComponent
            var pos = PositionData[index];
            var mesh = MeshData[index];

            Ecb.SetComponent(index, e, new LocalToWorld {
                    Value = float4x4.TRS(
                        new(pos.x, pos.y, pos.z),
                        quaternion.identity,
                        new(1F, 1F, 1F)
                    ) });
            
            Ecb.SetComponent(index, e, new InstanceBlockColor() { Value = ComputeColor(mesh.y) });
            Ecb.SetComponent(index, e, MaterialMeshInfo.FromRenderMeshArrayIndices(0, mesh.x));

            Ecb.SetComponent(index, e, new MagicComponent { TimeLeft = LifeTime, LifeTime = LifeTime, Position = pos });
            
        }

        public static float4 ComputeColor(int rgb)
        {
            return new(((rgb & 0xFF0000) >> 16) / 255F, ((rgb & 0xFF00) >> 8) / 255F, (rgb & 0xFF) / 255F, 1F);
        }

    }
}