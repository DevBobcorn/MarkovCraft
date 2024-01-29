using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace MarkovCraft
{
    public partial struct LowCostBlockInstanceSystem : ISystem
    {
        BufferTypeHandle<BlockMeshBufferElement> blockMeshBufferHandle;

        private static readonly float4 WHITE = new(1F);

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LowCostBlockInstanceComponent>();
            
            blockMeshBufferHandle = state.GetBufferTypeHandle<BlockMeshBufferElement>(true);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            blockMeshBufferHandle.Update(ref state);

            var myQuery = SystemAPI.QueryBuilder().WithAll<BlockMeshBufferElement>().Build();
            var chunks = myQuery.ToArchetypeChunkArray(state.WorldUpdateAllocator);

            if (chunks.Length <= 0) return;
            var accessor = chunks[0].GetBufferAccessor(ref blockMeshBufferHandle);

            if (accessor.Length <= 0) return;
            var gridDataBuffer = accessor[0];

            DynamicBuffer<int3> bufferAsInt3s = gridDataBuffer.Reinterpret<int3>();

            foreach (var (comp, mmm, ccc) in
                    SystemAPI.Query<
                        RefRW<LowCostBlockInstanceComponent>,
                        RefRW<MaterialMeshInfo>,
                        RefRW<InstanceBlockColorComponent>
                    >())
            {
                var dataIndex = comp.ValueRO.DataIndex;
                if (dataIndex >= bufferAsInt3s.Length) continue;

                int3 meshData = bufferAsInt3s[dataIndex]; // mesh index, material index, color

                // Update mesh and material
                if (mmm.ValueRW.Material != -meshData.y - 1)
                {
                    mmm.ValueRW.Material = -meshData.y - 1;
                }

                if (mmm.ValueRO.Mesh != -meshData.x - 1)
                {
                    mmm.ValueRW.Mesh = -meshData.x - 1;
                    ccc.ValueRW.Value = meshData.x == 0 ? ComputeColor(meshData.z) : WHITE;
                }
            }
        }

        public static float4 ComputeColor(int rgb)
        {
            return new(((rgb & 0xFF0000) >> 16) / 255F, ((rgb & 0xFF00) >> 8) / 255F, (rgb & 0xFF) / 255F, 1F);
        }
    }
}
