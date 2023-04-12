using Unity.Burst;
using Unity.Entities;

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

            foreach (var (magic, entity) in
                    SystemAPI.Query<RefRW<BlockInstanceComponent>>().WithEntityAccess())
            {
                if (magic.ValueRO.LifeTime <= 0F) // Persistent entities
                    continue;

                magic.ValueRW.TimeLeft -= SystemAPI.Time.DeltaTime;

                if (magic.ValueRO.TimeLeft <= -0.05F)
                {
                    // Making a structural change would invalidate the query we are iterating through,
                    // so instead we record a command to destroy the entity later.
                    ecb.DestroyEntity(entity);
                }
            }
        
            
        }
    }
}
