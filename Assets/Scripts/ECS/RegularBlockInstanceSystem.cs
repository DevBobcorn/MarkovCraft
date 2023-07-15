using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

namespace MarkovCraft
{
    public partial struct RegularBlockInstanceSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RegularBlockInstanceComponent>();
            
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        private const float FADE_TIME = 0.2F;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // An EntityCommandBuffer created from an EntityCommandBufferSystem singleton will be
            // played back and disposed by the EntityCommandBufferSystem the next time it updates.
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (comp, trs, entity) in
                    SystemAPI.Query<RefRW<RegularBlockInstanceComponent>, RefRW<LocalToWorld>>().WithEntityAccess())
            {
                float lt = comp.ValueRO.LifeTime;

                if (lt <= 0F) // Persistent entities
                    continue;
                
                comp.ValueRW.Timer += SystemAPI.Time.DeltaTime; // Time left increases

                if (comp.ValueRO.Timer > lt + 0.1F) // Entity expired, destroy them a bit later here to prevent glitches
                {
                    // Making a structural change would invalidate the query we are iterating through,
                    // so instead we record a command to destroy the entity later.
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
