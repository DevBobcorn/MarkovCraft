using Unity.Burst;
using Unity.Entities;

namespace MarkovCraft
{
    public partial struct ClearUpSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ClearTagComponent>();
            
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
                if (magic.ValueRO.LifeTime <= 0F)
                    magic.ValueRW.LifeTime = 1F;
                
                magic.ValueRW.TimeLeft = 0.1F;
            }

            // Remove all clear up tag components
            foreach (var (markov, entity) in
                    SystemAPI.Query<RefRW<ClearTagComponent>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}
