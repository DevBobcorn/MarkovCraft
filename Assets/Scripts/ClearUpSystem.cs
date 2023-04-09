using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace MarkovBlocks
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

            foreach (var (magic, trs, entity) in
                    SystemAPI.Query<RefRW<BlockInstanceComponent>, RefRW<LocalToWorld>>().WithEntityAccess())
            {
                if (magic.ValueRO.LifeTime <= 0F)
                    magic.ValueRW.LifeTime = magic.ValueRO.TimeLeft;
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
