using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace MarkovCraft
{
    public partial struct ClearUpSystem : ISystem
    {
        public const float FADE_TIME = OptimizedBlockInstanceSystem.FADE_TIME;

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

            foreach (var comp in SystemAPI.Query<RefRW<RegularBlockInstanceComponent>>())
            {
                var pos = comp.ValueRO.Position;
                comp.ValueRW.LifeTime = math.max(1, 25 + pos.x + pos.z - pos.y) * 0.01F;
                comp.ValueRW.Timer = 0F;
            }

            foreach (var comp in SystemAPI.Query<RefRW<OptimizedBlockInstanceComponent>>())
            {
                var pos = comp.ValueRO.Position;
                comp.ValueRW.LifeTime = math.max(1, 25 + pos.x + pos.z - pos.y) * 0.01F;
                // Reset playtime, but avoid playing fade-in animation
                comp.ValueRW.Timer = FADE_TIME;
            }

            // Remove all clear up tag components
            foreach (var (comp, entity) in SystemAPI.Query<RefRW<ClearTagComponent>>().WithEntityAccess())
            {
                ecb.DestroyEntity(entity);
            }
        }
    }
}
