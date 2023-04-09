using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace MarkovBlocks
{
    public partial struct BlockInstanceSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<BlockInstanceComponent>();
            
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // An EntityCommandBuffer created from an EntityCommandBufferSystem singleton will be
            // played back and disposed by the EntityCommandBufferSystem the next time it updates.
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (magic, trs, entity) in
                    SystemAPI.Query<RefRW<BlockInstanceComponent>, RefRW<LocalToWorld>>().WithEntityAccess())
            {
                if (magic.ValueRO.LifeTime <= 0F)
                    continue;

                magic.ValueRW.TimeLeft -= SystemAPI.Time.DeltaTime;

                if (magic.ValueRO.TimeLeft <= -magic.ValueRO.LifeTime)
                {
                    // Making a structural change would invalidate the query we are iterating through,
                    // so instead we record a command to destroy the entity later.
                    ecb.DestroyEntity(entity);
                }
                else if (magic.ValueRO.LifeTime >= 0.15F && magic.ValueRO.TimeLeft < -0.05F)
                {
                    // Fade out by reducing its scale
                    var scale = 1F + (magic.ValueRO.TimeLeft / magic.ValueRO.LifeTime);
                    var offset = (1F - scale) / 2F;

                    trs.ValueRW.Value = float4x4.TRS(
                        magic.ValueRO.Position + new float3(offset),
                        quaternion.identity,
                        new float3(scale)
                    );
                }
            }
        
            
        }
    }
}
