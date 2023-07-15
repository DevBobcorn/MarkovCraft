using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

namespace MarkovCraft
{
    public partial struct OptimizedBlockInstanceSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<OptimizedBlockInstanceComponent>();
            
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {

        }

        public const float FADE_TIME = 0.2F;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // An EntityCommandBuffer created from an EntityCommandBufferSystem singleton will be
            // played back and disposed by the EntityCommandBufferSystem the next time it updates.
            var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
            var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

            foreach (var (comp, trs, entity) in
                    SystemAPI.Query<RefRW<OptimizedBlockInstanceComponent>, RefRW<LocalToWorld>>().WithEntityAccess())
            {
                comp.ValueRW.Timer += SystemAPI.Time.DeltaTime; // Time left increases
                float ti = comp.ValueRO.Timer;
                float lt = comp.ValueRO.LifeTime;

                if (lt <= 0F) // Persistent entities
                {
                    if (ti <= FADE_TIME) // Fade in by increase its scale
                    {
                        var scale = math.min(1F, (ti / FADE_TIME) + 0.2F);
                        var offset = (1F - scale) / 2F;
                        trs.ValueRW.Value = float4x4.TRS(
                            comp.ValueRO.Position + new float3(offset),
                            quaternion.identity,
                            new float3(scale)
                        );
                    }
                }
                else // Non-persistent entities
                {
                    if (ti >= lt) // Entity expired
                    {
                        // Making a structural change would invalidate the query we are iterating through,
                        // so instead we record a command to destroy the entity later.
                        ecb.DestroyEntity(entity);
                    }
                    else if (lt >= FADE_TIME) // Could use some animation
                    {
                        if (ti <= FADE_TIME) // Fade in by increasing its scale
                        {
                            var scale = math.min(1F, (ti / FADE_TIME) + 0.2F);
                            var offset = (1F - scale) / 2F;
                            trs.ValueRW.Value = float4x4.TRS(
                                comp.ValueRO.Position + new float3(offset),
                                quaternion.identity,
                                new float3(scale)
                            );
                        }
                        else if ((lt - ti) <= FADE_TIME) // Fade out by reducing its scale
                        {
                            var scale = math.min(1F, ((lt - ti) / FADE_TIME) + 0.2F);
                            var offset = (1F - scale) / 2F;
                            trs.ValueRW.Value = float4x4.TRS(
                                comp.ValueRO.Position + new float3(offset),
                                quaternion.identity,
                                new float3(scale)
                            );
                        }
                    }
                }
            }
        }
    }
}
