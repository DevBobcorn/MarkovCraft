using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;

namespace MarkovBlocks
{
    [BurstCompile]
    public partial struct MagicSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<MagicComponent>();
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

            // WithAll() includes RotationSpeed in the query, but
            // the RotationSpeed component values will not be accessed.
            // WithEntityAccess() includes the Entity ID as the last element of the tuple.
            foreach (var (magic, entity) in
                     SystemAPI.Query<RefRW<MagicComponent>>().WithEntityAccess())
            {
                magic.ValueRW.TimeLeft -= SystemAPI.Time.DeltaTime;

                if (magic.ValueRO.TimeLeft <= 0F)
                {
                    // Making a structural change would invalidate the query we are iterating through,
                    // so instead we record a command to destroy the entity later.
                    ecb.DestroyEntity(entity);
                }
            }
        }
    }
}
