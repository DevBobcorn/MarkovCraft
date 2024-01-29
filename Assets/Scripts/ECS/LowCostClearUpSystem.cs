using Unity.Burst;
using Unity.Entities;

namespace MarkovCraft
{
    public partial struct LowCostClearUpSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<LowCostClearTagComponent>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // An EntityCommandBuffer created from an EntityCommandBufferSystem singleton will be
            // played back and disposed by the EntityCommandBufferSystem the next time it updates.
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (comp, entity) in SystemAPI.Query<RefRW<LowCostBlockInstanceComponent>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }

            // Remove all clear up tag components
            foreach (var (comp, entity) in SystemAPI.Query<RefRW<LowCostClearTagComponent>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}
